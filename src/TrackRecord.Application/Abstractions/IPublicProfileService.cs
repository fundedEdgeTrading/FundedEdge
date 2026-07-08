namespace TrackRecord.Application.Abstractions;

/// <summary>Gestiona la página pública de track record (F5.2, plan Elite).</summary>
public interface IPublicProfileService
{
    /// <summary>Estado de la página del usuario autenticado actual, para /plan.</summary>
    Task<PublicProfileSettings> GetOwnSettingsAsync(CancellationToken ct = default);

    /// <summary>Activa la página del usuario actual (genera slug si no existe). Requiere plan Elite.</summary>
    Task<PublicProfileSettings> EnableAsync(CancellationToken ct = default);

    Task DisableAsync(CancellationToken ct = default);

    /// <summary>Vista pública por slug, o null si no existe/está deshabilitada/el dueño ya no es Elite.</summary>
    Task<PublicProfileView?> GetPublicViewAsync(string slug, CancellationToken ct = default);
}
