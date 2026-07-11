using TrackRecord.Domain.Common;
using TrackRecord.Domain.Enums;

namespace TrackRecord.Domain.Entities;

/// <summary>
/// Propuesta de cambio del catálogo generada por la extracción LLM (fase 2 de
/// INVESTIGACION_AUTOMATIZACION_REGLAS.md): las reglas extraídas de una página oficial se
/// guardan aquí como staging y NUNCA se escriben directamente en <see cref="EvaluationProgram"/>.
/// Un administrador revisa el diff en /admin/rule-monitor y, al aprobar, se aplica el versionado
/// habitual (programa anterior inactivo, versión nueva con EffectiveFrom = hoy).
/// </summary>
public class ProposedProgramChange : Entity
{
    public Guid PropFirmId { get; set; }
    public PropFirm? PropFirm { get; set; }

    /// <summary>Nombre comercial del programa extraído (p.ej. "Apex 50K").</summary>
    public string ProgramName { get; set; } = null!;

    /// <summary>
    /// Programa activo contra el que se calculó el diff. Null = programa nuevo que no existe
    /// en el catálogo; al aprobar se crea en vez de versionar.
    /// </summary>
    public Guid? ExistingProgramId { get; set; }

    /// <summary>URL de la página de la que se extrajo (snapshot, sin FK: sobrevive al borrado de la fuente).</summary>
    public string? SourceUrl { get; set; }

    /// <summary>ExtractedProgramRules serializado: campos extraídos, citas literales y confianza.</summary>
    public string PayloadJson { get; set; } = null!;

    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }
}
