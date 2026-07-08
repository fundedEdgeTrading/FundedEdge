using TrackRecord.Domain.Enums;

namespace TrackRecord.Web.Support;

/// <summary>Pautas prácticas curadas por emoción para la sección "qué trabajar ahora" (GUIA_PSICOLOGIA_TRADING.md §7.5).</summary>
public static class EmotionCoachingTips
{
    private static readonly Dictionary<EmotionType, string> Tips = new()
    {
        [EmotionType.Fomo] = "Si el impulso llega con el precio ya en movimiento, espera el pullback o déjalo ir — apúntalo como trade no-tomado en vez de perseguirlo.",
        [EmotionType.Vengeful] = "Tras una pérdida, impón una pausa mínima de 15 minutos antes de poder abrir el siguiente trade.",
        [EmotionType.Frustrated] = "La frustración es la antesala directa del revenge trading — si aparece, cierra la plataforma 10 minutos antes de decidir el siguiente movimiento.",
        [EmotionType.Euphoric] = "Tras 2+ ganadores seguidos, fija el tamaño habitual como límite duro — la racha no valida un riesgo mayor.",
        [EmotionType.Overconfident] = "Antes de aumentar tamaño, pregúntate si lo harías igual tras una pérdida — si la respuesta es no, es la racha hablando, no el análisis.",
        [EmotionType.Fearful] = "Define el objetivo o el trailing stop antes de entrar, no durante el trade, para no salir por ansiedad.",
        [EmotionType.Doubtful] = "Si dudas del setup en el momento de entrar, es que no cumple tus criterios — anótalo y pasa al siguiente.",
        [EmotionType.Anxious] = "Reduce el tamaño hasta que la entrada no genere una respuesta física — el tamaño correcto es el que te permite pensar con calma.",
        [EmotionType.Bored] = "Fija un número máximo de trades/día y una lista de setups válidos — el aburrimiento busca actividad, no oportunidad.",
        [EmotionType.Regretful] = "Registra el trade y ciérralo mentalmente antes del siguiente — rumiar un trade cerrado contamina el criterio del próximo.",
        [EmotionType.Detached] = "Es una señal de descanso, no de ajuste técnico. Un día sin operar puede ser la mejora de mayor impacto ahora mismo.",
        [EmotionType.Calm] = "Tu estado óptimo — identifica qué rutina previa lo produce y repítela a propósito.",
        [EmotionType.Confident] = "Si viene de un análisis sólido, mantenla; si viene de una racha, trátala como euforia incipiente.",
        [EmotionType.Hopeful] = "En un trade abierto suele significar que no hay plan de salida — defínelo explícitamente antes de que la esperanza decida por ti.",
    };

    public static string TipFor(EmotionType emotion) => Tips.GetValueOrDefault(emotion, "Registra más trades con esta emoción para afinar una pauta específica.");
}
