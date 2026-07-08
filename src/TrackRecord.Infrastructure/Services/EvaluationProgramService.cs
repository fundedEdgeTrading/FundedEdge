using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class EvaluationProgramService(IDbContextFactory<TrackRecordDbContext> dbFactory) : IEvaluationProgramService
{
    public async Task<IReadOnlyList<EvaluationProgramDto>> GetByFirmAsync(Guid firmId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.EvaluationPrograms
            .AsNoTracking()
            .Include(p => p.PropFirm)
            .Where(p => p.PropFirmId == firmId && p.IsActive)
            .OrderBy(p => p.AccountSize)
            .Select(p => ToDto(p))
            .ToListAsync(ct);
    }

    public async Task<EvaluationProgramDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var p = await db.EvaluationPrograms
            .AsNoTracking()
            .Include(p => p.PropFirm)
            .SingleOrDefaultAsync(p => p.Id == id, ct);
        return p is null ? null : ToDto(p);
    }

    public async Task<Guid> CreateAsync(UpsertEvaluationProgramRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var program = BuildEntity(request);
        db.EvaluationPrograms.Add(program);
        await db.SaveChangesAsync(ct);
        return program.Id;
    }

    public async Task<Guid> UpdateAsync(Guid id, UpsertEvaluationProgramRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Patrón de versionado: marcar el programa actual como inactivo y crear uno nuevo.
        var existing = await db.EvaluationPrograms.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"EvaluationProgram {id} no encontrado.");

        existing.IsActive = false;

        var newProgram = BuildEntity(request);
        newProgram.EffectiveFrom = DateOnly.FromDateTime(DateTime.Today);
        db.EvaluationPrograms.Add(newProgram);

        await db.SaveChangesAsync(ct);
        return newProgram.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var program = await db.EvaluationPrograms.FindAsync([id], ct);
        if (program is null) return;
        program.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private static EvaluationProgram BuildEntity(UpsertEvaluationProgramRequest r) => new()
    {
        PropFirmId               = r.PropFirmId,
        Name                     = r.Name,
        AccountSize              = r.AccountSize,
        EvaluationCost           = r.EvaluationCost,
        ActivationCost           = r.ActivationCost,
        ProfitTarget             = r.ProfitTarget,
        MaxDrawdown              = r.MaxDrawdown,
        DrawdownType             = r.DrawdownType,
        DailyLossLimit           = r.DailyLossLimit,
        MinTradingDays           = r.MinTradingDays,
        ConsistencyMaxDayFraction = r.ConsistencyMaxDayFraction,
        FundedMaxDrawdown        = r.FundedMaxDrawdown,
        FundedDrawdownType       = r.FundedDrawdownType,
        FundedDailyLossLimit     = r.FundedDailyLossLimit,
        FundedProfitTarget       = r.FundedProfitTarget,
        FundedMinTradingDays     = r.FundedMinTradingDays,
        PayoutSplitTraderPct     = r.PayoutSplitTraderPct,
        PayoutMaxProfitPct       = r.PayoutMaxProfitPct,
        PayoutMinDaysBetween     = r.PayoutMinDaysBetween,
        EffectiveFrom            = DateOnly.FromDateTime(DateTime.Today),
        IsActive                 = true,
    };

    private static EvaluationProgramDto ToDto(EvaluationProgram p) => new(
        p.Id,
        p.PropFirmId,
        p.PropFirm?.Name ?? string.Empty,
        p.Name,
        p.AccountSize,
        p.EvaluationCost,
        p.ActivationCost,
        p.ProfitTarget,
        p.MaxDrawdown,
        p.DrawdownType,
        p.DailyLossLimit,
        p.MinTradingDays,
        p.ConsistencyMaxDayFraction,
        p.FundedMaxDrawdown,
        p.FundedDrawdownType,
        p.FundedDailyLossLimit,
        p.FundedProfitTarget,
        p.FundedMinTradingDays,
        p.PayoutSplitTraderPct,
        p.PayoutMaxProfitPct,
        p.PayoutMinDaysBetween,
        p.EffectiveFrom,
        p.IsActive);
}
