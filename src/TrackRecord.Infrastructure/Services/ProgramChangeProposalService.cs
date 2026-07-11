using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Domain.Enums;
using TrackRecord.Infrastructure.Persistence;
using TrackRecord.Infrastructure.RuleMonitor;

namespace TrackRecord.Infrastructure.Services;

public class ProgramChangeProposalService(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    IEvaluationProgramService programService) : IProgramChangeProposalService
{
    public async Task<IReadOnlyList<ProposedProgramChangeDto>> GetPendingAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pending = await db.ProposedProgramChanges
            .AsNoTracking()
            .Include(p => p.PropFirm)
            .Where(p => p.Status == ProposalStatus.Pending)
            .OrderBy(p => p.PropFirm!.Name).ThenBy(p => p.ProgramName)
            .ToListAsync(ct);

        if (pending.Count == 0) return [];

        var existingIds = pending.Where(p => p.ExistingProgramId is not null).Select(p => p.ExistingProgramId!.Value).ToList();
        var existingById = await db.EvaluationPrograms.AsNoTracking()
            .Where(e => existingIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        var result = new List<ProposedProgramChangeDto>(pending.Count);
        foreach (var p in pending)
        {
            var rules = Deserialize(p);
            var existing = p.ExistingProgramId is not null && existingById.TryGetValue(p.ExistingProgramId.Value, out var e) ? e : null;
            result.Add(new ProposedProgramChangeDto(
                p.Id, p.PropFirmId, p.PropFirm?.Name ?? string.Empty, p.ProgramName,
                p.ExistingProgramId, p.SourceUrl, p.Status, p.CreatedAt, rules.Confidence,
                ProgramDiffCalculator.ComputeDiffs(rules, existing)));
        }
        return result;
    }

    public async Task ApproveAsync(Guid proposalId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var proposal = await db.ProposedProgramChanges.SingleOrDefaultAsync(p => p.Id == proposalId, ct)
            ?? throw new KeyNotFoundException($"Propuesta {proposalId} no encontrada.");
        if (proposal.Status != ProposalStatus.Pending)
        {
            throw new InvalidOperationException("La propuesta ya fue revisada.");
        }

        var rules = Deserialize(proposal);
        var existing = proposal.ExistingProgramId is null
            ? null
            : await db.EvaluationPrograms.AsNoTracking()
                .SingleOrDefaultAsync(e => e.Id == proposal.ExistingProgramId, ct);

        var request = BuildRequest(proposal.PropFirmId, rules, existing);

        // El versionado (inactivar + crear con EffectiveFrom = hoy) lo aplica el servicio de
        // catálogo existente; la propuesta nunca escribe EvaluationPrograms por su cuenta.
        if (existing is not null)
            await programService.UpdateAsync(existing.Id, request, ct);
        else
            await programService.CreateAsync(request, ct);

        proposal.Status = ProposalStatus.Approved;
        proposal.ReviewedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(Guid proposalId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var proposal = await db.ProposedProgramChanges.SingleOrDefaultAsync(p => p.Id == proposalId, ct)
            ?? throw new KeyNotFoundException($"Propuesta {proposalId} no encontrada.");
        if (proposal.Status != ProposalStatus.Pending)
        {
            throw new InvalidOperationException("La propuesta ya fue revisada.");
        }

        proposal.Status = ProposalStatus.Rejected;
        proposal.ReviewedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static ExtractedProgramRules Deserialize(ProposedProgramChange proposal) =>
        JsonSerializer.Deserialize<ExtractedProgramRules>(proposal.PayloadJson, RuleExtractionJson.Options)
        ?? throw new InvalidOperationException($"Payload ilegible en la propuesta {proposal.Id}.");

    /// <summary>
    /// Fusiona extracción y programa actual: un campo null en la extracción ("la página no lo
    /// menciona") conserva el valor vigente. Para un programa nuevo, los campos imprescindibles
    /// se validaron al crear la propuesta.
    /// </summary>
    private static UpsertEvaluationProgramRequest BuildRequest(Guid propFirmId, ExtractedProgramRules r, EvaluationProgram? e) => new(
        propFirmId,
        r.Name,
        r.AccountSize,
        r.EvaluationCost ?? e?.EvaluationCost ?? throw MissingField("evaluationCost"),
        r.ActivationCost ?? e?.ActivationCost ?? 0m,
        r.ProfitTarget ?? e?.ProfitTarget ?? throw MissingField("profitTarget"),
        r.MaxDrawdown ?? e?.MaxDrawdown ?? throw MissingField("maxDrawdown"),
        r.DrawdownType ?? e?.DrawdownType ?? throw MissingField("drawdownType"),
        r.DailyLossLimit ?? e?.DailyLossLimit,
        r.MinTradingDays ?? e?.MinTradingDays,
        r.ConsistencyMaxDayFraction ?? e?.ConsistencyMaxDayFraction,
        r.FundedMaxDrawdown ?? e?.FundedMaxDrawdown,
        r.FundedDrawdownType ?? e?.FundedDrawdownType,
        r.FundedDailyLossLimit ?? e?.FundedDailyLossLimit,
        r.FundedProfitTarget ?? e?.FundedProfitTarget,
        r.FundedMinTradingDays ?? e?.FundedMinTradingDays,
        r.PayoutSplitTraderPct ?? e?.PayoutSplitTraderPct ?? 1.0m,
        r.PayoutMaxProfitPct ?? e?.PayoutMaxProfitPct,
        r.PayoutMinDaysBetween ?? e?.PayoutMinDaysBetween);

    private static InvalidOperationException MissingField(string field) =>
        new($"La extracción no incluye \"{field}\" y no hay programa existente del que heredarlo; corrige el programa a mano.");
}
