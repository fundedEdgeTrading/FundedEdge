namespace TrackRecord.Domain.Enums;

public enum AiReportKind
{
    /// <summary>Informe completo bajo demanda: fortalezas, fugas, viabilidad del negocio, plan de acción.</summary>
    Analysis = 0,

    /// <summary>Pregunta puntual del usuario respondida con las estadísticas agregadas como contexto.</summary>
    AdHocQuestion = 1,
}
