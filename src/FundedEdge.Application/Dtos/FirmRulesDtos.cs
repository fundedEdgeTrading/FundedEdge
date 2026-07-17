namespace FundedEdge.Application.Dtos;

/// <summary>
/// Resultado de sincronizar las reglas de una firma desde sus fuentes (Nimble + IA), pensado para
/// previsualización antes de persistir los programas. El reglamento Markdown ya queda guardado en
/// la firma; <see cref="Programs"/> es la propuesta que el administrador revisa y confirma.
/// </summary>
public record FirmRulesSyncResult(
    Guid FirmId,
    string FirmName,
    string Markdown,
    IReadOnlyList<EvaluationProgramDto> Programs,
    string? Warning);
