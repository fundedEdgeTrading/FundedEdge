# FundedEdge — Guía de rebranding, SEO y marketing de captación

> Decisión de marca definitiva, plan de posicionamiento SEO paso a paso y canales de
> captación priorizando lo gratuito. Complementa `GUIA_MONETIZACION_Y_MARKETING.md`
> (cuyo §4 dejaba el nombre pendiente: aquí se decide) y asume el producto actual:
> dashboard de negocio de fondeo, módulo de riesgo, Firm Fit, calculadora pública
> (`/calculadora`) y track records públicos (`/t/{slug}`), en ES/EN.

---

## 1. Rebranding

### 1.1 Decisión: el producto pasa a llamarse **FundedEdge**

"TrackRecord" era un nombre de trabajo: genérico, imposible de posicionar (la SERP la
dominan el término contable y decenas de homónimos) y no registrable. De los candidatos
del §4.2 de la guía de monetización y nuevas opciones, la decisión es **FundedEdge**:

| Criterio | Por qué FundedEdge gana |
|---|---|
| Dice qué es y para quién | "Funded" = el nicho exacto (funded trading); "Edge" = la promesa del producto: saber si tu negocio de evaluaciones tiene ventaja estadística o no |
| Es la propuesta de valor | La pregunta que responde la app (¿tengo edge?) está EN el nombre — el naming vende solo |
| Bilingüe de serie | "Funded" y "edge" son jerga adoptada tal cual por los traders hispanohablantes del nicho; no hay que traducir la marca |
| SEO semántico | Contiene "funded", presente en las búsquedas comerciales del nicho ("funded trading tracker", "funded account journal") sin ser genérico |
| Diferenciación | Los competidores usan nombres de journal (Tradezella, TraderSync, Edgewonk) o de firma (Topstep, Apex); ninguno posee "funded + edge" |
| Riesgo de marca bajo | Menos saturado que los sufijos -pilot/-flow/-ly; verificar igualmente (ver checklist) |

- **Tagline (ES):** «El copiloto financiero del trader de fondeo» (se mantiene — ya es bueno).
- **Tagline (EN):** "Know your edge. Run funding like a business."
- **Respaldo si falla la verificación legal/dominio:** 1º *Evalio* (sonoro, dominio probable,
  tono producto), 2º *PropLedger* (posicionamiento contable serio).

**Verificación previa (hacer ANTES de tocar código, en este orden, ~1 hora):**
1. Dominio: `fundededge.com` y alternativas aceptables `fundededge.io` / `.app` /
   `getfundededge.com` (Namecheap/Porkbun para comprobar y registrar; 10-30 €/año).
2. Marca: búsqueda en TMview (EUIPO) y USPTO de "FundedEdge" en clases 9/36/42.
3. Redes: handle `@fundededge` libre en X, YouTube, TikTok, Instagram (namechk.com).
4. SERP: googlear "FundedEdge" — no debe existir un competidor activo con ese nombre.

**Migración técnica (el código ya está preparado):**
- [ ] `Brand.Name = "FundedEdge"` en `src/TrackRecord.Domain/Common/Brand.cs` — cambio de
  una línea; todo el producto (títulos, emails, PDF, landing) lo hereda.
- [ ] Dominio nuevo con **redirects 301** desde el dominio antiguo si ya hubiera tráfico,
  y `App:BaseUrl` actualizado (Stripe, emails).
- [ ] Google Search Console: alta de la propiedad nueva + "Cambio de dirección".
- [ ] Favicon/logo y OG images regenerados (ver 1.2).

### 1.2 Identidad visual (evolución, no revolución)

La base actual es correcta (dark fintech, tokens centralizados en `:root` de
`wwwroot/app.css`) — se conserva y se le da firma propia. Regla de oro en una app de
trading: **el color de marca nunca compite con la semántica P&L** (verde=ganancia,
rojo=pérdida quedan reservados).

**Paleta FundedEdge** (sustituir solo los tokens `--accent*`; el resto ya está bien):

| Token | Valor | Uso |
|---|---|---|
| `--bg` | `#0b0e14` (actual) | Fondo — mantener: transmite "terminal profesional" |
| `--accent` | `#2fd6a3` — *edge mint* | Color de marca: CTAs, enlaces, foco. Diferencia frente al naranja Topstep, morado Tradezella y azul genérico actual |
| `--accent-strong` | `#5ce6ba` | Hover/estados activos |
| `--accent-soft` | `rgba(47, 214, 163, 0.16)` | Fondos suaves de chips/badges |
| `--good` / `--danger` | actuales (`#3fb950` / `#f85149`) | SOLO P&L y estados. El mint de marca es más azulado que `--good` a propósito para no confundirse |
| `--warning` | actual (`#d9a521`) | Riesgo/avisos — es el "segundo color" de la marca en material de marketing (edge vs riesgo) |

**Tipografías** (self-hosted en `wwwroot/fonts` — la CSP actual solo permite `'self'`,
nada de Google Fonts por CDN; los tres son open source, licencia OFL):
- **UI:** Inter (400/500/600) — legibilidad en denso.
- **Números y tablas:** JetBrains Mono con `font-variant-numeric: tabular-nums` — en un
  producto cuyo héroe son los números, que las cifras alineen es identidad de marca.
- **Titulares/landing:** Space Grotesk (600/700) — carácter técnico sin ser frío.

**Logo:** evolucionar el símbolo actual (flecha de tendencia) hacia una **cuña
ascendente** ("edge" literal) en mint sobre dark; monograma "FE" para favicon. Encaja
con el `brand-mark` SVG existente sin rehacer el layout.

**Tono de voz** (ya implícito en la landing, ahora es doctrina): estadística honesta,
sin humo. Nunca prometer rentabilidad; a veces el producto dice "tu negocio NO es
rentable" — eso es un argumento de venta, no un problema. Prohibido: lambos, capturas
de payouts ajenos, "hazte fondeado fácil". Bienvenido: números propios, intervalos de
confianza, "conoce tu edge".

---

## 2. Posicionamiento SEO

### 2.1 SEO técnico — checklist específica de esta app (estado actual: casi todo pendiente)

Hoy las páginas públicas no tienen meta description, ni Open Graph, ni sitemap, ni
robots.txt. Orden de ejecución:

- [ ] **Meta description + Open Graph + Twitter Card por página pública** (landing,
  `/precios`, `/calculadora`, `/t/{slug}`): componente `SeoHead` reutilizable con
  `<HeadContent>` (título ≤ 60 car., description ≤ 155 con la keyword objetivo). Para
  `/t/{slug}`, OG image dinámica con los KPIs del trader (ver 3.1: es el loop viral).
- [ ] **`robots.txt`** en `wwwroot`: permitir públicos, `Disallow` de `/Account`,
  `/settings`, `/api`; línea `Sitemap: https://fundededge.com/sitemap.xml`.
- [ ] **`sitemap.xml`**: endpoint minimal API que liste páginas públicas + los `/t/{slug}`
  activos + (cuando existan) las páginas programáticas de 2.2.
- [ ] **Canonical** en cada página pública (evita duplicados con parámetros).
- [ ] **Idioma indexable**: hoy el idioma se resuelve por **cookie**, que Google no ve —
  para el buscador solo existe la versión española. Para posicionar en inglés, las
  páginas públicas necesitan **rutas propias por idioma** (`/en/pricing`,
  `/en/calculator`) con `hreflang` es/en cruzado. Es EL requisito para captar el mercado
  EN, que es 10× el ES en este nicho. (La app autenticada puede seguir con cookie.)
- [ ] **Datos estructurados** (JSON-LD): `SoftwareApplication` + `Offer` (planes) en la
  landing; `FAQPage` en `/precios` (las FAQ ya existen — es marcar lo que hay);
  `BreadcrumbList` en las páginas programáticas.
- [ ] **Core Web Vitals**: el prerender SSR de Blazor ya da primer render rápido; vigilar
  con PageSpeed Insights que la landing no cargue `blazor.web.js` bloqueante y que las
  imágenes lleven `loading="lazy"` y dimensiones.

### 2.2 Estrategia de contenido: la mina es el SEO programático

La ventaja injusta: **el catálogo de `EvaluationPrograms` (Firm Fit) y la calculadora ya
son datos y motores reales**. Generar páginas públicas desde ellos ataca long-tails
comerciales que los journals genéricos no pueden copiar:

1. **Página por firma** — `/firms/apex-trader-funding`: reglas del programa (drawdown,
   consistencia, días mínimos) explicadas + coste esperado por cuenta fondeada según pass
   rate + CTA "simúlalo con TU operativa". Keywords: *"apex trader funding rules"*,
   *"apex 50k evaluation cost"*.
2. **Comparativas** — `/compare/apex-vs-tradeify`: tabla de reglas lado a lado generada
   del catálogo + qué operativa encaja mejor con cada una. Keywords: *"apex vs tradeify"*,
   *"best futures prop firm 2026"* (las búsquedas "X vs Y" convierten altísimo).
3. **Cluster de calculadoras** — variantes de landing sobre el motor ya existente:
   `/calculadora-ev-fondeo`, `/en/prop-firm-ev-calculator`, `/en/risk-of-ruin-calculator`.
   Las calculadoras gratuitas son los imanes de enlaces nº1 en nichos financieros.
4. **Glosario/blog** (1 pieza/semana basta): "¿Qué es el trailing drawdown?", "Regla de
   consistencia explicada con números", "Cuánto cuesta de verdad una cuenta fondeada".
   Reutilizar como hilos de X y guiones de vídeo (3.3).

Keywords semilla para el Keyword Planner: `prop firm tracker`, `funded trading journal`,
`prop firm ev calculator`, `apex trader funding calculator`, `trailing drawdown
calculator`, `prop firm comparison`, `diario de trading fondeo`, `calculadora prop firm`.

### 2.3 ¿Hay herramienta que lo automatice? — respuesta honesta

**No existe una herramienta que "haga el SEO" de punta a punta** (las que lo prometen
generan spam que Google penaliza). Lo que SÍ se automatiza, con qué:

| Qué se automatiza | Herramienta | Coste |
|---|---|---|
| Indexación (avisar a buscadores de páginas nuevas) | **IndexNow** (ping desde el endpoint del sitemap) + Search Console API | Gratis |
| Diagnóstico y avisos (errores, keywords que ya rankean) | **Google Search Console** + **Bing Webmaster Tools** — obligatorias desde el día 1 | Gratis |
| Auditoría técnica (enlaces rotos, metas duplicadas) | **Ahrefs Webmaster Tools** (gratis para tu propio sitio) + **Screaming Frog** (500 URLs) | Gratis |
| Analítica de tráfico | **GA4** o, más ligero y sin banner de cookies, **Umami/Plausible self-hosted** | Gratis |
| Investigación de keywords | **Google Keyword Planner** + **Google Trends** + autocompletar/"People also ask" | Gratis |
| Generación de páginas | Tu propio SEO programático (2.2) — plantillas Razor sobre el catálogo: se escribe una vez, escala solo | Gratis |
| Borradores de contenido | Claude/IA para el primer borrador del blog — **siempre revisado y con tus datos**; el valor diferencial son tus números reales | ~Gratis |
| Todo-en-uno de pago (cuando haya ingresos) | Semrush o Ahrefs de pago (~120 €/mes) — útil desde ~1k visitas/mes, no antes | De pago |

### 2.4 Autoridad (backlinks) sin presupuesto

- Directorios de producto: **Product Hunt** (lanzamiento, ver 3.4), AlternativeTo
  (alternativa a Tradezella/TraderSync), SaaSHub, BetaList, Indie Hackers.
- Las **calculadoras** enlazables: ofrecerlas a los agregadores/reviews de prop firms
  (PropFirmMatch y similares enlazan recursos útiles).
- Cada **track record público** compartido por un usuario es un backlink + social proof.
- HARO/Featured (peticiones de prensa): responder consultas sobre prop trading.

---

## 3. Marketing de captación — canales gratuitos por orden de ROI

### 3.1 El producto como canal (product-led growth) — prioridad nº1

1. **Track record público `/t/{slug}` como loop viral**: el trader lo comparte para
   presumir/rendir cuentas → sus seguidores ven el producto. Potenciarlo: **OG image
   dinámica** con sus KPIs (al pegar el enlace en X/Discord se ve una tarjeta con números,
   no un enlace soso) + pie "Hecho con FundedEdge — conoce tu edge" enlazado + botón
   "Comparte tu track record" tras cada payout registrado.
2. **La calculadora como lead magnet**: ya es pública y anónima; añadir CTA post-cálculo
   ("Esto con supuestos manuales — regístrate y calcúlalo con tus datos reales") y
   difundirla en cada conversación relevante (3.2). Es la respuesta perfecta a la
   pregunta más repetida del nicho: *"¿compensa comprar otra evaluación?"*.
3. **Firm Fit como gancho editorial**: publicar mensualmente un "Firm Fit Report"
   (agregado y anónimo cuando haya masa; mientras, con perfiles de operativa sintéticos):
   "para una operativa scalper media, la regla de consistencia de X cuesta 14 puntos de
   pass rate". Nadie más puede publicar eso → citas y enlaces.

### 3.2 Comunidades donde ya está el cliente (regla 90/10: 90 % aportar, 10 % mencionar)

- **Discords** de prop firms (Apex, Topstep, Tradeify…) y de traders de futuros ES/EN:
  responder dudas de reglas/EV con números; el enlace a la calculadora cuando aporte.
- **Reddit**: r/FuturesTrading, r/Daytrading, r/propfirms — los posts "mis números de 6
  meses de evaluaciones" (con capturas del dashboard) rinden; la autopromo directa no.
- **Foros**: Futures.io (nicho exacto, muy activo).
- **X (FinTwit ES y EN)**: build in public del producto + hilos con datos ("qué % de
  cuentas llegan al primer payout según los datos"). Constancia > viralidad.

### 3.3 Contenido corto (si hay cámara o capturas, 2-3 piezas/semana)

Formatos probados del nicho, siempre con pantalla del producto:
- "Mi negocio de fondeo en números este mes" (dashboard real).
- "Lo que la regla de consistencia de [firma] hace a tu pass rate" (Firm Fit).
- "¿Cuánto cuesta DE VERDAD llegar a fondeado en [firma]?" (calculadora).
YouTube Shorts + TikTok + Reels con el mismo clip; el largo (si sale) a YouTube. El vídeo
es el canal de descubrimiento nº1 del nicho de fondeo.

### 3.4 Lanzamientos y prensa de producto

- **Product Hunt**: un martes/miércoles, con la calculadora como demo sin registro.
- Indie Hackers / r/SideProject: la historia "construí el contable de mi negocio de
  evaluaciones" funciona ahí.
- Nota corta a newsletters de trading cuantitativo/prop.

### 3.5 Email (gratis hasta escala)

- Brevo o MailerLite (planes gratuitos): onboarding (3 correos: configura → importa
  trades → tu primer informe) + resumen semanal — **el informe semanal de IA ya existe**;
  es retención gratuita ya construida.
- Lead magnet: PDF "Los números reales del negocio de evaluaciones" a cambio de email en
  la landing de la calculadora.

### 3.6 De pago, solo cuando haya caja (referencia, no ahora)

Micro-influencers del nicho con código de descuento/afiliación (mejor CAC del sector);
Google Ads solo a keywords transaccionales exactas ("prop firm tracker"). Nada de display.

---

## 4. Plan de 90 días y KPIs

| Semanas | Foco | Entregables |
|---|---|---|
| 1-2 | Rebranding | Verificación legal/dominio → `Brand.cs` → dominio + 301 + GSC. Tokens de color y fuentes |
| 2-4 | SEO técnico | Metas/OG + robots + sitemap + canonical + JSON-LD. Alta GSC/Bing/Ahrefs WT. Analítica |
| 4-8 | Contenido programático | Rutas `/en/` públicas con hreflang; 6 páginas de firma + 3 comparativas + cluster calculadoras |
| 6-12 | Captación | Rutina 90/10 en 3 comunidades; 2 piezas cortas/semana; OG dinámicas de `/t/{slug}`; lanzamiento Product Hunt (semana 10-12) |

**KPIs (revisar quincenal):** impresiones y clics en GSC (esperar tracción real desde el
mes 2-3 — el SEO es lento, los otros canales cubren mientras); registros/semana y su
fuente (parámetros UTM en todo enlace que publiques); % de usuarios con track record
público activo (motor del loop viral); conversión visita→registro de la calculadora.

---

*Resumen ejecutivo: el nombre pasa a ser **FundedEdge** (la promesa del producto en la
marca, bilingüe y posicionable; respaldos Evalio/PropLedger tras verificación). El SEO no
se automatiza con una herramienta mágica: se automatiza la indexación y el diagnóstico
(GSC + IndexNow + Ahrefs WT, gratis) y se escala con SEO programático sobre los datos que
ya tiene la app. La captación gratuita más rentable ya está construida — track record
público, calculadora y Firm Fit — solo hay que convertirla en loops: OG images dinámicas,
CTAs de compartir y presencia constante donde el nicho ya conversa.*
