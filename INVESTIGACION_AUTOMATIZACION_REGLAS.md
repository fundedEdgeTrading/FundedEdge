# Investigación: Automatización de la obtención de reglas de prop firms

**Fecha:** 2026-07-11
**Estado:** Investigación — pendiente de decisión de implementación

---

## 1. Problema

El catálogo de programas (`EvaluationProgram`) y firmas (`PropFirm`) se mantiene **a mano**:
cada vez que una firma cambia condiciones (drawdown, targets, payouts, consistencia…) hay que
detectarlo por cuenta propia y corregir o crear los registros manualmente. Consecuencias:

- Trabajo recurrente y propenso a errores.
- Riesgo de catálogo **desactualizado** sin saberlo → el motor Firm Fit recomienda con reglas falsas.
- El problema es real y frecuente: en marzo de 2026 Apex lanzó "Apex 4.0", que **reemplazó por
  completo** su modelo (pago único, dos tipos nuevos de trailing drawdown, eliminación de 6 reglas
  legacy como la MAE o el 5:1 RR). Un cambio así invalida de golpe todos los programas de esa firma
  en el catálogo. ([fuente](https://phidiaspropfirm.com/education/apex-trader-funding-4-0-explained))

**Ventaja de partida:** el dominio ya está preparado para esto. `EvaluationProgram` se versiona con
`EffectiveFrom` + `IsActive` (nunca se borra, se desactiva y se crea la versión nueva), y el schema
de reglas ya está definido con precisión (evaluación, fase fondeada y payout). La automatización
solo tiene que *alimentar* ese mecanismo existente.

---

## 2. Objetivo

Un sistema que, con mínima intervención:

1. **Detecte** cuándo una firma cambia reglas o precios.
2. **Extraiga** las reglas nuevas en el formato estructurado de `EvaluationProgram`.
3. **Proponga** el cambio (nueva versión del programa) para revisión.
4. Tras aprobación (o automáticamente con confianza alta), **publique** la nueva versión con su
   `EffectiveFrom`, desactivando la anterior.

---

## 3. Hallazgos de la investigación

### 3.1 No existen APIs oficiales de reglas

Las prop firms de futuros (Apex, Tradeify, Lucid, TopStep…) **no publican APIs** con sus reglas.
Las reglas viven en páginas de marketing, help centers / FAQs y anuncios (email, Discord). Las APIs
que sí existen en el sector son de ejecución/copias de trading, no de catálogo de condiciones.
**Opción "API oficial": descartada.**

### 3.2 Agregadores: los datos existen, pero sin API pública

Hay terceros que ya mantienen exactamente el dataset que necesitamos:

| Agregador | Qué tiene | API pública |
|---|---|---|
| [PropFirmMatch](https://propfirmmatch.com/futures) | Reglas por firma y **registro de cambios de reglas con fechas** | No documentada |
| [TradesViz](https://www.tradesviz.com/blog/prop-firm-compliance-tracking/) | Base de datos de reglas de ~20 firmas, 65 configuraciones de cuenta | No documentada |
| QuantCrawler, PropScorer, DamnPropFirms | Fichas de reglas por firma actualizadas | No documentada |

Implicación: **la información está estructurada en algún sitio**, pero acceder programáticamente
requiere o bien un acuerdo/partnership de datos (email, coste desconocido, dependencia de tercero)
o bien scrapearlos a ellos (frágil, y sus ToS probablemente lo prohíben).

Interesante como *fuente secundaria de verificación*: la página de "rule changes" de PropFirmMatch
sirve como señal de alerta barata de que algo cambió, aunque no como fuente primaria.

### 3.3 El enfoque técnico estándar en 2026: extracción schema-driven con LLM

El patrón dominante para este problema es **scraping + extracción con LLM contra un JSON schema
estricto** ([ByteTunnels](https://bytetunnels.com/posts/llm-powered-data-extraction-schema-driven-scraping-with-structured-output/),
[Firecrawl](https://www.firecrawl.dev/blog/best-web-extraction-tools)):

- Se define el schema exacto del dato deseado (nosotros **ya lo tenemos**: los campos de
  `EvaluationProgram`) y el LLM extrae desde el HTML/Markdown crudo.
- Elimina el mantenimiento de selectores CSS/XPath que se rompen con cada rediseño — el punto
  débil clásico del scraping tradicional, crítico aquí porque son ~10-20 sitios heterogéneos.
- Buenas prácticas: convertir HTML → Markdown y podar el DOM antes de enviar al LLM (reduce
  tokens ~98 % manteniendo precisión), exigir `required` en el schema, y pedir **citas literales
  del texto fuente** por cada campo extraído para poder verificar.
- Herramientas: Claude API con structured outputs (schema propio, control total), o servicios
  gestionados tipo Firecrawl `/extract` / ScrapeGraphAI si se prefiere delegar el fetching
  (manejan anti-bot/Cloudflare, coste por página).

### 3.4 Detección de cambios es la mitad del problema

No basta con extraer una vez: hay que saber **cuándo re-extraer**. Patrón estándar:

1. Registrar las URLs fuente por firma (página de reglas, FAQ, pricing).
2. Fetch periódico (diario es suficiente; las reglas no cambian intradía).
3. Normalizar (HTML → Markdown, quitar navegación/fechas dinámicas) y **hashear** el contenido.
4. Solo si el hash cambia → pipeline de extracción LLM. Esto hace el coste LLM casi nulo en
   régimen estacionario (la mayoría de días nada cambia).

---

## 4. Opciones evaluadas

| Opción | Descripción | Pros | Contras | Veredicto |
|---|---|---|---|---|
| **A. APIs oficiales** | Consumir API de cada firma | Fiable | **No existen** | Descartada |
| **B. Datos de agregador** | Partnership/licencia con PropFirmMatch o similar | Cero scraping, datos curados | Sin API pública; coste y dependencia; cobertura no garantizada para nuestros campos (p.ej. `ConsistencyMaxDayFraction`) | Explorar en paralelo (un email es barato), no bloquear |
| **C. Pipeline propio: monitor + extracción LLM + revisión humana** | Fuentes oficiales de cada firma, detección de cambios, extracción schema-driven, cola de aprobación | Control total; usa el schema y versionado ya existentes; coste bajo; sin dependencia de terceros | Hay que construirlo; anti-bot en algunos sitios; requiere revisión humana | **Recomendada** |
| **D. Solo alertas** | Monitor de cambios que avisa, actualización sigue manual | Trivial de construir | No elimina el trabajo manual, solo el riesgo de no enterarse | Válida como **fase 1 / MVP** de C |

---

## 5. Diseño propuesto (Opción C, adaptado a este proyecto)

### 5.1 Arquitectura

```
[RuleSource]  --fetch diario-->  [Snapshot + hash]
     |                                 |  hash cambió
     |                                 v
     |                    [Extracción LLM → JSON schema]
     |                                 |
     |                                 v
     |                  [Diff contra EvaluationProgram activo]
     |                                 |  hay diferencias
     |                                 v
     |                  [ProposedProgramChange (staging) + notificación]
     |                                 |  aprobación en UI admin
     |                                 v
     |          [Nueva versión: IsActive=false la vieja, nueva con EffectiveFrom]
```

### 5.2 Piezas nuevas (mínimas)

1. **`RuleSource`** (entidad): `PropFirmId`, `Url`, `Kind` (Pricing/FAQ/HelpCenter),
   `LastContentHash`, `LastCheckedAt`. Varias por firma.
2. **Job programado** en `TrackRecord.Infrastructure` (`BackgroundService` con timer diario basta;
   no hace falta Hangfire/Quartz para esta frecuencia): fetch → normalizar → hash → comparar.
3. **Extractor**: llamada a Claude API con structured output cuyo schema replica los campos de
   `EvaluationProgram` (incluidos `Funded*` y `Payout*`) más, por campo, `sourceQuote` (cita
   literal) y `confidence`. Entrada: el Markdown de la página. Un programa por tamaño de cuenta.
4. **`ProposedProgramChange`** (staging): JSON extraído + diff campo a campo contra el programa
   activo + citas. Nunca escribe directamente en el catálogo.
5. **UI de revisión** (admin): lista de propuestas pendientes, diff resaltado, botones
   aprobar/rechazar/editar. Aprobar ejecuta el versionado existente (`EffectiveFrom` = fecha del
   cambio detectado).

### 5.3 Validaciones automáticas antes de proponer

- Rangos plausibles: `ProfitTarget` y `MaxDrawdown` proporcionales a `AccountSize` (p.ej. 2-15 %),
  fracciones en (0,1], `EvaluationCost` > 0, `DrawdownType` dentro del enum.
- Campo sin cita literal en la fuente → marcar en amarillo, no autocompletar.
- Si la extracción difiere del programa activo en > N campos a la vez, exigir revisión manual
  aunque la confianza sea alta (probable rediseño de página, no cambio real de reglas).

### 5.4 Fuentes primarias por firma (a compilar en la implementación)

Para cada firma del catálogo, registrar 1-3 URLs oficiales: la página de pricing/planes (tiene
coste, tamaño, target, drawdown) y el help center/FAQ (tiene consistencia, payouts, días mínimos).
Complementar con la página de [rule changes de PropFirmMatch](https://propfirmmatch.com/prop-firm-rules)
como señal de alerta secundaria y verificación cruzada.

> Nota: verificar los ToS de cada sitio antes de monitorizarlo. Fetch 1×/día de páginas públicas
> de marketing es de bajo riesgo, pero algunos help centers (Cloudflare) pueden requerir un
> servicio de fetching gestionado (Firecrawl o similar) en vez de `HttpClient` directo.

### 5.5 Costes estimados

- Fetch diario de ~30-50 URLs: despreciable.
- Extracción LLM: solo cuando cambia el hash (pocas veces/mes por firma). Con páginas podadas a
  Markdown (~5-15K tokens) y salida estructurada, céntimos por extracción → **< 5 €/mes** con
  Claude Sonnet; usar un modelo pequeño para el triaje "¿el cambio afecta a reglas o es cosmético?"
  lo baja aún más.

---

## 6. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Alucinación del LLM en un número | Citas literales obligatorias por campo + validación de rangos + revisión humana antes de publicar |
| Cambio cosmético de la página dispara extracciones | Normalización agresiva antes del hash; triaje barato "¿cambió alguna regla?" antes de la extracción completa |
| Anti-bot / Cloudflare en algunas fuentes | Servicio de fetching gestionado solo para esas URLs |
| Cambios anunciados fuera de la web (email/Discord) antes de actualizar la página | Aceptable: la web oficial es la fuente de verdad contractual; PropFirmMatch como alerta secundaria |
| ToS de scraping | Solo fuentes oficiales públicas, frecuencia mínima, User-Agent identificado; revisar ToS por firma |
| Dependencia de la calidad de la fuente (reglas repartidas en varias páginas) | `RuleSource.Kind` permite combinar varias páginas por firma en una sola extracción |

---

## 7. Plan por fases

1. **Fase 1 — MVP "no volver a estar desactualizado" (esfuerzo bajo):** entidad `RuleSource` +
   job diario de hash + notificación "la página X de la firma Y cambió". El cambio se aplica
   manualmente como hasta ahora, pero ya no hay que vigilarlo.
2. **Fase 2 — Extracción asistida:** al detectar cambio, extracción LLM + diff + propuesta en UI
   admin. La actualización pasa de "leer, interpretar y teclear" a "revisar un diff y aprobar".
3. **Fase 3 — Cobertura y confianza:** más firmas, verificación cruzada con agregador,
   auto-aprobación opcional para cambios de 1 campo con confianza alta y cita verificada.
4. **En paralelo (gratis):** email a PropFirmMatch/TradesViz preguntando por acceso a datos; si
   existe a precio razonable, puede sustituir o reforzar la fase 2.

---

## 8. Fuentes

- [PropFirmMatch — Futures prop firms](https://propfirmmatch.com/futures) y [Prop firm rules](https://propfirmmatch.com/prop-firm-rules)
- [TradesViz — Prop firm compliance tracking](https://www.tradesviz.com/blog/prop-firm-compliance-tracking/) y [Prop firm journal](https://www.tradesviz.com/prop-firm-journal/)
- [Apex Trader Funding 4.0 — cambios 2026 (Phidias)](https://phidiaspropfirm.com/education/apex-trader-funding-4-0-explained), [DamnPropFirms — Apex drawdown rules](https://damnpropfirms.com/trading-guides/apex-trader-funding-drawdown-rules/), [SpicyFutures — Apex 4.0 review](https://spicyfutures.com/apex-trader-funding-4-0-review-2026/)
- [ByteTunnels — Schema-driven LLM extraction](https://bytetunnels.com/posts/llm-powered-data-extraction-schema-driven-scraping-with-structured-output/)
- [Firecrawl — Web extraction tools](https://www.firecrawl.dev/blog/best-web-extraction-tools), [ScrapeGraphAI — Scraping APIs para LLM](https://scrapegraphai.com/blog/3-best-web-scraping-api)
- [BrightData — LLM scrapers 2026](https://brightdata.com/blog/ai/best-llm-scrapers)
