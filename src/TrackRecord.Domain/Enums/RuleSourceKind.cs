namespace TrackRecord.Domain.Enums;

/// <summary>Tipo de página oficial de una firma que se monitoriza en busca de cambios de reglas.</summary>
public enum RuleSourceKind
{
    /// <summary>Página de precios/planes: coste, tamaño de cuenta, target, drawdown.</summary>
    Pricing = 0,

    /// <summary>FAQ / help center: consistencia, payouts, días mínimos, letra pequeña.</summary>
    Faq = 1,

    /// <summary>Página de reglas dedicada u otra fuente oficial.</summary>
    Rules = 2,
}
