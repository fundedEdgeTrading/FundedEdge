using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TrackRecord.Domain.Entities;
using TrackRecord.Infrastructure.Persistence;

namespace TrackRecord.Infrastructure.Settings;

public class DataProtectedIntegrationSettingsStore(
    IDbContextFactory<TrackRecordDbContext> dbFactory,
    IDataProtectionProvider dataProtectionProvider) : IIntegrationSettingsStore
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("TrackRecord.IntegrationSettings.v1");

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.IntegrationSettings.AsNoTracking().SingleOrDefaultAsync(s => s.Key == key, ct);
        if (row is null) return null;

        try
        {
            return _protector.Unprotect(row.ProtectedValue);
        }
        catch (CryptographicException)
        {
            // Las claves de cifrado de Data Protection cambiaron (p.ej. se perdió el key ring):
            // se trata como si no estuviera configurado en lugar de tumbar la aplicación.
            return null;
        }
    }

    public async Task SetAsync(string key, string? value, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.IntegrationSettings.SingleOrDefaultAsync(s => s.Key == key, ct);

        if (string.IsNullOrWhiteSpace(value))
        {
            if (row is not null) db.IntegrationSettings.Remove(row);
        }
        else if (row is null)
        {
            db.IntegrationSettings.Add(new IntegrationSetting
            {
                Key = key,
                ProtectedValue = _protector.Protect(value),
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            row.ProtectedValue = _protector.Protect(value);
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<string?> FindKeyPrefixByValueAsync(string keySuffix, string value, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var candidates = await db.IntegrationSettings.AsNoTracking()
            .Where(s => s.Key.EndsWith(keySuffix))
            .ToListAsync(ct);

        foreach (var row in candidates)
        {
            string decrypted;
            try
            {
                decrypted = _protector.Unprotect(row.ProtectedValue);
            }
            catch (CryptographicException)
            {
                continue;
            }

            if (FixedTimeEquals(decrypted, value))
            {
                return row.Key[..^keySuffix.Length];
            }
        }

        return null;
    }

    /// <summary>Comparación en tiempo constante para evitar timing attacks sobre el valor comparado.</summary>
    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = System.Text.Encoding.UTF8.GetBytes(a);
        var bytesB = System.Text.Encoding.UTF8.GetBytes(b);
        if (bytesA.Length != bytesB.Length) return false;
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
