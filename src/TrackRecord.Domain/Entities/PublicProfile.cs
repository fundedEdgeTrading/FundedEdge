using TrackRecord.Domain.Common;

namespace TrackRecord.Domain.Entities;

/// <summary>
/// Página pública de track record de un usuario (F5.2, plan Elite), accesible en /t/{Slug}.
/// Solo expone KPIs agregados no monetarios — nunca trades individuales ni importes de costes
/// (ver PublicProfileView, el único DTO que puede leer esta entidad hacia el exterior).
/// </summary>
public class PublicProfile : Entity
{
    public string UserId { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
