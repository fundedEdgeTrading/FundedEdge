# PLAN DE IMPLEMENTACIÓN — Conclusiones de ANALISIS_MERCADO.md

> Traduce las conclusiones de [`ANALISIS_MERCADO.md`](./ANALISIS_MERCADO.md) a fases de
> implementación concretas sobre el código existente (Fases 1–3 ya completas). Usa numeración
> propia **M1–M7** para no colisionar con la Fase 4 de
> [`GUIA_IMPLEMENTACION.md`](./GUIA_IMPLEMENTACION.md) ni con las F6.x de
> [`GUIA_FEATURE_DIFERENCIADORA.md`](./GUIA_FEATURE_DIFERENCIADORA.md), a las que referencia.

---

## 0. Qué es implementable y qué no

**Implementable en la aplicación** (cubierto por las fases M1–M7):

- Semáforo de reglas en vivo con probabilidad (ANALISIS §5.2)
- Informe fiscal de payouts/costes (ANALISIS §3.5)
- Fricción cero de importación y auto-sync Tradovate (ANALISIS §5.1)
- Catálogo de programas + rule engine (base de Firm Fit, GUIA_FEATURE F6.1)
- Planner de payouts y consistencia (ANALISIS §4.1)
- Firm Fit score y plan de compra (GUIA_FEATURE F6.2–F6.3)
- Monitor de salud de firmas (ANALISIS §4.2)
- Modo "primer mes fondeado" (ANALISIS §4.3)
- PWA + notificaciones push (ANALISIS §5.3)
- Paridad mínima de journal y export compartible (ANALISIS §4.4, §5.5)
- Mini-herramientas públicas para SEO (calculadora EV / simulador drawdown, ANALISIS §6)

**No implementable en código** (decisiones de negocio/marketing, fuera de este plan): mensaje de
posicionamiento, política de precios de los tiers, contenido SEO editorial, y la decisión
estratégica futures vs. forex (ANALISIS §5.4) — aunque M4 y M7 dejan preparado el terreno técnico
para ambas.

---

## M1 — Quick wins sin dependencias (≈ 1–2 semanas)

Tres entregables independientes que solo usan datos ya persistidos.

### M1.1 Semáforo de reglas v1 (por cuenta)

- **Dominio**: añadir a `FundedAccount` los campos de regla que faltan: `DailyLossLimit`,
  `ConsistencyMaxDayPct`, `MinTradingDays` (nullable; el profit target y el drawdown
  trailing/EOD/estático ya existen). Migración EF.
- **Aplicación**: servicio `AccountRuleStatusService` que, con los trades del día y el histórico,
  calcula por cuenta: distancia al drawdown (con high-water mark para trailing), consumo del
  límite diario, % del mejor día sobre el profit total, días operados vs. mínimo.
- **UI**: tarjeta "Estado de reglas" en `/accounts/{id}` y chips de color (verde/ámbar/rojo) en la
  lista de cuentas y el dashboard. Umbral ámbar configurable (por defecto 70 % consumido).
- **Giro diferenciador**: junto a la distancia en $, mostrar la P(quemar la cuenta) del Monte Carlo
  intra-cuenta ya implementado en `FundedEdge.Domain.Risk` — reutilización directa, sin código
  nuevo de simulación.
- **Tests**: unitarios del cálculo de estado con datasets sintéticos (trailing vs. EOD, día en
  pérdidas, cuenta recién abierta sin trades).

### M1.2 Informe fiscal de payouts y costes

- **Aplicación**: `TaxReportService` que agrega por año natural (y por trimestre): payouts
  recibidos (fecha, bruto, firma), costes deducibles por categoría (evaluación, activación, reset,
  cuota), neto. Los datos ya están en `Payout` y las entidades de coste.
- **UI**: sección en el dashboard de negocio o página `/reports` con selector de año y botón
  **Exportar CSV** (suficiente para gestoría; el PDF puede esperar a M7).
- **Tests**: agregación con años cruzados (payout solicitado en diciembre, recibido en enero).

### M1.3 Fricción cero de importación CSV

- Botón "Reimportar último archivo" en `/accounts/{id}` (recordar nombre/mapeo del último import
  por cuenta; la idempotencia ya garantiza que no duplica).
- Recordatorio semanal por email "no has importado trades desde el {fecha}" — reutiliza el canal
  SMTP y el patrón del `WeeklyAiReportService` existente.

**Criterio de salida M1**: un usuario ve en rojo una cuenta a punto de romper una regla, exporta
su CSV fiscal del año y recibe recordatorio si deja de importar.

---

## M2 — Catálogo de programas y rule engine (≈ 2 semanas) — F6.1

Fase habilitadora: desbloquea M3 (planner de payouts), M5 (Firm Fit) y la v2 del semáforo.

- **Dominio**: entidad `FirmProgram` colgando de `Firm` (catálogo compartido, como firmas e
  instrumentos): coste de evaluación/activación/reset, cuota mensual, tamaño, profit target, tipo
  y cuantía de drawdown, límite diario, regla de consistencia, días mínimos, y **política de
  payout** (frecuencia, mínimo de días entre payouts, cap por retiro, split %, umbral de buffer).
- **Seed**: programas vigentes de Lucid Trading, Tradeify y Apex (las 3 firmas ya sembradas);
  campo `RulesLastVerifiedAt` para saber cuándo se revisó cada programa.
- **Aplicación/UI**: CRUD en `/firms/{id}` (admin) y selector de programa al crear cuenta — la
  cuenta hereda las reglas del programa (los campos de M1.1 pasan a rellenarse solos, pero siguen
  siendo sobrescribibles por cuenta).
- **Semáforo v2**: `AccountRuleStatusService` lee del programa cuando la cuenta tiene uno asignado.

**Criterio de salida M2**: crear una cuenta "Apex 50K" rellena todas sus reglas automáticamente y
el semáforo las vigila sin configuración manual.

---

## M3 — Planner de payouts y consistencia (≈ 2 semanas) — depende de M2

El diferenciador sin competencia identificado en ANALISIS §4.1.

- **Aplicación**: `PayoutPlannerService` por cuenta fondeada: elegibilidad actual (días que
  cuentan, buffer sobre el mínimo, distancia en $ y en días al siguiente payout), importe máximo
  retirable según cap/split, y **simulador de consistencia** bidireccional ("si mañana ganas X…"
  / "máximo que puedes ganar mañana sin romper la regla").
- **Calendario de cobros**: fecha estimada del siguiente payout P50/P90 muestreando la
  distribución real de PnL diario con el motor Monte Carlo existente.
- **UI**: página `/payouts` (vista agregada multi-firma: "puedes solicitar $X hoy") + bloque en la
  ficha de cuenta.
- **IA/retención**: añadir al email semanal existente la línea de elegibilidad ("esta semana
  puedes solicitar $1.200 en Apex; te faltan 2 días operables en Tradeify") — solo tocar el
  contexto del prompt.
- **Tests**: elegibilidad con reglas de consistencia límite, caps y cuentas sin payout previo.

**Criterio de salida M3**: la pregunta "¿cuánto puedo retirar y cuándo?" tiene respuesta en un
clic para todas las cuentas.

---

## M4 — Auto-sync Tradovate (≈ 2–3 semanas)

Elimina la mayor objeción de venta (ANALISIS §5.1, fase B). El diseño técnico ya existe en
GUIA_IMPLEMENTACION §5 y §7.

- Cliente REST Tradovate con token del usuario (guardado cifrado; patrón de secretos ya
  establecido) + `TradeSyncService` diario que normaliza a las mismas `Execution` del import CSV
  (idempotencia ya resuelta).
- Estado de sync visible por cuenta (última sincronización, huecos detectados vs. CSV).
- **Alcance explícitamente excluido**: Rithmic y AddOn NT8 push (fase C del análisis — solo si la
  tracción lo justifica; el CSV de NT8 sigue cubriendo ese caso).
- **Tests**: contrato con respuestas grabadas (WireMock.Net), renovación de token, idempotencia.

**Criterio de salida M4**: una cuenta Tradovate/Apex se mantiene al día sin tocar ningún CSV.

---

## M5 — Firm Fit: score y plan de compra (≈ 2–3 semanas) — F6.2 + F6.3, depende de M2

- Score de ajuste por programa combinando el catálogo de M2 con las métricas propias del usuario
  ya calculadas (pass rate, distribución de PnL, EV, P(ruina) intra-cuenta).
- Ranking "¿qué evaluación me conviene comprar?" + "next best action" integrada con el planner de
  bankroll existente en `/risk`.
- Detalle completo en GUIA_FEATURE_DIFERENCIADORA §3 (F6.2 y F6.3) — este plan solo fija su
  posición en la secuencia: después de M3 porque el planner de payouts da valor inmediato a más
  usuarios (todos los fondeados) que el motor de compra (compradores recurrentes).

---

## M6 — Monitor de salud de firmas (≈ 2 semanas, por capas)

- **Capa editorial**: campos en `Firm` (compartida): estado (activa/en observación/cerrada), país,
  notas de cambios de regla con fecha, enlace a política de payout. Visible en `/firms`.
- **Capa comunidad**: agregado anónimo del tiempo solicitado→recibido de los payouts ya
  registrados por los usuarios (mediana y P90 por firma, mínimo N usuarios para publicar).
  Reutiliza el enfoque de benchmarking anónimo (GUIA_FUNCIONALIDADES §3.6) y alimenta el score de
  M5 (penalizar firmas deterioradas).
- **Capa alertas**: aviso in-app/email a los usuarios con cuentas en una firma cuyo estado cambia.
- **Privacidad**: solo agregados, opt-out disponible, umbral mínimo de muestra.

---

## M7 — Retención y alcance (≈ 2–3 semanas, entregables independientes)

- **M7.1 PWA + push**: manifest + service worker sobre Blazor Server, Web Push para los eventos
  que ya detectamos (semáforo en ámbar/rojo, elegibilidad de payout, informe semanal, alertas de
  firma). El email sigue siendo fallback.
- **M7.2 Modo "primer mes fondeado"**: al registrar la transición Evaluación→Fondeada (evento ya
  auditable), activar durante los primeros 20 días operables: plan de conservación (½ Kelly ya
  calculado), detector de sobre-tamaño (tamaño medio vs. etapa de evaluación), check-ins de
  psicología intensificados y tono "supervivencia" en el prompt del informe IA.
- **M7.3 Paridad mínima de journal**: heatmap-calendario de PnL y curva de equity por
  cuenta/agregada con filtros hora/día/símbolo (los datos y KPIs ya existen; es UI).
- **M7.4 Export compartible**: imagen/PDF de curva + KPIs con marca de agua FundedEdge para
  redes/Discord (viral loop; base del track record verificado de GUIA_FUNCIONALIDADES §3.7).
- **M7.5 (opcional) Mini-herramientas públicas**: páginas sin login (calculadora de EV, simulador
  de drawdown) reutilizando `FundedEdge.Domain.Risk` — puerta de entrada SEO.

---

## Secuencia y dependencias

```
M1 (quick wins) ──────────────────────────────┐
M2 (catálogo programas) ──> M3 (payouts) ──> M5 (Firm Fit) ──> M6 (salud firmas)
M4 (Tradovate sync)  [independiente, en paralelo desde M2]
M7 (retención/alcance) [tras M3; sus 5 entregables son independientes entre sí]
```

Estimación total: ~3–4 meses a ritmo de las fases anteriores. Cada fase termina con migración
aplicada, tests de la capa de aplicación/dominio en verde y criterio de salida verificable en la
demo (`DemoDataSeeder` ampliado cuando aplique).
