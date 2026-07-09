namespace TrackRecord.Domain.Enums;

/// <summary>Auto-evaluación de disciplina: ¿siguió el trader su plan en este trade?</summary>
public enum PlanAdherence
{
    FollowedPlan = 0,
    PartialDeviation = 1,
    NoPlan = 2,
}
