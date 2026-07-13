# GUÍA — Módulo de Psicología del Trading (diario emocional + coach IA)

> Diseño funcional y técnico de la funcionalidad de psicología: registro de emociones por trade,
> integración con el motor de informes IA, gráficas emocionales, cruce emociones×resultados y
> nueva página **/psychology**. Redactado desde la doble perspectiva de psicología del
> rendimiento y trading en prop firms.

---

## 1. Fundamento psicológico

### 1.1 Por qué esto importa más en prop firms que en cuentas propias
El trader de prop firm opera bajo **restricciones asimétricas** (pérdida diaria máxima, trailing
drawdown, reglas de consistencia) que convierten errores emocionales puntuales en la pérdida
total de la cuenta. Los tres patrones que queman cuentas no son técnicos, son emocionales:

1. **Revenge trading**: reentrar inmediatamente tras una pérdida para "recuperar", con tamaño
   igual o mayor y sin setup.
2. **FOMO**: entrar tarde persiguiendo un movimiento ya hecho por miedo a quedarse fuera.
3. **Parálisis / miedo residual**: tras una racha de pérdidas, no ejecutar setups válidos o
   cortar ganadores demasiado pronto (aversión a la pérdida, Kahneman & Tversky).

La literatura de psicología del rendimiento (Steenbarger, *The Psychology of Trading*; Douglas,
*Trading in the Zone*) coincide en que **el registro emocional sistemático es la intervención de
mayor impacto**: no se puede regular lo que no se nombra (*affect labeling* — nombrar la emoción
ya reduce la activación de la amígdala, Lieberman et al. 2007).

### 1.2 Modelo emocional elegido
Usamos una taxonomía **cerrada y específica de trading** (no texto libre como dato primario),
porque necesitamos agregarla, graficarla y cruzarla con métricas. Cada emoción se mapea sobre el
**modelo circumplejo** (valencia × activación), lo que permite cálculos aunque el usuario elija
etiquetas distintas:

| Emoción (ES) | Enum | Valencia | Activación | Señal típica |
|---|---|---|---|---|
| Calma / foco | `Calm` | + | baja | Estado óptimo de ejecución |
| Confianza | `Confident` | + | media | Bien si es post-análisis; peligro si es post-racha |
| Euforia | `Euphoric` | + | alta | Antesala de sobreapalancamiento |
| Esperanza | `Hopeful` | + | media | En un trade abierto = no hay plan de salida |
| Ansiedad / nervios | `Anxious` | − | alta | Tamaño excesivo o setup no validado |
| Miedo | `Fearful` | − | alta | Parálisis, salidas prematuras |
| FOMO | `Fomo` | − | alta | Entrada persiguiendo precio |
| Ira / frustración | `Frustrated` | − | alta | Precursor directo del revenge trading |
| Venganza | `Vengeful` | − | alta | Revenge trading declarado |
| Aburrimiento | `Bored` | − | baja | Overtrading por falta de estímulo |
| Duda | `Doubtful` | − | media | Ejecución inconsistente del plan |
| Arrepentimiento | `Regretful` | − | media | Rumiación post-trade, contamina el siguiente |
| Exceso de confianza | `Overconfident` | + | alta | Saltarse el stop, ampliar tamaño |
| Indiferencia / desconexión | `Detached` | − | baja | Fatiga, burnout — señal de descanso |

**Principios de diseño del formulario (críticos para que se use):**
- **Fricción mínima**: registrar las emociones de un trade debe costar **< 20 segundos** (chips
  seleccionables, no textos obligatorios). Un diario emocional que da pereza es un diario vacío.
- **Tres momentos**: la emoción *antes* (al entrar), *durante* y *después* del trade son datos
  distintos y diagnósticos distintos. FOMO antes ≠ frustración después.
- **Intensidad 1–5**, no solo presencia/ausencia: ansiedad 2/5 es información; 5/5 es alarma.
- **Auto-evaluación de disciplina**: "¿Seguiste tu plan en este trade?" (sí / parcialmente / no)
  y "¿La entrada fue impulsiva?" (sí/no). Son las dos variables que más correlacionan con
  expectancy y las que la IA necesita para separar *proceso* de *resultado*.
- **Sin juicio en la UI**: el formulario nunca dice "mal". Las emociones no son el enemigo; el
  enemigo es operar *desde* ellas sin saberlo.
- **Contexto del día**: un mini check-in diario (sueño, estrés externo, estado pre-mercado)
  explica más varianza que el propio trade a veces.

---

## 2. Requisitos funcionales (lo pedido)

| # | Requisito |
|---|---|
| R1 | Formulario diario por usuario para describir las emociones de **cada trade registrado ese día** |
| R2 | Las respuestas se inyectan como **contexto del motor de informes IA** y de las respuestas ad-hoc |
| R3 | **Gráficas** a partir de las emociones |
| R4 | **Cruce emociones × trades**: diagnóstico de si se operó bien/mal/impulsivamente |
| R5 | Nueva **página de Psicología** con asesoramiento sobre qué emociones tratar, detección de malas rachas emocionales, etc. |

---

## 3. Modelo de dominio

Nuevos tipos en `src/FundedEdge.Domain` siguiendo las convenciones existentes (`Entity`,
enums en `Domain/Enums`):

```csharp
// Domain/Enums/EmotionType.cs
public enum EmotionType
{
    Calm = 0, Confident = 1, Euphoric = 2, Hopeful = 3,
    Anxious = 10, Fearful = 11, Fomo = 12, Frustrated = 13, Vengeful = 14,
    Bored = 20, Doubtful = 21, Regretful = 22, Overconfident = 23, Detached = 24,
}

// Domain/Enums/EmotionMoment.cs
public enum EmotionMoment { BeforeEntry = 0, DuringTrade = 1, AfterExit = 2 }

// Domain/Enums/PlanAdherence.cs
public enum PlanAdherence { FollowedPlan = 0, PartialDeviation = 1, NoPlan = 2 }

// Domain/Entities/TradeEmotionLog.cs
/// <summary>Registro emocional de un trade. Varias emociones por trade y momento.</summary>
public class TradeEmotionLog : Entity
{
    public Guid TradeId { get; set; }
    public Trade? Trade { get; set; }

    public EmotionMoment Moment { get; set; }
    public EmotionType Emotion { get; set; }
    /// <summary>Intensidad auto-reportada, 1–5.</summary>
    public int Intensity { get; set; }

    public PlanAdherence Adherence { get; set; }
    /// <summary>Auto-reporte: "¿fue una entrada impulsiva?"</summary>
    public bool WasImpulsive { get; set; }

    /// <summary>Nota libre opcional (máx. 500 chars). Nunca obligatoria.</summary>
    public string? Note { get; set; }

    public DateTimeOffset LoggedAt { get; set; }
}

// Domain/Entities/DailyMindsetCheckIn.cs
/// <summary>Check-in diario de estado general (uno por usuario y día de operativa).</summary>
public class DailyMindsetCheckIn : Entity
{
    public string UserId { get; set; } = null!;
    public DateOnly Date { get; set; }

    /// <summary>1–5: calidad de sueño, estrés externo (vida/trabajo) y foco pre-mercado.</summary>
    public int SleepQuality { get; set; }
    public int ExternalStress { get; set; }
    public int PreMarketFocus { get; set; }

    public EmotionType DominantPreMarketEmotion { get; set; }
    public string? Note { get; set; }
}
```

**Decisiones y por qué:**
- `TradeEmotionLog` separado de `Trade` (no columnas en `Trade`): N emociones por trade y
  momento, y el trade importado por webhook no se toca — el log emocional llega después.
- La adherencia al plan y la impulsividad se registran **a nivel de trade** (se repiten en las
  filas del mismo trade o, alternativamente, se puede normalizar en una entidad
  `TradeReflection` 1:1 con el trade; se recomienda la entidad 1:1 si se prevén más campos de
  reflexión — decisión a tomar en la revisión del PR de Fase 1).
- Enum cerrado + nota libre opcional: agregable para gráficas/IA, sin perder matiz cualitativo.
- Privacidad: estos datos **nunca** se exponen en `PublicProfile` ni en exports compartibles.

---

## 4. Capa de aplicación

`src/FundedEdge.Application/Psychology/`:

```csharp
public interface IPsychologyService
{
    /// <summary>Trades del día (o rango) del usuario que aún no tienen registro emocional.</summary>
    Task<IReadOnlyList<PendingEmotionTradeDto>> GetPendingAsync(DateOnly date, CancellationToken ct = default);

    Task SaveTradeEmotionsAsync(SaveTradeEmotionsRequest request, CancellationToken ct = default);
    Task SaveDailyCheckInAsync(DailyCheckInDto dto, CancellationToken ct = default);

    /// <summary>Serie temporal y agregados para las gráficas de la página de psicología.</summary>
    Task<EmotionAnalyticsDto> GetAnalyticsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Métricas psicológicas derivadas (ver §6) para dashboard, IA y alertas.</summary>
    Task<PsychMetricsDto> GetMetricsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
```

`EmotionAnalyticsDto` (para R3) incluye como mínimo:
- `EmotionFrequencyPoint[]` — frecuencia/intensidad media por emoción y semana.
- `EmotionPerformancePoint[]` — por emoción (en `BeforeEntry`): nº trades, win rate, expectancy
  media en R, PnL neto. *La gráfica estrella: "cuánto te cuesta cada emoción".*
- `MoodCalendarDay[]` — calendario tipo heatmap: valencia media del día + PnL del día.
- `DisciplineTrendPoint[]` — % de trades con `FollowedPlan` por semana.

---

## 5. Formulario diario (R1) — UX

**Disparo:** al abrir la app, si existen trades de días anteriores o de hoy sin registro
emocional, banner no bloqueante en `Home`: *"Tienes 4 trades sin diario emocional — completarlo
lleva un minuto"*. Nunca un modal bloqueante: la culpa y la obligación matan la adherencia al
hábito. Opcional (Fase 4): recordatorio por email al cierre de sesión de mercado.

**Flujo (`EmotionSurvey.razor`, accesible desde el banner y desde `/psychology`):**
1. Check-in del día (si no existe): 3 sliders (sueño, estrés, foco) + emoción dominante. ~10 s.
2. Por cada trade del día (uno por pantalla, con contexto visible: símbolo, hora, PnL en R):
   - "¿Qué sentías **al entrar**?" → chips de emociones (multi-select, máx. 3) + intensidad.
   - "¿Y **durante** / **al salir**?" → mismos chips (opcional, se puede saltar).
   - "¿Seguiste tu plan?" → Sí / En parte / No tenía plan.
   - "¿Entrada impulsiva?" → Sí / No.
   - Nota opcional.
3. Pantalla final: refuerzo positivo + un insight inmediato si hay datos ("3.º día seguido
   registrando — tu win rate con entradas 'Calma' es del 61% vs 33% con 'FOMO'").

**Regla de oro:** todo lo del paso 2 salvo la emoción de entrada es opcional. Mejor datos
parciales sostenidos en el tiempo que datos perfectos durante una semana.

---

## 6. Cruce emociones × trades (R4): motor de diagnóstico

Dos capas complementarias — primero reglas deterministas (transparentes, testables), después la
IA narra y prioriza.

### 6.1 Detectores deterministas (`Domain/Psychology/` o `Application`)

| Detector | Regla (sobre datos ya existentes + emociones) | Diagnóstico |
|---|---|---|
| **Revenge trading** | Trade abierto < 15 min tras un trade perdedor, con tamaño ≥, **o** emoción `Vengeful`/`Frustrated` en `BeforeEntry` | "Operaste para recuperar, no por setup" |
| **FOMO confirmado** | Emoción `Fomo` en entrada + `WasImpulsive` + R-múltiplo negativo | Persecución de precio |
| **Euforia peligrosa** | `Euphoric`/`Overconfident` tras ≥ 2 ganadores seguidos + tamaño > media personal | Riesgo de devolver la racha |
| **Parálisis** | `Fearful`/`Doubtful` recurrente + duración de ganadores << duración de perdedores | Cortar ganadores por miedo |
| **Overtrading emocional** | Nº trades del día > percentil 90 personal + valencia media negativa | Operar por aburrimiento/frustración |
| **Indisciplina rentable (trampa)** | `NoPlan` o `WasImpulsive` con PnL positivo | El refuerzo más tóxico: ganar haciéndolo mal |
| **Mala racha emocional** | Valencia media móvil (7 días) negativa y descendente, o ≥ 3 días con emociones de alta activación negativa | Activar protocolo de §7 |
| **Fatiga/burnout** | `Detached` recurrente + sueño ≤ 2 + rendimiento decreciente | Recomendar descanso, no técnica |

Cada detector emite un `PsychInsight { Severity, Title, Evidence[], Recommendation }` con los
trades concretos como evidencia. **Testables unitariamente** con datos sintéticos
(`tests/`): cada regla, un test de activación y otro de no-activación.

### 6.2 Métricas derivadas (`PsychMetricsDto`)
- **Índice de tilt (0–100):** combinación ponderada de detecciones de revenge + intensidad de
  emociones de alta activación negativa + desviación de tamaño post-pérdida.
- **Score de disciplina (0–100):** % trades `FollowedPlan`, penalizado por `WasImpulsive`.
- **Coste emocional (€/R):** diferencia de expectancy entre trades con valencia positiva-baja
  activación (calma) y el resto → *"tus emociones te cuestan 0.8R por trade"*. El número que
  convierte la psicología en dinero y hace que el usuario vuelva.
- **Capital emocional (tendencia):** media móvil de valencia diaria — la "equity curve
  emocional" que se pinta junto a la monetaria.

---

## 7. Página de Psicología (R5) — `/psychology`

Nueva página Blazor `Psychology.razor` en `src/FundedEdge.Web/Components/Pages/` (con entrada
en el menú de navegación, requiere usuario autenticado como el resto).

**Layout propuesto (de arriba abajo):**
1. **Cabecera de estado:** índice de tilt, score de disciplina, coste emocional y racha de
   registro (días seguidos con diario completo), con tendencia vs período anterior.
2. **Alerta de racha emocional** (si el detector de §6.1 está activo): panel destacado con el
   protocolo de mala racha — recomendaciones escalonadas: reducir tamaño al 50%, máximo N
   trades/día, día de descanso si el índice de tilt supera umbral. Redactado sin paternalismo.
3. **Gráficas (R3):**
   - *Emoción → rendimiento*: barras por emoción de entrada con expectancy media en R y nº de
     trades (la evidencia de qué emociones tratar primero).
   - *Calendario emocional*: heatmap mensual valencia media/día con overlay del PnL diario.
   - *Equity curve emocional vs monetaria*: dos series; cuando divergen (se gana dinero con
     valencia cayendo) es predictor de blow-up — anotarlo en la gráfica.
   - *Radar de perfil emocional*: distribución de emociones del período vs período anterior.
   - *Tendencia de disciplina*: % plan seguido por semana.
4. **Insights activos:** lista de `PsychInsight` ordenada por severidad, cada uno expandible
   con sus trades-evidencia (enlace al detalle del trade).
5. **"Qué trabajar ahora":** las 1–2 emociones con mayor coste en R del período, con una pauta
   práctica cada una (p. ej., para FOMO: regla de "si el impulso llega con el precio ya en
   movimiento, espera el pullback o déjalo ir; apúntalo como trade no-tomado"). Base estática
   curada + narrativa personalizada por IA (§8).
6. **Botón "Analizar mi psicología con IA"** → genera un `AiReport` de tipo `PsychologyAnalysis`.

**Nota legal/ética:** pie visible: la funcionalidad es coaching de rendimiento sobre datos
auto-reportados, **no es diagnóstico ni tratamiento psicológico**; ante malestar sostenido,
recomendar ayuda profesional. Importante también para el tono de los prompts de IA (§8.3).

---

## 8. Integración con el motor IA (R2)

### 8.1 Contexto para informes y preguntas
`ClaudeTradingAnalystService` ya construye el contexto con KPIs agregados. Se amplía con un
bloque de psicología (agregados, **no** los logs crudos completos, para controlar tokens):

```
## Psicología (auto-reportada por el trader, últimos 30 días)
- Cobertura del diario emocional: 78% de los trades
- Check-in medio: sueño 3.2/5, estrés externo 3.8/5, foco 3.1/5
- Emociones de entrada más frecuentes: Calma 34%, FOMO 18%, Frustración 14%...
- Expectancy por emoción de entrada: Calma +0.42R (n=31) | FOMO -0.61R (n=16) | ...
- Disciplina: 71% plan seguido; 12% de trades marcados como impulsivos
- Índice de tilt: 62/100 (subiendo) | Coste emocional estimado: 0.8R/trade
- Detecciones activas: revenge trading (3 episodios, trades #.., #..), racha emocional
  negativa desde 2026-07-01
- Notas destacadas del trader (muestra): "sabía que no era mi setup pero llevaba
  dos horas sin operar" (trade #..)
```

- **Informe completo (`GenerateAnalysisReportAsync`)**: el prompt pide explícitamente una
  sección "Psicología" que cruce estos datos con los KPIs (¿las fugas de PnL coinciden con las
  emociones caras?).
- **Preguntas ad-hoc (`AskQuestionAsync`)**: mismo bloque en el contexto, de modo que "¿por qué
  pierdo los lunes?" pueda responderse también desde lo emocional.
- **Informe semanal (`WeeklyAiReportService`)**: añade un párrafo de estado emocional y, si hay
  mala racha activa, la prioriza sobre el análisis técnico.

### 8.2 Nuevo tipo de informe
```csharp
public enum AiReportKind
{
    Analysis = 0,
    AdHocQuestion = 1,
    PsychologyAnalysis = 2, // nuevo
}
```
`GeneratePsychologyReportAsync` en `ITradingAnalystService` (o servicio dedicado): informe
centrado en el patrón emocional, con historial de informes previos de psicología como memoria
("hace 3 semanas acordamos limitar a 3 trades/día: se cumplió el 60% de los días").

### 8.3 Tono del prompt (system) para la parte psicológica
Instrucciones clave: hablar como coach de rendimiento (Steenbarger, no gurú); validar la emoción
y señalar el comportamiento; evidencia concreta (trades, números) antes que consejo genérico;
una prioridad por informe, no diez; nunca lenguaje clínico/diagnóstico; si detecta señales de
malestar serio sostenido, sugerir con tacto apoyo profesional.

---

## 9. Persistencia y migración

- `Infrastructure/Persistence`: `DbSet<TradeEmotionLog>`, `DbSet<DailyMindsetCheckIn>` +
  configuraciones EF (índices: `TradeEmotionLog(TradeId)`, `DailyMindsetCheckIn(UserId, Date)`
  único).
- Migración `AddPsychologyModule`. Se aplica sola al arrancar (patrón actual de `Program.cs`).
- Borrado en cascada del log al borrar el trade; el check-in diario es independiente.
- Los datos emocionales quedan **excluidos** de `PublicProfile` y de cualquier export público.

---

## 10. Plan de fases

| Fase | Alcance | Criterio de aceptación |
|---|---|---|
| **F1 — Datos + formulario** | Entidades, migración, `IPsychologyService` (pendientes + guardado), `EmotionSurvey.razor`, banner en Home | Un usuario registra emociones de todos los trades del día en < 1 min; datos persistidos por usuario |
| **F2 — Página + gráficas** | `/psychology` con cabecera de métricas y las 3 gráficas principales (emoción→rendimiento, calendario, disciplina) | Con ≥ 10 trades registrados las gráficas son coherentes con los datos |
| **F3 — Detectores + insights** | Reglas de §6.1 con tests unitarios, `PsychInsight` en la página, protocolo de mala racha | Cada detector con test de activación/no-activación; insights enlazan a trades-evidencia |
| **F4 — IA** | Bloque de contexto en los 3 flujos IA existentes, `PsychologyAnalysis`, botón en la página, equity emocional vs monetaria y radar | El informe semanal menciona el estado emocional; informe de psicología persiste en el histórico |

Dependencias: F2–F4 dependen de F1; F3 y F4 son paralelizables. La cobertura del diario es la
métrica de éxito del módulo: si en F1 la adherencia real es baja, iterar la UX del formulario
antes de construir encima.

---

## 11. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| El usuario deja de rellenar el diario a la semana | Fricción < 20 s, todo opcional salvo emoción de entrada, insight inmediato al terminar, racha de registro visible |
| Auto-reporte sesgado (se registra "calma" siempre) | Los detectores deterministas usan también señales objetivas (tiempos, tamaños); la IA contrasta relato vs datos |
| Sensación de app "juzgona" | Copys revisados: nombrar comportamiento, no a la persona; nunca bloquear la operativa |
| Datos sensibles | Privados por usuario, fuera de perfiles públicos y exports; mismo tratamiento que ajustes de integraciones |
| Deriva a terreno clínico | Disclaimer permanente + instrucciones de tono en el prompt (§8.3) |
