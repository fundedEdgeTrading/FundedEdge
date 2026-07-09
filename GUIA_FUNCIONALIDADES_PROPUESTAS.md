# GUÍA — Funcionalidades propuestas sobre los datos e infraestructura existentes

> Objetivo: convertir FundedEdge en la herramienta de referencia tanto para traders que
> **empiezan en prop firms** como para traders **ya consolidados** que gestionan varias cuentas
> de fondeo como un negocio. Todas las propuestas de este documento se apoyan en datos que **ya
> capturamos** y en infraestructura que **ya existe** — no requieren nuevas fuentes de datos
> externas salvo donde se indica.

---

## 1. Inventario: qué tenemos ya

| Activo | Dónde vive | Qué aporta |
|---|---|---|
| Trades round-turn con R-múltiplo, tags, notas y ejecuciones | `Trade`, `Execution`, `TradeBuilder`, `ManualTradeFactory` | Base de todo el análisis de operativa |
| Ciclo de vida de cuentas de fondeo (etapas, eventos, resets) | `TradingAccount`, `AccountStage`, `AccountEvent` | Historia completa de cada cuenta: evaluación → fondeada → quemada/payout |
| Costes y payouts por cuenta | `AccountCost`, `Payout`, `CostKind`, `PayoutStatus` | La contabilidad real del "negocio de fondeo" |
| Catálogo de firmas y programas | `PropFirm`, `EvaluationProgram` | Reglas, precios y objetivos de cada firma |
| KPIs agregados | `DashboardKpis` (`BusinessKpis`, `TradingKpis`, `EquityCurvePoint`, `MonthlyCashflowPoint`, `TagPerformanceDto`) | Métricas ya calculadas y listas para explotar |
| Simuladores de riesgo | `AccountSimulator`, `BankrollSimulator`, `EvCalculator`, `ProgramFitSimulator` | Motor Monte Carlo / EV ya implementado |
| Motor IA (Claude) | `ClaudeTradingAnalystService`, `WeeklyAiReportService`, `AiReport` | Informes completos, preguntas ad-hoc, informe semanal automático, histórico persistido |
| Integraciones | Tradovate, NinjaTrader (webhooks), `ProcessedWebhookEvent` | Ingesta automática de operativa |
| Perfil público | `PublicProfile` | Base para funcionalidades sociales/verificación |
| Billing y planes | `PlanTier`, Infrastructure/Billing | Palanca de monetización para gating de features |
| Multi-divisa, Identity + Google, export | `Currency`, páginas `Export`, `Settings` | Plataforma madura para crecer encima |

---

## 2. Funcionalidades para traders que EMPIEZAN en prop firms

### 2.1 Asistente "¿Qué firma me conviene?" (quick win ⭐)
Un wizard de onboarding que pregunta estilo (scalping/intradía/swing), instrumento, capital
disponible y tolerancia al riesgo, y usa el **`ProgramFitSimulator` ya existente** contra el
catálogo `PropFirm`/`EvaluationProgram` para rankear programas por probabilidad de pasar la
evaluación y coste esperado hasta el primer payout. La página `FirmFit` ya apunta en esta
dirección: se trata de convertirla en un flujo guiado con lenguaje para principiantes.

### 2.2 Semáforo de reglas en tiempo real
Con los trades del día y las reglas del programa (`EvaluationProgram`: drawdown, pérdida diaria,
consistencia), mostrar en el dashboard un semáforo por cuenta:
- 🟢 margen sobrado, 🟡 a menos de X% del límite, 🔴 límite en peligro hoy.
- "Te quedan $340 de pérdida diaria antes de incumplir la regla" es la frase que un novato
  necesita ver **antes** de abrir el siguiente trade.

### 2.3 Simulador de supervivencia de evaluación
Reutilizar `AccountSimulator` para responder: *"con tu win rate y R-múltiplo actuales, ¿qué
probabilidad tienes de pasar esta evaluación sin quemarla?"*. Mostrar la distribución Monte Carlo
en la página de progreso de cuenta (`AccountProgress`). Es el mismo motor, con un framing
pedagógico brutal para retención.

### 2.4 Checklist pre-mercado y post-mercado
Rutina diaria configurable (¿revisaste el calendario económico? ¿definiste riesgo máximo del
día?). Se guarda por día y se cruza con el resultado: "los días que completas el checklist tu
expectancy es +0.4R mayor". Sinergia directa con la funcionalidad de psicología
(ver `GUIA_PSICOLOGIA_TRADING.md`).

### 2.5 Modo "primera cuenta": educación contextual
Tooltips y micro-lecciones junto a cada KPI (¿qué es el R-múltiplo? ¿por qué importa el trailing
drawdown?). Contenido estático + posibilidad de "explícamelo con mis datos" delegando en
`AskQuestionAsync` del motor IA ya existente.

### 2.6 Alertas de coste hundido
Los novatos encadenan resets sin darse cuenta del agujero. Con `AccountCost` + `CostKind` ya
registramos cada reset/suscripción: alertar cuando el gasto acumulado en una firma supera el
payout esperado ("llevas $840 en resets de Apex y $0 retirados — el EV de seguir es negativo,
mira estas alternativas") usando `EvCalculator`.

---

## 3. Funcionalidades para traders EXPERTOS / multi-cuenta

### 3.1 P&L de negocio por firma (quick win ⭐)
Ya tenemos costes, payouts y trades por cuenta. Falta la vista pivotada: ROI por firma, coste
medio por cuenta fondeada conseguida, tiempo medio evaluación→payout, tasa de quema por
programa. Es la cuenta de resultados del "negocio de fondeo" que ningún journal generalista
ofrece.

### 3.2 Copy-risk entre cuentas (gestión de granja de cuentas)
Los traders avanzados operan 3–10 cuentas replicando la misma señal. Funcionalidades:
- Vista consolidada de exposición simultánea entre cuentas.
- Detección de divergencia: "la cuenta B se desincronizó de la A el martes (slippage/fallo de copia)".
- Planificador de payouts: qué cuenta llevar al payout y cuál mantener en colchón, con
  `BankrollSimulator` proyectando el cashflow.

### 3.3 Analítica temporal avanzada
Con `OpenedAt`/`ClosedAt` de cada trade:
- Heatmap de expectancy por hora del día y día de la semana.
- Duración media de ganadores vs perdedores (el clásico "corto ganadores, dejo correr perdedores" cuantificado).
- Rendimiento en días de noticias de alto impacto (fase 2: requiere calendario económico externo).

### 3.4 MAE/MFE y calidad de ejecución
Las `Execution` ya se persisten desde Tradovate/NinjaTrader. Con ellas: excursión máxima adversa
y favorable por trade, eficiencia de salida ("capturas el 42% del movimiento disponible"), y
análisis de piramidación/parciales. Métricas que los expertos hoy calculan a mano en Excel.

### 3.5 Score de consistencia por programa
Muchas firmas exigen reglas de consistencia (ningún día > X% del beneficio total). Calcularlo en
vivo por cuenta y avisar antes de solicitar payout: "hoy llevas el 34% del profit del período;
la regla de Tradeify exige < 30% — reduce tamaño o para".

### 3.6 Benchmarking anónimo (efecto red 🔥)
Con múltiples usuarios: percentiles anónimos por programa ("tu win rate en evaluaciones NQ de
Apex está en el percentil 71"; "el 18% de usuarios pasa esta evaluación al primer intento").
Nadie más tiene este dato. Es el foso competitivo natural del producto y una razón para que los
expertos suban sus datos aunque ya tengan journal.

### 3.7 Track record verificado y compartible
`PublicProfile` ya existe. Extensión: página pública con equity curve, KPIs y payouts
**verificados por la ingesta automática** (badge "datos importados de broker, no editados").
Útil para traders que buscan capital privado o venden formación — y marketing orgánico gratuito
para FundedEdge (cada perfil compartido es un anuncio).

---

## 4. Funcionalidades transversales (ambos públicos)

### 4.1 IA proactiva por eventos (quick win sobre `WeeklyAiReportService` ⭐)
Ya generamos informe semanal. Añadir disparadores por evento:
- racha de 3+ días perdedores → mini-informe de contención;
- cuenta a < 20% del drawdown máximo → plan de emergencia;
- primer payout conseguido → informe de consolidación ("qué hiciste distinto este mes").
Misma infraestructura (`ITradingAnalystService`), nuevos `AiReportKind`.

### 4.2 Chat IA sobre un trade concreto
Desde el detalle de un trade, "pregunta a la IA sobre este trade" con el contexto del trade +
estadísticas de trades similares (mismo tag/instrumento/hora). Reutiliza `AskQuestionAsync`
enriqueciendo el contexto.

### 4.3 Detección de tilt y sobreoperativa (sinergia con psicología)
Detección determinista con datos ya disponibles: trade abierto < N minutos después de una
pérdida con tamaño ≥ al anterior (revenge trading), nº de trades del día > percentil 90 personal
(overtrading), doblar tamaño tras racha ganadora (euforia). Ver diseño completo en
`GUIA_PSICOLOGIA_TRADING.md` — allí se cruza con emociones auto-reportadas.

### 4.4 Objetivos y hábitos con seguimiento
Objetivos medibles sobre KPIs existentes ("máx. 3 trades/día", "riesgo ≤ 1R", "no operar tras
-2R diario") con seguimiento automático desde los datos importados y racha de cumplimiento.
Gamificación ligera: badges por hitos reales (primera evaluación pasada, primer payout, 30 días
respetando el plan).

### 4.5 Calendario del negocio
Vista calendario unificada: renovaciones de suscripción (`AccountCost` recurrente), ventanas de
payout por firma, días operados con resultado (heatmap mensual de PnL). Todo son datos ya
persistidos, solo falta la proyección temporal.

### 4.6 Import CSV universal + más brokers
Para captar usuarios cuya plataforma no es Tradovate/NinjaTrader: import CSV con mapeo asistido
(y en plan de pago, mapeo automático vía IA). Cada broker soportado es un canal de adquisición.

### 4.7 Informe mensual "para Hacienda / para tu socio"
Export PDF con PnL, costes deducibles, payouts cobrados por divisa (`Currency` ya soportado).
Los traders fondeados son autónomos con contabilidad caótica: esto solo ya justifica una
suscripción. Se apoya en la página `Export` existente.

---

## 5. Priorización sugerida

| Prioridad | Funcionalidad | Esfuerzo | Palanca |
|---|---|---|---|
| P0 | Psicología + emociones (guía dedicada) | M | Diferenciación única |
| P0 | Semáforo de reglas en vivo (2.2) + score consistencia (3.5) | M | Dolor nº1 del fondeado |
| P0 | P&L de negocio por firma (3.1) | S | Ya está el 80% del dato |
| P1 | Asistente de elección de firma (2.1) | S | Adquisición de novatos |
| P1 | IA proactiva por eventos (4.1) | S | Reutiliza motor existente |
| P1 | Simulador de supervivencia (2.3) | S | Reutiliza `AccountSimulator` |
| P1 | Detección de tilt (4.3) | M | Gancho hacia psicología |
| P2 | MAE/MFE (3.4), heatmaps temporales (3.3) | M | Retención de expertos |
| P2 | Track record verificado (3.7) | M | Marketing orgánico |
| P2 | Import CSV universal (4.6) | M | Adquisición |
| P3 | Benchmarking anónimo (3.6) | L | Foso competitivo (requiere masa) |
| P3 | Copy-risk multi-cuenta (3.2) | L | Nicho experto de alto valor |

**Gating por plan (`PlanTier`) sugerido:** gratis = journal + KPIs básicos + 1 cuenta; Pro =
multi-cuenta, IA, psicología, simuladores; Elite = benchmarking, copy-risk, informes fiscales.

---

*Documento generado como propuesta de producto; ver `GUIA_MONETIZACION_Y_MARKETING.md` para la
estrategia comercial y `GUIA_PSICOLOGIA_TRADING.md` para el diseño detallado de la funcionalidad
de psicología.*
