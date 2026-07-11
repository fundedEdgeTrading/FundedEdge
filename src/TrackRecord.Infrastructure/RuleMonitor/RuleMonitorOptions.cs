namespace TrackRecord.Infrastructure.RuleMonitor;

/// <summary>
/// Configuración del monitor de reglas ("RuleMonitor:*"). Desactivado por defecto (mismo criterio
/// que los servicios de IA en background: genera tráfico saliente y, con extracción, coste de API).
/// <see cref="NotifyEmail"/> es el buzón del administrador que recibe los avisos de cambio;
/// sin él, los cambios solo quedan en el log y en la UI de /admin/rule-monitor.
/// </summary>
public record RuleMonitorOptions(bool Enabled, string? NotifyEmail);
