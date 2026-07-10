using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class TradeSetupService(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    ICurrentUserAccessor currentUser) : ITradeSetupService
{
    public async Task<IReadOnlyList<TradeSetupDto>> GetAllAsync(CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.TradeSetups.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Name)
            .Select(s => new TradeSetupDto(s.Id, s.Name))
            .ToListAsync(ct);
    }

    public async Task<Guid> CreateAsync(string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("El nombre del setup no puede estar vacío.", nameof(name));
        }

        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var exists = await db.TradeSetups.AnyAsync(s => s.UserId == userId && s.Name == trimmed, ct);
        if (exists)
        {
            throw new InvalidOperationException($"Ya existe un setup llamado '{trimmed}'.");
        }

        var setup = new TradeSetup { UserId = userId, Name = trimmed };
        db.TradeSetups.Add(setup);
        await db.SaveChangesAsync(ct);
        return setup.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = await currentUser.RequireUserIdAsync();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var setup = await db.TradeSetups.SingleOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);
        if (setup is null) return;

        db.TradeSetups.Remove(setup);
        await db.SaveChangesAsync(ct);
    }
}
