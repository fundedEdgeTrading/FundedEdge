namespace TrackRecord.Domain.Enums;

public enum AiReportKind
{
    /// <summary>Informe completo bajo demanda: fortalezas, fugas, viabilidad del negocio, plan de acción.</summary>
    Analysis = 0,

    /// <summary>Pregunta puntual del usuario respondida con las estadísticas agregadas como contexto.</summary>
    AdHocQuestion = 1,

    /// <summary>Informe centrado en el patrón emocional del trader (GUIA_PSICOLOGIA_TRADING.md §8.2).</summary>
    PsychologyAnalysis = 2,

    /// <summary>Mini-informe de contención disparado por 3+ días consecutivos con resultado negativo (GUIA_FUNCIONALIDADES_PROPUESTAS.md §4.1).</summary>
    LosingStreakAlert = 3,

    /// <summary>Plan de emergencia disparado cuando una cuenta baja de 20% de colchón de drawdown restante.</summary>
    DrawdownRiskAlert = 4,

    /// <summary>Informe de consolidación disparado al cobrar el primer payout de una cuenta.</summary>
    FirstPayoutMilestone = 5,
}
