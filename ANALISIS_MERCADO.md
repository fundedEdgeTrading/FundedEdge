# ANÁLISIS DE MERCADO — Posicionamiento de FundedEdge frente a la competencia

> Análisis competitivo y de necesidades del trader de cuentas de fondeo (julio 2026).
> Complementa —no sustituye— a [`GUIA_FUNCIONALIDADES_PROPUESTAS.md`](./GUIA_FUNCIONALIDADES_PROPUESTAS.md)
> y [`GUIA_FEATURE_DIFERENCIADORA.md`](./GUIA_FEATURE_DIFERENCIADORA.md): aquí se valida contra el
> mercado real qué proponer, qué priorizar y dónde está el hueco defendible.

---

## 1. Resumen ejecutivo

El mercado de herramientas para prop traders está dominado por **journals de trades** (TradeZella,
TradesViz, Tradervue, Edgewonk, Journali) que han ido añadiendo "prop firm tracking" como feature.
**Ninguno trata el prop trading como un negocio**: nadie calcula EV por evaluación, probabilidad de
ruina del bankroll, coste por cuenta fondeada ni ROI del ciclo completo evaluación→fondeo→payout.
Ese es exactamente el núcleo de FundedEdge (módulo `/risk` + dashboard de KPIs de negocio) y es un
posicionamiento defendible: *"el back-office del trader de fondeo"*, no *"otro journal más"*.

Las tres carencias que más nos separan hoy del estándar del mercado son: **(1) auto-sync de trades**
(todos los líderes lo tienen; nosotros solo CSV), **(2) tracking de reglas de la firma en tiempo
real** (drawdown trailing, límite diario, consistencia — es la feature que los competidores más
publicitan) y **(3) presencia móvil/alertas**. Las tres oportunidades donde nadie es fuerte y
podemos resaltar son: **motor de decisión de compra** (Firm Fit, ya diseñado), **planner de payouts
y consistencia** y **monitor de salud de las firmas**.

---

## 2. Mapa competitivo

### 2.1 Competidores directos (journals con features de prop firm)

| Producto | Precio | Fortaleza | Debilidad frente a FundedEdge |
|---|---|---|---|
| **TradeZella** | $35–99/mes | Marketing #1, "Prop Firm Sync" (Apex, Topstep, Earn2Trade, MyFundedFutures, FTMO, TPT), 500+ brokers, IA | Caro; visión por-trade, no de negocio; sin EV/ruina/costes |
| **TradesViz** | Free (3.000 trades/mes) + Pro barato | Auto-sync Tradovate/Rithmic/NT8; 60+ configuraciones de cuenta por firma; mejor relación calidad/precio | Sin monitor de reglas integrado en dashboard; sin capa de negocio |
| **Tradervue** | Free + $29,95/mes | Conexión directa Rithmic/Tradovate (las plataformas del futures funding); veterano y fiable | Sin soporte de reglas de prop firms; journal puro |
| **Journali** | Suscripción | Simula el *rulebook* de cada firma en tiempo real (10+ firmas preconfiguradas), stacking de evaluaciones ilimitado | Nicho pequeño; solo reglas, sin economía del negocio |
| **Edgewonk** | $169 pago único | Psicología del trading; sin suscripción; MT4/MT5 | Desktop-céntrico; sin ciclo de vida de cuentas ni payouts |
| **Traders Second Brain** | $99–349 pago único | Vive en Notion; pago único; trackers de prop firm | No es producto autónomo; sin IA ni simulación |

### 2.2 Competidores indirectos

- **PropFirmMatch / comparadores de firmas**: resuelven el *descubrimiento* ("¿qué firma compro?")
  con tablas estáticas y cupones de afiliado. No usan los datos del propio trader — ahí encaja
  nuestro **Firm Fit** (recomendación personalizada con TU pass rate y TU distribución de PnL).
- **Hojas de cálculo**: siguen siendo la herramienta real de la mayoría para payouts, costes e
  impuestos. Es competencia silenciosa y a la vez la prueba de que el "back-office" está sin
  resolver.
- **Dashboards de las propias firmas**: cada firma muestra sus reglas y su cuenta, pero nadie
  agrega la visión multi-firma ni el histórico tras perder/retirar la cuenta.

### 2.3 Conclusión del mapa

La batalla del *journal por trade* está perdida de antemano (TradeZella gasta más en marketing de
lo que nosotros facturaríamos). La batalla del **negocio del fondeo** —EV, bankroll, costes,
payouts, decisión de compra, supervivencia— está vacía. FundedEdge ya tiene el 70 % de esa base
construida (Fases 1–3); el análisis de mercado confirma que es la dirección correcta.

---

## 3. Necesidades del trader de fondeo sin resolver (validadas contra el mercado)

Dolor real documentado en la industria (ver Fuentes) y qué implica para producto:

1. **El trailing drawdown es la causa nº 1 de cuentas quemadas.** Los competidores responden con
   tracking en tiempo real. Nosotros lo tenemos *a posteriori* (simulación intra-cuenta en
   `/risk`), pero no *en vivo*. → Valida y sube la prioridad del "Semáforo de reglas" (GUIA_FUNC §2.2).
2. **Las reglas de consistencia y los caps de payout confunden y frustran.** Un solo día del 40–50 %
   del profit puede descalificar; los caps semanales bloquean retiros. Ninguna herramienta ofrece un
   *planner*: "¿cuánto puedo retirar, cuándo, y qué me lo bloquea?". → Hueco nuevo (§4.1).
3. **La mayoría de los que pierden la fondeada lo hacen el primer mes** (presión por "demostrar",
   sobre-tamaño). → Hueco nuevo: modo "primer mes fondeado" (§4.3), sinergia directa con el módulo
   de psicología ya implementado.
4. **Inestabilidad de las firmas**: 80–100 prop firms cerraron o salieron del mercado en 2024;
   retrasos y denegaciones de payout son tema constante. Nadie monitoriza la *salud de la firma*.
   → Hueco nuevo (§4.2).
5. **Fiscalidad con hojas de cálculo**: la recomendación estándar de la industria es "llevar un
   Excel de payouts y gastos". Nosotros ya persistimos costes y payouts estructurados. → Valida
   GUIA_FUNC §4.7 (informe fiscal) como quick win con demanda probada.
6. **Fricción de importación**: el estándar 2026 es auto-sync (Rithmic/Tradovate); el CSV manual se
   percibe como herramienta de segunda. → Gap técnico prioritario (§5.1).

---

## 4. Oportunidades donde nadie es fuerte (features nuevas propuestas)

### 4.1 Planner de payouts y consistencia ⭐ (diferenciador, coste medio)

El "cuándo y cuánto cobro" es la pregunta más emocional del trader fondeado y ninguna herramienta
la responde. Con los datos que ya tenemos (reglas por programa —llegarán con el catálogo de Firm
Fit F6.1—, trades diarios, payouts históricos):

- **Elegibilidad en vivo**: días operados que cuentan, buffer sobre el mínimo, distancia al
  siguiente payout por cuenta y agregado multi-firma.
- **Simulador de consistencia**: "si mañana ganas X, tu mejor día pasa a ser el N % del total —
  cap/descalificación sí/no". Inverso: "máximo que puedes ganar mañana sin romper la regla".
- **Calendario de cobros previsto** (P50/P90 vía el Monte Carlo existente) → conecta con cashflow
  del dashboard de negocio.
- Gancho de retención perfecto para el email semanal de IA ya implementado ("esta semana puedes
  solicitar $1.200 en Apex; te faltan 2 días operables en Tradeify").

### 4.2 Monitor de salud de firmas 🔥 (efecto red + SEO, coste medio)

Tras la purga de firmas 2024–2026, la confianza es EL criterio de compra. Propuesta por capas:

- **Capa editorial (barata)**: estado por firma en el catálogo compartido (`/firms`): cambios
  recientes de reglas, tiempos de payout publicados, país/regulación, señales públicas de riesgo.
- **Capa comunidad (efecto red)**: los usuarios ya registran payouts con fechas
  solicitado→recibido — agregado anónimo = **tiempo real de pago por firma**, un dato que nadie
  publica con datos verificables. Sinergia directa con el benchmarking anónimo (GUIA_FUNC §3.6).
- **Capa alerta**: "tu firma X ha cambiado la regla de consistencia / acumula retrasos de payout
  reportados" — razón de peso para tener la app abierta cada semana.
- Además alimenta Firm Fit: el score de compra puede penalizar firmas con salud deteriorada.

### 4.3 Modo "primer mes fondeado" (coste bajo, sinergia psicología)

Detección del momento de máximo riesgo del ciclo de vida (transición Evaluación→Fondeada ya es un
evento auditable en la app):

- Plan de conservación autogenerado: tamaño recomendado (½ Kelly ya calculado), objetivo hasta el
  primer payout, buffer de drawdown objetivo.
- Check-ins de psicología intensificados los primeros 20 días operables + detector de
  sobre-tamaño (tamaño medio vs. etapa de evaluación).
- El informe IA cambia de tono en esta fase: prioriza supervivencia sobre optimización.

### 4.4 Paridad mínima con el mercado en journal (no competir, no desentonar)

No hay que ganar a TradeZella en journaling, pero sí evitar el descarte inmediato en comparativas:
calendario de PnL (heatmap), curva de equity por cuenta y agregada, y filtros por hora/día/símbolo
(la base ya existe: GUIA_FUNC §3.3). Con eso el pitch "journal suficiente + negocio insuperable"
se sostiene.

---

## 5. Gaps técnicos frente al estándar 2026 (mejoras a lo existente)

### 5.1 Auto-sync de trades (crítico a medio plazo)

Todos los líderes sincronizan Tradovate/Rithmic/NinjaTrader automáticamente; nuestro CSV manual es
la mayor objeción de venta. Ruta incremental sin reescribir nada (la importación ya es idempotente
y normaliza a `CsvTradeRow`):

1. **Fase A — fricción cero con CSV**: carpeta vigilada/atajo de reimportación 1-clic + recordatorio
   semanal ("no has importado desde el martes").
2. **Fase B — API Tradovate** (REST, token del usuario): sync diario del Performance report. Cubre
   de golpe Apex/Tradeify/la mayoría del futures funding.
3. **Fase C — Rithmic** (más costoso, requiere acuerdo/SDK): solo si la tracción lo justifica.

### 5.2 Semáforo de reglas en tiempo (casi) real

Ya diseñado en GUIA_FUNC §2.2 — el mercado lo confirma como *table stakes*: es la feature que
TradeZella, TradesViz y Journali ponen en su hero. Nuestra ventaja: podemos unirlo a probabilidad
(no solo "te queda $800 de drawdown" sino "P(quemar la cuenta) al ritmo actual = 34 %", que sale
del Monte Carlo intra-cuenta ya implementado). Nadie más puede decir eso.

### 5.3 Móvil y alertas (PWA)

Los journals son desktop-first; el trader mira el móvil entre operaciones. Sin app nativa: PWA
instalable + notificaciones push/email para los eventos que ya detectamos (proximidad a drawdown,
elegibilidad de payout, informe semanal, alertas de firma). Coste contenido sobre Blazor Server y
multiplica la retención del email semanal existente.

### 5.4 Cobertura de mercados

Hoy el catálogo sembrado es futures-céntrico (ES/NQ/GC/CL, Tradovate/NT8). El 60 % del mercado de
fondeo es forex/CFD (FTMO, FundedNext, The5ers — MT4/MT5, cTrader). Decisión estratégica explícita:

- **Opción recomendada a corto plazo**: dominar el nicho futures funding (Apex/Topstep/Tradeify…)
  donde la competencia de reglas es más débil y nuestro seed/import ya encaja; declarar el foco en
  el marketing ("built for futures prop traders").
- Medio plazo: import de MT4/MT5 (HTML/CSV de statements) abre el mercado forex sin tocar el
  dominio (el modelo `Trade`/`Execution` ya es agnóstico del mercado).

### 5.5 Export y track record compartible

Está declarado fuera de alcance en el README (export PDF), pero el mercado lo usa como viral loop:
los traders comparten su curva/track record en X/Discord con marca de agua del producto. Combinado
con el track record verificado (GUIA_FUNC §3.7) es adquisición gratuita. Prioridad media.

---

## 6. Posicionamiento y precio

- **Mensaje**: *"Los journals te dicen cómo operaste. FundedEdge te dice si tu negocio de fondeo es
  rentable, cuánto bankroll necesitas y qué evaluación comprar."* Toda la comunicación debe huir de
  la comparación feature-a-feature con TradeZella y llevar la conversación a EV, ruina, costes y
  payouts — terreno donde estamos solos.
- **Precio**: TradeZella ($35–99/mes) ha dejado un hueco enorme por abajo; TradesViz gana por
  precio y Edgewonk/TSB por pago único. Mantener Starter gratuito generoso + Pro claramente por
  debajo de $35/mes maximiza la conversión desde "el Excel"; el tier Elite se justifica con lo que
  nadie más tiene (Firm Fit, benchmarks de comunidad, monitor de firmas, informes IA ilimitados).
- **SEO/contenido**: las búsquedas ganadoras del nicho son "X firm payout rules", "trailing
  drawdown calculator", "prop firm EV calculator", "is [firma] worth it". Mini-herramientas
  públicas gratuitas (calculadora de EV, simulador de drawdown) como puerta de entrada — el motor
  de simulación ya existe en `FundedEdge.Domain.Risk`.

---

## 7. Priorización recomendada (impacto de mercado ÷ esfuerzo)

1. **Semáforo de reglas + probabilidad** (§5.2) — table stakes con giro propio; base ya construida.
2. **Planner de payouts y consistencia** (§4.1) — diferenciador sin competencia, alto valor emocional.
3. **Informe fiscal de payouts/costes** (GUIA_FUNC §4.7) — quick win, demanda validada, datos ya persistidos.
4. **Fricción cero de importación → API Tradovate** (§5.1 A/B) — elimina la mayor objeción de venta.
5. **Firm Fit** (GUIA_FEATURE) — el foso a largo plazo; requiere el catálogo de programas (F6.1) que además desbloquea §4.1.
6. **Monitor de salud de firmas** (§4.2 capa editorial → comunidad) — efecto red y SEO.
7. **PWA + push** (§5.3) — multiplicador de retención de todo lo anterior.
8. **Modo primer mes fondeado** (§4.3) — barato, reduce churn del segmento que más paga.
9. **Paridad journal mínima** (§4.4) y **export compartible** (§5.5) — sostener comparativas y viralidad.

---

## 8. Fuentes

- [TradesViz — Prop Firm Journal](https://www.tradesviz.com/prop-firm-journal/)
- [TradeZella](https://www.tradezella.com/) · [TradeZella vs TradesViz](https://www.tradezella.com/vs/tradesviz) · [TradeZella vs Tradervue](https://www.tradezella.com/vs/tradervue)
- [Journali — journal para futures/prop firms](https://journali.io/)
- [Best Trading Journal for Prop Firm Traders 2026 (TSB)](https://traderssecondbrain.com/guides/best-trading-journal-for-prop-firms)
- [Best Prop Firm Trading Journals 2026 (Lunefi)](https://lunefi.com/blog/best-prop-firm-trading-journals-2026)
- [Tradezella vs Tradervue: pricing (FinancialTechWiz)](https://www.financialtechwiz.com/post/tradezella-vs-tradervue/)
- [Prop Firm Statistics 2026 (QuantVPS)](https://www.quantvps.com/blog/prop-firm-statistics) · [Atmos Funded](https://atmosfunded.com/prop-firm-statistics/)
- [Are Prop Firms Worth It in 2026? (The Prop Firm Guide)](https://thepropfirmguide.com/are-prop-firms-worth-it/)
- [Prop Firm Purge 2026 (FXNX)](https://fxnx.com/en/blog/prop-firm-purge-2026-trader-survival-guide)
- [Prop Firm Taxes 2026 (TSB)](https://traderssecondbrain.com/guides/prop-firm-taxes) · [Living From Trading](https://www.livingfromtrading.com/blog/prop-firm-taxes-in-the-usa/)
