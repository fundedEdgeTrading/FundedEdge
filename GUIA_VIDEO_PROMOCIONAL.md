# Guía: Vídeo promocional de FundedEdge con IA

Cómo producir un vídeo promocional del producto (30–60 s para redes + 90 s para la landing) usando herramientas de IA, sin equipo de vídeo ni presupuesto de agencia.

---

## 1. Qué vendemos (mensaje central)

FundedEdge es el copiloto del trader fondeado: registra tus cuentas de fondeo (Apex, Tradeify, Lucid…), su ciclo de vida, costes y payouts, y convierte tus trades en KPIs, informes de IA y seguimiento psicológico.

**Mensaje en una frase (úsala como cierre del vídeo):**

> "Deja de adivinar si tu fondeo es rentable. Míralo."

**Dolores del público objetivo (elige 1–2 por vídeo, no todos):**

- No sabe cuánto le cuestan realmente los resets y las evaluaciones frente a lo que cobra en payouts.
- Lleva sus cuentas en un Excel caótico (o en ninguna parte).
- No detecta patrones: qué setups funcionan, en qué estado emocional pierde dinero.
- No sabe qué firma de fondeo le conviene según su estilo (FirmFit).

---

## 2. Stack de herramientas IA recomendado

| Fase | Herramienta | Alternativa | Coste aprox. |
|---|---|---|---|
| Guion y storyboard | Claude / ChatGPT | — | Gratis / suscripción |
| Grabación de pantalla del producto | OBS Studio | Screen Studio (Mac, zooms automáticos) | Gratis / ~89 $ |
| B-roll generativo (escenas cinematográficas) | Runway Gen-4 | Google Veo 3, Sora, Kling, Pika | ~12–30 $/mes |
| Voz en off IA (español natural) | ElevenLabs | Voz de Veo 3 integrada | Gratis (limitado) / 5 $/mes |
| Avatar presentador (opcional) | HeyGen | Synthesia | ~24–30 $/mes |
| Música | Suno | Biblioteca de CapCut / YouTube Audio Library | Gratis / 10 $/mes |
| Edición y subtítulos automáticos | CapCut | DaVinci Resolve, Descript | Gratis |

**Stack mínimo recomendado (lo que yo usaría):** guion con Claude → grabación real de la app con OBS → voz con ElevenLabs → 2–3 clips de b-roll con Runway o Veo 3 → montaje y subtítulos en CapCut. Coste total: < 30 $.

**Regla de oro:** el producto real debe ocupar el 60–70 % del vídeo. El b-roll de IA es condimento (apertura y transiciones), nunca el plato principal — el software es el protagonista y la grabación de pantalla genera más confianza que cualquier clip generado.

---

## 3. Estructura del guion (fórmula probada)

Duración objetivo: **45 segundos**, formato Hook → Problema → Solución → Demo → CTA.

| Segundos | Sección | Contenido |
|---|---|---|
| 0–3 | Hook | Pregunta o dato que duele: "¿Sabes cuánto te han costado tus resets este año?" |
| 3–10 | Problema | Excel caótico, cuentas quemadas, payouts sin controlar (b-roll IA + texto en pantalla) |
| 10–30 | Solución + demo | Grabación real de la app: dashboard de KPIs → cuentas y su progreso → costes vs payouts → informe de IA |
| 30–40 | Diferenciador | Psicología y setups: "FundedEdge te dice en qué estado mental pierdes dinero" |
| 40–45 | CTA | Logo + "Empieza gratis" + URL. Cierre: "Deja de adivinar. Míralo." |

### Guion de ejemplo (voz en off, listo para ElevenLabs)

> ¿Sabes cuánto dinero te han costado tus resets este año?
> La mayoría de traders fondeados no lo sabe. Cuentas en Excel, capturas sueltas, payouts sin controlar.
> FundedEdge lo pone todo en un solo sitio: tus cuentas de Apex, Tradeify o Lucid, su progreso, sus costes y tus payouts reales.
> Un dashboard con tus KPIs, informes generados por IA, y hasta el seguimiento de tu psicología: descubre en qué estado mental pierdes dinero.
> FundedEdge. Deja de adivinar si tu fondeo es rentable. Míralo. Empieza gratis.

---

## 4. Producción paso a paso

### Paso 1 — Guion definitivo
Adapta el guion de ejemplo al tono de tu marca. Pide a la IA 3 variantes de hook y elige la más agresiva. Léelo en voz alta: si supera 50 s hablado, recorta.

### Paso 2 — Prepara la app para grabar
- Crea una cuenta demo con **datos realistas y bonitos**: 4–6 cuentas de fondeo en distintos estados (en evaluación, fondeada, con payouts), trades suficientes para que el dashboard luzca, un informe de IA generado.
- Ventana del navegador a 1920×1080, zoom 100 %, modo que mejor luzca la UI, sin extensiones ni marcadores visibles.
- Nunca grabes con datos reales de usuarios.

### Paso 3 — Graba las pantallas (OBS)
Graba clips separados de 5–10 s por pantalla, con movimiento de ratón lento y deliberado:
1. Dashboard de KPIs (Home)
2. Lista de cuentas y detalle de progreso de una cuenta
3. Costes y payouts
4. Informe de IA
5. Psicología / encuesta emocional
6. FirmFit (comparador de firmas)

En edición aplicarás zooms y paneos sobre estos clips (CapCut lo hace fácil; Screen Studio lo hace automático).

### Paso 4 — Genera el b-roll con IA (2–3 clips máximo)
Prompts de ejemplo para Runway / Veo 3 / Sora:

- *"Cinematic close-up of a stressed trader at night, multiple monitors with red charts, dark room, shallow depth of field"* (problema)
- *"Messy spreadsheet on a laptop screen, papers and coffee cups on desk, chaotic home office, cinematic lighting"* (problema)
- *"Confident trader closing laptop and smiling, morning light, clean minimal desk, cinematic"* (resolución, antes del CTA)

Genera 3–4 variantes de cada uno y quédate con la mejor. Evita clips con texto generado (la IA escribe mal) y manos en primer plano.

### Paso 5 — Voz en off (ElevenLabs)
- Elige una voz en español (castellano o neutro según tu mercado), estilo "confiado, cercano".
- Genera frase a frase, no todo el bloque: te da mejores tomas y facilita el montaje.
- Ajusta *stability* media-baja para que suene menos robótica.

### Paso 6 — Música (Suno o biblioteca)
Prompt para Suno: *"minimal electronic corporate, driving beat, modern fintech, no vocals, builds to a confident finish, 60 seconds"*. Volumen en mezcla: la música 15–20 dB por debajo de la voz.

### Paso 7 — Montaje (CapCut)
1. Coloca la voz en off como columna vertebral y corta el vídeo a su ritmo.
2. Alterna: b-roll (hook/problema) → pantallas del producto (solución/demo) → b-roll corto → CTA.
3. Zooms suaves sobre la UI destacando el dato del que habla la voz (un KPI, un payout).
4. Subtítulos automáticos **siempre** (el 80 % de las visualizaciones en redes es sin sonido) — revisa la transcripción a mano.
5. Rotula 3–4 textos grandes de refuerzo: "Todas tus cuentas", "Costes vs payouts reales", "Informes de IA", "Empieza gratis".
6. Cierre: logo + URL + botón "Empieza gratis" en pantalla 2–3 s.

### Paso 8 — Exporta en tres formatos
| Destino | Formato | Duración |
|---|---|---|
| Landing / YouTube | 16:9, 1080p, MP4 H.264 | 60–90 s |
| Reels / TikTok / Shorts | 9:16, 1080×1920 | 30–45 s |
| X / LinkedIn | 1:1 o 16:9 | 30–45 s |

Para 9:16 no reescales el 16:9: reencuadra las capturas de pantalla con zooms más agresivos sobre la zona relevante de la UI.

---

## 5. Checklist antes de publicar

- [ ] El producto real aparece antes del segundo 10.
- [ ] Se entiende sin sonido (subtítulos + textos de refuerzo).
- [ ] Ningún dato personal o real de usuarios en pantalla.
- [ ] Hook de menos de 3 segundos.
- [ ] CTA claro con URL visible al final.
- [ ] Nombres de firmas (Apex, Tradeify, Lucid) usados solo como compatibilidad, sin implicar afiliación.
- [ ] Sin promesas de rentabilidad ni resultados garantizados (riesgo legal en productos de trading).
- [ ] Probado en un móvil real antes de publicar.

---

## 6. Variantes para iterar (una vez tengas el primero)

- **Vídeo por feature** (15–20 s): uno solo sobre FirmFit, otro sobre psicología, otro sobre costes vs payouts. Reutiliza las mismas grabaciones.
- **Formato "POV/problema"**: empieza con el Excel caótico del trader y termina en el dashboard.
- **Avatar IA (HeyGen)** para versión "fundador explica el producto" si no quieres salir a cámara.
- Publica 2 hooks distintos del mismo vídeo y mide retención a 3 s para decidir cuál escalar.
