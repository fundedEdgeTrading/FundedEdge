using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Entities;

/// <summary>
/// URL oficial de una prop firm (pricing, FAQ, página de reglas) que el monitor de reglas
/// (INVESTIGACION_AUTOMATIZACION_REGLAS.md §5) comprueba periódicamente: se descarga, se
/// normaliza y se compara su hash con <see cref="LastContentHash"/> para detectar cambios de
/// condiciones sin depender de revisiones manuales.
/// </summary>
public class RuleSource : Entity
{
    public Guid PropFirmId { get; set; }
    public PropFirm? PropFirm { get; set; }

    public string Url { get; set; } = null!;

    public RuleSourceKind Kind { get; set; }

    /// <summary>Fuentes deshabilitadas se conservan pero el monitor las ignora.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// SHA-256 del contenido normalizado (sin HTML ni espaciado) de la última comprobación.
    /// Null = todavía no comprobada; la primera comprobación establece la línea base sin avisar.
    /// </summary>
    public string? LastContentHash { get; set; }

    public DateTimeOffset? LastCheckedAt { get; set; }

    /// <summary>Última vez que el hash cambió respecto a la comprobación anterior.</summary>
    public DateTimeOffset? LastChangedAt { get; set; }

    /// <summary>Error de la última comprobación (HTTP, timeout…). Null = la última fue bien.</summary>
    public string? LastError { get; set; }
}
