namespace TrackRecord.Domain.Enums;

/// <summary>Estado de una propuesta de cambio de programa generada por la extracción automática.</summary>
public enum ProposalStatus
{
    /// <summary>Esperando revisión del administrador en /admin/rule-monitor.</summary>
    Pending = 0,

    /// <summary>Aprobada: se aplicó al catálogo como nueva versión del programa.</summary>
    Approved = 1,

    /// <summary>Rechazada: no se aplicó (extracción errónea o cambio irrelevante).</summary>
    Rejected = 2,
}
