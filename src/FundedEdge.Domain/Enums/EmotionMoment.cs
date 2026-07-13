namespace FundedEdge.Domain.Enums;

/// <summary>Momento del trade en el que se registra la emoción — diagnósticos distintos según el momento.</summary>
public enum EmotionMoment
{
    BeforeEntry = 0,
    DuringTrade = 1,
    AfterExit = 2,
}
