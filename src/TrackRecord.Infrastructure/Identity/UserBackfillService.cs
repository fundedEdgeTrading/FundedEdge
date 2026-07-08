using Microsoft.EntityFrameworkCore;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Identity;

/// <summary>
/// Al añadir autenticación a una instancia que ya tenía cuentas/informes de IA sin dueño (creados
/// antes de que existiera login), el primer usuario que se registra se convierte en su dueño —
/// así no se pierden los datos de prueba/reales cargados previamente. A partir del segundo
/// usuario ya no hay filas huérfanas que reclamar y esto no hace nada.
/// </summary>
public class UserBackfillService(IDbContextFactory<TrackRecordDbContext> dbFactory)
{
    public async Task BackfillIfFirstUserAsync(string newUserId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var userCount = await db.Users.CountAsync(ct);
        if (userCount != 1) return; // el propio usuario recién creado ya cuenta

        var orphanedAccounts = await db.TradingAccounts.Where(a => a.UserId == null).ToListAsync(ct);
        foreach (var account in orphanedAccounts) account.UserId = newUserId;

        var orphanedReports = await db.AiReports.Where(r => r.UserId == null).ToListAsync(ct);
        foreach (var report in orphanedReports) report.UserId = newUserId;

        if (orphanedAccounts.Count > 0 || orphanedReports.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
