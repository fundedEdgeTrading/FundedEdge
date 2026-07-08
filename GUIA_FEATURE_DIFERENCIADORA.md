# TrackRecord — Feature diferenciadora: «Firm Fit» (motor de decisión de compra de evaluaciones)

> Análisis de mercado y guía de implementación de la funcionalidad con mayor potencial de
> diferenciación frente a la competencia. Formato compatible con el roadmap de
> `GUIA_MONETIZACION_Y_MARKETING.md`: pensada para ejecutarse como fases F6.x, una por PR,
> con build y tests en verde como DoD.

---

## 1. El hueco en el mercado

El software alrededor del funded trading se divide hoy en dos familias, y ninguna cruza sus datos:

| Familia | Ejemplos | Qué hacen | Qué NO hacen |
|---|---|---|---|
| Journals de trading | TraderSync, Tradezella, Edgewonk, TradesViz | Analizan tus trades (win rate, expectancy, setups) | Ignoran la **economía del negocio de evaluaciones**: costes, pass rate, payouts, reglas de firma |
| Comparadores de prop firms | PropFirmMatch, agregadores de descuentos | Comparan precios y reglas **genéricas** de las firmas | No saben **nada de ti**: te recomiendan lo mismo que a cualquiera |

TrackRecord ya tiene lo que ninguna de las dos familias tiene a la vez: **la distribución real
de PnL por trade del usuario Y la economía observada de su negocio** (pass rate, costes,
payouts por firma). La feature diferenciadora es cruzarlos con las **reglas concretas de cada
programa de evaluación** para responder la única pregunta que de verdad mueve dinero:

> **«¿Qué evaluación me conviene comprar ahora, de qué firma, de qué tamaño — y cuántas a la vez?»**

Hoy el trader decide esto por intuición, hilos de Discord o el descuento de la semana. Nadie
se lo calcula con SUS datos. Eso es «Firm Fit».

## 2. La feature: Firm Fit

Un motor prescriptivo (no solo descriptivo) con tres entregables visibles:

1. **Firm Fit Score por programa.** Un catálogo de programas de evaluación (firma, tamaño,
   coste, profit target, max drawdown y su tipo, daily loss, regla de consistencia, días
   mínimos, reglas de payout). Para cada programa, el motor Monte Carlo existente
   (`AccountRuleSimulator`/`BankrollSimulator`) simula la operativa real del usuario contra
   las reglas del programa y devuelve: **P(pasar), coste esperado por cuenta fondeada, EV,
   tiempo mediano hasta primer payout y Fit Score 0–100**. Ranking personalizado: «para TU
   operativa, la evaluación de 50K de la firma A tiene EV +212 €; la de 100K de la firma B,
   EV −87 €».
2. **Sensibilidad a las reglas.** El diferencial técnico: simular el efecto de cada regla
   sobre TU distribución (el trailing drawdown castiga distinta operativa que el EOD; la
   regla de consistencia castiga a quien concentra el PnL en pocos días). Mostrar el
   desglose: «pierdes un 14 % de P(pasar) por el trailing; con drawdown EOD tu P(pasar)
   sube al 41 %».
3. **Plan de compra.** Reutilizando el planner de bankroll: dado tu bankroll y tu cadencia,
   la asignación sugerida («2 evaluaciones/mes de la firma A» — fracción de Kelly, P(ruina)
   < 5 %) y el momento de escalar. Cierra el ciclo: registrar → medir → **decidir**.

### Por qué diferencia muchísimo

- **De descriptivo a prescriptivo.** Los journals te dicen qué pasó; Firm Fit te dice qué
  comprar. Es la diferencia entre un contable y un asesor.
- **Personalizado con datos reales, no marketing.** Los comparadores ordenan por precio y
  descuento; Firm Fit ordena por EV calculado con la distribución de PnL del usuario. Es
  inimitable sin tener sus datos — y sus datos ya viven aquí.
- **Foso creciente.** Con consentimiento explícito y agregación anónima (fase posterior),
  los pass rates observados por firma/programa de toda la base de usuarios se convierten en
  un dataset que ningún competidor puede replicar («pass rate real de la firma A en cuentas
  de 50K: 23 %, no el 8 % del folclore de Discord»).
- **Alineado con la monetización existente.** Encaja como feature Pro/Elite (§3 de la guía
  de monetización): el semáforo básico en Starter, el ranking completo y la sensibilidad a
  reglas en Pro, el plan de compra y los benchmarks agregados en Elite.
- **Motor ya construido.** El 70 % del trabajo duro (Monte Carlo, EV, Kelly, bootstrap) ya
  existe y está testeado; la inversión marginal es catálogo de reglas + rule engine + UI.

## 3. Implementación por fases

### F6.1 — Catálogo de programas y rule engine (dominio)

**Objetivo:** modelar programas de evaluación con sus reglas y extender el simulador para
respetarlas.

- `Domain/Entities/EvaluationProgram.cs`: firma (FK a `PropFirm` o catálogo global sembrado),
  tamaño, coste, activación, profit target, max drawdown + `DrawdownType` (ya existe:
  Trailing/EndOfDay/Static), daily loss limit, regla de consistencia (% máx. del target en un
  día), días mínimos, buffer/cap de payout.
- Extender el simulador intra-cuenta para aplicar daily loss y consistencia sobre la
  secuencia muestreada de trades agrupada por día (ya se muestrea la distribución del
  usuario; falta el calendario diario).
- **DoD:** tests de dominio con distribuciones sintéticas donde cada regla cambia P(pasar)
  en la dirección esperada (p.ej. mismo PnL total concentrado en 2 días viola consistencia).

### F6.2 — Firm Fit Score y ranking (aplicación + UI)

**Objetivo:** página `/firm-fit` con el ranking personalizado.

- `IFirmFitService.RankProgramsAsync()`: por programa → P(pasar), EV con IC (bootstrap ya
  existente), coste por fondeada, meses hasta payout, score compuesto (EV normalizado ×
  confianza por tamaño de muestra).
- UI: tabla ranking + tarjeta de desglose por reglas («qué regla te mata») + aviso honesto
  de muestra insuficiente (< 20 trades ⇒ solo semáforo, coherente con el tono de marca:
  «estadística honesta, no promesas»).
- Gating por plan (`IPlanService`): Starter ve top-1 con semáforo; Pro ve ranking completo.
- **DoD:** tests de servicio (ranking estable con seed fija, gating por plan) + página
  localizada ES/EN desde el primer commit (lección del PR #18).

### F6.3 — Plan de compra y «next best action»

**Objetivo:** conectar Firm Fit con el planner de bankroll: dado bankroll y programa
elegido, cuántas evaluaciones/mes con P(ruina) < 5 % y ½ Kelly; tarjeta «Tu siguiente paso»
en el dashboard.

### F6.4 (posterior, opcional) — Benchmarks agregados anónimos (Elite)

Pass rate y tiempo-a-payout observados por programa sobre la base de usuarios, con
consentimiento opt-in explícito, k-anonimato (mínimo N usuarios por celda) y revisión de
privacidad antes de publicar nada. Es el foso a largo plazo; no bloquea F6.1–F6.3.

## 4. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Muestra pequeña ⇒ recomendaciones ruidosas | IC visibles siempre; bajo N solo semáforo, nunca ranking «preciso» |
| Catálogo de reglas desactualizado | Versionar programas con `EffectiveFrom`; edición manual + fecha «verificado el» visible |
| Percepción de recomendación financiera | Mismo descargo que el módulo de riesgo: marco orientativo, no promesa; mostrar supuestos siempre |
| Firmas cambian reglas para «romper» el modelo | El modelo es del usuario, no de la firma: re-simular es un clic |

## 5. KPIs de éxito

- % de usuarios Pro/Elite que visitan `/firm-fit` semanalmente (objetivo: > 40 %).
- Conversión Starter→Pro atribuida a la página (evento en el CTA del gating).
- Retención a 8 semanas de usuarios que usaron el plan de compra vs. los que no.

---

*Resumen ejecutivo: la competencia o mira tus trades sin entender el negocio de fondeo, o
mira las firmas sin conocerte. Firm Fit cruza ambas cosas con un motor que ya está
construido y testeado, convierte la app de cuaderno de contabilidad en asesor de compras, y
crea con el tiempo un dataset propietario imposible de copiar.*
