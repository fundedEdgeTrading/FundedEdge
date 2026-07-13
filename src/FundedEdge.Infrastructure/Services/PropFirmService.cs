using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Dtos;
using FundedEdge.Domain.Common;
using FundedEdge.Domain.Entities;
using FundedEdge.Domain.Enums;
using FundedEdge.Infrastructure.Email;
using FundedEdge.Infrastructure.Persistence;

namespace FundedEdge.Infrastructure.Services;

public class PropFirmService(
    IDbContextFactory<FundedEdgeDbContext> dbFactory,
    IAppEmailSender emailSender,
    ILogger<PropFirmService> logger) : IPropFirmService
{
    /// <summary>Muestra mínima para publicar el tiempo de pago agregado de una firma (M6).</summary>
    public const int MinPayoutSample = 5;
    public const int MinTraderSample = 3;

    public async Task<IReadOnlyList<PropFirmDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PropFirms
            .OrderBy(f => f.Name)
            .Select(f => new PropFirmDto(
                f.Id, f.Name, f.Website, f.Notes, f.MinDaysBetweenPayouts, f.Accounts.Count,
                f.HealthStatus, f.Country, f.HealthNotes, f.HealthUpdatedOn))
            .ToListAsync(ct);
    }

    public async Task<PropFirmDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PropFirms
            .Where(f => f.Id == id)
            .Select(f => new PropFirmDto(
                f.Id, f.Name, f.Website, f.Notes, f.MinDaysBetweenPayouts, f.Accounts.Count,
                f.HealthStatus, f.Country, f.HealthNotes, f.HealthUpdatedOn))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<Guid> CreateAsync(UpsertPropFirmRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var firm = new PropFirm
        {
            Name = request.Name,
            Website = request.Website,
            Notes = request.Notes,
            MinDaysBetweenPayouts = request.MinDaysBetweenPayouts,
            HealthStatus = request.HealthStatus,
            Country = request.Country,
            HealthNotes = request.HealthNotes,
            HealthUpdatedOn = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.PropFirms.Add(firm);
        await db.SaveChangesAsync(ct);
        return firm.Id;
    }

    public async Task UpdateAsync(Guid id, UpsertPropFirmRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var firm = await db.PropFirms.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"PropFirm {id} no encontrada.");
        var previousStatus = firm.HealthStatus;
        firm.Name = request.Name;
        firm.Website = request.Website;
        firm.Notes = request.Notes;
        firm.MinDaysBetweenPayouts = request.MinDaysBetweenPayouts;
        firm.HealthStatus = request.HealthStatus;
        firm.Country = request.Country;
        firm.HealthNotes = request.HealthNotes;
        if (previousStatus != request.HealthStatus)
        {
            firm.HealthUpdatedOn = DateOnly.FromDateTime(DateTime.UtcNow);
        }
        await db.SaveChangesAsync(ct);

        if (previousStatus != request.HealthStatus)
        {
            await NotifyHealthChangeAsync(db, firm, previousStatus, ct);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var firm = await db.PropFirms.FindAsync([id], ct);
        if (firm is null) return;
        db.PropFirms.Remove(firm);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, FirmPayoutSpeedDto>> GetPayoutSpeedAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Agregado deliberadamente global (todos los usuarios): es el dato de comunidad que nadie
        // publica con base verificable. Nunca se exponen payouts individuales, solo percentiles
        // por firma y solo con muestra mínima.
        var rows = await db.Payouts.AsNoTracking()
            .Where(p => p.Status == PayoutStatus.Paid && p.PaidOn != null && p.Account!.UserId != null)
            .Select(p => new
            {
                FirmId = p.Account!.PropFirmId,
                UserId = p.Account.UserId!,
                p.RequestedOn,
                PaidOn = p.PaidOn!.Value,
            })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, FirmPayoutSpeedDto>();
        foreach (var group in rows.GroupBy(r => r.FirmId))
        {
            var days = group
                .Select(r => r.PaidOn.DayNumber - r.RequestedOn.DayNumber)
                .Where(d => d >= 0)
                .OrderBy(d => d)
                .ToList();
            var traders = group.Select(r => r.UserId).Distinct().Count();
            if (days.Count < MinPayoutSample || traders < MinTraderSample) continue;

            result[group.Key] = new FirmPayoutSpeedDto(
                group.Key,
                days.Count,
                traders,
                Percentile(days, 0.5),
                Percentile(days, 0.9));
        }
        return result;
    }

    /// <summary>Percentil por rango más cercano sobre una lista ya ordenada ascendente.</summary>
    private static int Percentile(IReadOnlyList<int> sortedDays, double fraction) =>
        sortedDays[Math.Min(sortedDays.Count - 1, (int)Math.Ceiling(fraction * sortedDays.Count) - 1)];

    /// <summary>
    /// Aviso a los usuarios con cuentas activas en la firma cuando cambia su estado de salud
    /// (M6, capa alertas). Un fallo de envío no aborta la actualización del catálogo.
    /// </summary>
    private async Task NotifyHealthChangeAsync(FundedEdgeDbContext db, PropFirm firm, FirmHealthStatus previous, CancellationToken ct)
    {
        try
        {
            var emails = await db.TradingAccounts.AsNoTracking()
                .Where(a => a.PropFirmId == firm.Id && a.UserId != null
                    && (a.Stage == AccountStage.Evaluation || a.Stage == AccountStage.Funded))
                .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => u.Email)
                .Where(e => e != null && e != "")
                .Distinct()
                .ToListAsync(ct);

            if (emails.Count == 0) return;

            var html = $"""
                <p>El estado de <strong>{firm.Name}</strong> en el catálogo de {Brand.Name} ha cambiado
                de <strong>{previous}</strong> a <strong>{firm.HealthStatus}</strong> y tienes cuentas activas en esta firma.</p>
                {(string.IsNullOrWhiteSpace(firm.HealthNotes) ? "" : $"<p>Notas: {firm.HealthNotes}</p>")}
                <p style="color:#666;font-size:.85em">Revisa la sección "Firmas" de tu cuenta para ver el detalle.</p>
                """;

            foreach (var email in emails)
            {
                await emailSender.SendAsync(email!, $"Cambio de estado de {firm.Name} — {Brand.Name}", html, ct);
            }
            logger.LogInformation("Aviso de cambio de salud de {Firm} ({Previous} → {Current}) enviado a {Count} usuarios.",
                firm.Name, previous, firm.HealthStatus, emails.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo avisar del cambio de salud de la firma {FirmId}.", firm.Id);
        }
    }
}
