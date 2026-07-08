namespace TrackRecord.Domain.Enums;

/// <summary>
/// Etapa del ciclo de vida de una cuenta de fondeo.
/// Evaluation y Funded son estados activos; Failed, Withdrawn y Expired son terminales.
/// </summary>
public enum AccountStage
{
    Evaluation = 0,
    Funded = 1,
    Failed = 2,
    Withdrawn = 3,
    Expired = 4,
}
