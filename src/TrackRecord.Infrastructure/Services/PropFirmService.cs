using Microsoft.EntityFrameworkCore;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Dtos;
using TrackRecord.Domain.Entities;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Services;

public class PropFirmService(IDbContextFactory<TrackRecordDbContext> dbFactory) : IPropFirmService
{
    public async Task<IReadOnlyList<PropFirmDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PropFirms
            .OrderBy(f => f.Name)
            .Select(f => new PropFirmDto(f.Id, f.Name, f.Website, f.Notes, f.MinDaysBetweenPayouts, f.Accounts.Count))
            .ToListAsync(ct);
    }

    public async Task<PropFirmDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PropFirms
            .Where(f => f.Id == id)
            .Select(f => new PropFirmDto(f.Id, f.Name, f.Website, f.Notes, f.MinDaysBetweenPayouts, f.Accounts.Count))
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
        firm.Name = request.Name;
        firm.Website = request.Website;
        firm.Notes = request.Notes;
        firm.MinDaysBetweenPayouts = request.MinDaysBetweenPayouts;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var firm = await db.PropFirms.FindAsync([id], ct);
        if (firm is null) return;
        db.PropFirms.Remove(firm);
        await db.SaveChangesAsync(ct);
    }
}
