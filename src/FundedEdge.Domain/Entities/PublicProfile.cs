using FundedEdge.Domain.Common;

namespace FundedEdge.Domain.Entities;

/// <summary>
/// Página pública de track record de un usuario (F5.2, plan Elite), accesible en /t/{Slug}.
/// Solo expone KPIs agregados no monetarios — nunca trades individuales ni importes de costes
/// (ver PublicProfileView, el único DTO que puede leer esta entidad hacia el exterior).
/// </summary>
public class PublicProfile : Entity
{
    public string UserId { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Opt-in explícito del dueño para que otros usuarios Elite puedan analizar su operativa
    /// (setups, franjas horarias, R-múltiplos) en el módulo de perfiles Elite. Solo agregados,
    /// nunca trades individuales. Desactivado por defecto (F5.6).
    /// </summary>
    public bool ShareOperativa { get; set; }

    /// <summary>
    /// Opt-in explícito, independiente de <see cref="ShareOperativa"/>, para incluir además el
    /// patrón emocional agregado (frecuencias de emociones, disciplina) en los informes de
    /// inspiración que generan otros usuarios Elite. Dato sensible: desactivado por defecto.
    /// </summary>
    public bool ShareEmotions { get; set; }
}
