using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class RuleSourceService(IDbContextFactory<TrackRecordDbContext> dbFactory) : IRuleSourceService
{
    public async Task<IReadOnlyList<RuleSourceDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.RuleSources
            .AsNoTracking()
            .Include(s => s.PropFirm)
            .OrderBy(s => s.PropFirm!.Name).ThenBy(s => s.Kind)
            .Select(s => new RuleSourceDto(
                s.Id, s.PropFirmId, s.PropFirm!.Name, s.Url, s.Kind, s.IsEnabled,
                s.LastCheckedAt, s.LastChangedAt, s.LastError))
            .ToListAsync(ct);
    }

    public async Task<Guid> CreateAsync(UpsertRuleSourceRequest request, CancellationToken ct = default)
    {
        ValidateUrl(request.Url);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var source = new RuleSource
        {
            PropFirmId = request.PropFirmId,
            Url = request.Url.Trim(),
            Kind = request.Kind,
            IsEnabled = request.IsEnabled,
        };
        db.RuleSources.Add(source);
        await db.SaveChangesAsync(ct);
        return source.Id;
    }

    public async Task UpdateAsync(Guid id, UpsertRuleSourceRequest request, CancellationToken ct = default)
    {
        ValidateUrl(request.Url);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var source = await db.RuleSources.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"RuleSource {id} no encontrada.");

        // Si la URL cambia, el hash anterior deja de tener sentido como línea base.
        if (!string.Equals(source.Url, request.Url.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            source.LastContentHash = null;
            source.LastChangedAt = null;
        }

        source.PropFirmId = request.PropFirmId;
        source.Url = request.Url.Trim();
        source.Kind = request.Kind;
        source.IsEnabled = request.IsEnabled;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var source = await db.RuleSources.FindAsync([id], ct);
        if (source is null) return;
        db.RuleSources.Remove(source);
        await db.SaveChangesAsync(ct);
    }

    private static void ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("La URL debe ser absoluta y https://.", nameof(url));
        }
    }
}
