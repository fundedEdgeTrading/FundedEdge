# FundedEdge — Guía de Monetización, Marketing e Implementación por Fases

> **Qué es este documento.** Análisis de mercado del nicho (trackers/journals para traders
> de cuentas de fondeo), estrategia de monetización y rebranding, y un **roadmap de
> implementación por fases escrito para ser ejecutado por un modelo de IA** (o un
> desarrollador) **sin necesidad de más contexto que este archivo y el repositorio**.
>
> **Cómo usarlo (instrucciones para el modelo ejecutor):**
> 1. Ejecuta **una fase por PR**, en orden (F1 → F2 → …). No mezcles fases.
> 2. Antes de tocar código, lee la sección **§7 Convenciones obligatorias del repo** entera.
> 3. Cada fase tiene *Objetivo*, *Archivos*, *Pasos* y *Criterios de aceptación (DoD)*.
>    No des una fase por terminada sin cumplir su DoD completo.
> 4. Los precios, límites y textos de marketing de este documento son la **fuente de
>    verdad**; si el código y este documento discrepan, gana este documento.
> 5. Si una decisión no está cubierta aquí ni en `GUIA_IMPLEMENTACION.md`, elige la opción
>    más simple que no cierre puertas y déjala anotada en la descripción del PR.

---

## Índice

1. [Análisis de mercado](#1-análisis-de-mercado)
2. [Posicionamiento y diferenciación](#2-posicionamiento-y-diferenciación)
3. [Modelo de suscripciones (pricing)](#3-modelo-de-suscripciones-pricing)
4. [Rebranding](#4-rebranding)
5. [Estrategia de marketing y adquisición](#5-estrategia-de-marketing-y-adquisición)
6. [Roadmap de implementación por fases](#6-roadmap-de-implementación-por-fases)
   - [F1 — Infraestructura de planes y feature-gating](#f1--infraestructura-de-planes-y-feature-gating)
   - [F2 — Límites de IA por plan y medición de uso](#f2--límites-de-ia-por-plan-y-medición-de-uso)
   - [F3 — Landing pública y página de precios](#f3--landing-pública-y-página-de-precios)
   - [F4 — Pagos con Stripe](#f4--pagos-con-stripe)
   - [F5 — Features de crecimiento](#f5--features-de-crecimiento)
7. [Convenciones obligatorias del repo](#7-convenciones-obligatorias-del-repo)
8. [Métricas de éxito (KPIs SaaS)](#8-métricas-de-éxito-kpis-saas)

---

## 1. Análisis de mercado

### 1.1 El nicho

El trading con **cuentas de fondeo (prop firms)** — Apex, Tradeify, Lucid, Topstep,
FundedNext, MyFundedFutures… — ha explotado desde 2020. El trader de fondeo tiene un
problema que el trader retail clásico no tiene: **gestiona un negocio**, no solo una
curva de equity. Compra evaluaciones (coste), algunas pasan (pass rate), las fondeadas
generan payouts (ingreso) y queman drawdowns (riesgo). Nadie le responde la pregunta
central: *"¿mi operativa + mi funnel de evaluaciones es un negocio rentable, y cuánto
bankroll necesito para no quebrar por varianza?"*

### 1.2 Competidores y huecos

| Producto | Precio (aprox.) | Fuerte en | Débil en (nuestro hueco) |
|---|---|---|---|
| TradeZella | ~29–49 $/mes | Journaling, UX, comunidad | Cero concepto de "negocio de fondeo": ni costes de evaluación, ni payouts, ni EV por evaluación |
| TraderSync | ~30–80 $/mes | Importadores de brokers | Igual: métricas por trade, no por *cuenta de fondeo* |
| Tradervue | 0–~50 $/mes | Veteranía, compartir trades | UI anticuada, nada de prop firms |
| Edgewonk | ~169 $ (pago único) | Análisis de disciplina | Sin sync automático, sin multi-cuenta de fondeo |
| TradesViz | ~15–30 $/mes | Gráficos masivos, barato | Curva de aprendizaje, nada de fondeo |
| Dashboards de las propias firms | Incluido | Datos oficiales de esa firma | Solo *su* firma; el trader con 5 cuentas en 3 firms no tiene visión agregada |
| Hojas de cálculo (el statu quo real) | 0 $ | Flexibilidad | Manuales, frágiles, sin IA, sin Monte Carlo |

**Conclusión:** todos los journals compiten en "análisis de trades". Ninguno compite en
**"contabilidad y viabilidad del negocio de fondeo multi-firma"**, que es exactamente lo
que ya tenemos construido (KPIs de negocio, funnel de evaluaciones, Monte Carlo de
bankroll, EV por evaluación con IC 95 %, analista IA). No intentemos ganar a TradeZella
en journaling: **ganamos siendo el CFO del trader de fondeo.**

### 1.3 Cliente objetivo (ICP)

- **Primario:** trader de futuros con 2+ cuentas de fondeo activas en 1–3 firms
  (Apex/Tradeify/Lucid/Topstep), opera NinjaTrader/Tradovate, gasta 100–500 $/mes en
  evaluaciones y resets. Dolor: no sabe si su funnel es EV+ y lo gestiona en Excel.
- **Secundario:** aspirante en evaluación que quiere maximizar su probabilidad de pasar
  (módulo de riesgo intra-cuenta) antes de escalar.
- **Geografía inicial:** hispanohablante (la UI ya está en español y casi nadie ataca ese
  mercado en este nicho) + inglés en una segunda oleada (F5).

---

## 2. Posicionamiento y diferenciación

### 2.1 Propuesta de valor (una frase)

> **"El copiloto financiero del trader de fondeo: sabrás si tu negocio de evaluaciones es
> rentable, cuánto bankroll necesitas y qué arreglar primero — con tus datos, no con
> promesas."**

### 2.2 Los 4 diferenciadores a explotar (ya existen en el producto)

1. **KPIs de negocio de fondeo** (pass rate, coste por cuenta fondeada, ROI, cashflow
   neto mensual) — nadie más los tiene como ciudadano de primera clase.
2. **Módulo de riesgo cuantitativo** (`/risk`): Monte Carlo de ruina del bankroll,
   bankroll mínimo recomendado, EV por evaluación con intervalo de confianza, Kelly.
   Es material de nivel institucional aplicado al fondeo retail.
3. **Analista IA** (`/ai`): informes que citan las métricas exactas del usuario y su
   viabilidad — no un chatbot genérico. Con límites por plan se convierte en el motor
   principal de upgrade.
4. **Sync automático** (Tradovate + AddOn NinjaTrader 8): elimina la fricción nº 1
   (picar trades a mano) que mata la retención en todos los journals.

### 2.3 Qué NO somos (anti-posicionamiento para el copy)

- No somos una prop firm ni vendemos evaluaciones (independencia = confianza).
- No somos señales ni "hazte rico": somos contabilidad y estadística honesta. El copy
  debe poder decir "quizá tu negocio NO es viable — mejor saberlo hoy".

---

## 3. Modelo de suscripciones (pricing)

Tres planes. El gratuito existe para adquisición y para alimentar el funnel de upgrade;
el medio es donde debe caer la mayoría; el superior monetiza la IA intensiva y el multi-
cuenta serio. **Estos nombres, límites y precios son la fuente de verdad para el código**
(enum `PlanTier` y récord `PlanLimits`, ver F1).

| | **Starter** (gratis) | **Pro** — 14,99 €/mes o 149 €/año | **Elite** — 29,99 €/mes o 299 €/año |
|---|---|---|---|
| Cuentas de fondeo activas | 2 | 10 | Ilimitadas |
| Firms | Ilimitadas | Ilimitadas | Ilimitadas |
| Trades manuales + importación CSV | ✔ | ✔ | ✔ |
| Sync automático Tradovate / NT8 | ✖ | ✔ | ✔ |
| KPIs de negocio y trading (dashboard) | ✔ | ✔ | ✔ |
| Gráficas (cashflow, equity) | ✔ | ✔ | ✔ |
| Módulo de riesgo `/risk` (Monte Carlo, EV, Kelly) | Solo semáforo EV | ✔ completo | ✔ completo |
| **Informes IA completos** | 1 al mes | 1 por semana | 1 al día |
| **Preguntas IA ad-hoc** | 3 al mes | 30 al mes | Ilimitadas* |
| Modelo IA | Haiku (esfuerzo bajo) | Haiku (esfuerzo medio) | Opus (esfuerzo alto) |
| Informe semanal IA automático | ✖ | ✔ | ✔ |
| Divisa EUR/USD | ✔ | ✔ | ✔ |
| Export PDF del track record (F5) | ✖ | ✔ | ✔ |
| Página pública de track record (F5) | ✖ | ✖ | ✔ |
| Alertas de drawdown/payout (F5) | ✖ | ✖ | ✔ |
| Perfiles Elite: ranking por ROI + informe de inspiración IA (F5.6) | ✖ | ✖ | ✔ |

\* "Ilimitadas" con un tope técnico anti-abuso de 50/día (constante en `PlanLimits`, no
se comunica en la web salvo en la letra pequeña de términos).

**Reglas de degradación** (importante para F1): si un usuario baja de plan y excede el
nuevo límite de cuentas, **no se borra nada** — las cuentas más recientes por encima del
límite pasan a *solo lectura* (se ven, no se pueden editar ni añadirles trades) hasta que
archive/eliminel el exceso o vuelva a subir de plan.

**Prueba gratuita:** 14 días de Pro al registrarse (sin tarjeta). Al expirar, cae a
Starter automáticamente.

---

## 4. Rebranding

### 4.1 Diagnóstico del nombre actual

"FundedEdge" describe bien el producto pero es **genérico** (imposible de posicionar en
SEO, difícil de registrar como marca, decenas de productos homónimos). Sirve para el MVP;
para vender, conviene una marca propietaria.

### 4.2 Candidatos de nombre (elegir uno; no implementar hasta que el dueño decida)

| Nombre | Racional | Riesgo |
|---|---|---|
| **Fundr** | Corto, evoca "funded" | Parecido a marcas fintech existentes |
| **PropLedger** | "El libro contable del prop trader" — exacto al posicionamiento | Menos sexy |
| **Evalio** | Evoca "evaluación", sonoro, dominio probable | Abstracto |
| **FundedFlow** | Funnel + cashflow, describe el producto | Largo |
| **Quantara** | Cuantitativo + premium | No dice qué hace |

**Recomendación del análisis:** *PropLedger* para posicionamiento serio/contable o
*Evalio* para un tono más producto-consumo. Mientras no se decida, **todo el código debe
dejar de hardcodear "FundedEdge"** (ver F3, tarea de centralización de marca) para que
el rebranding final sea un cambio de una sola línea.

### 4.3 Identidad visual

Ya existe una base sólida (tema oscuro fintech, gradiente azul→violeta, chips de
instrumentos en el login). Reglas para mantener coherencia:

- **Tokens**: todos los colores viven en `:root` de `src/FundedEdge.Web/wwwroot/app.css`.
  Prohibido introducir colores hex sueltos en componentes; añade un token si hace falta.
- **Tono del copy**: honesto, cuantitativo, sin promesas de rentabilidad. Segunda persona
  ("tu funnel", "tus payouts"). Español neutro.
- **El dato es el héroe**: capturas reales del dashboard/risk en la landing, no
  ilustraciones abstractas.

---

## 5. Estrategia de marketing y adquisición

*(Contexto para entender las features de F3/F5; no requiere código salvo lo indicado.)*

1. **SEO de nicho** (coste cero, intención altísima): la landing (F3) debe tener páginas/
   secciones indexables tipo "calculadora de EV de evaluación", "¿cuánto bankroll necesito
   para Apex?", "tracker de payouts multi-firma". La **calculadora pública de riesgo**
   (F5.3) es el imán de leads: versión limitada del Monte Carlo sin registro.
2. **Comunidades**: Discords de prop firms hispanas, r/Daytrading, X/Twitter de fondeo.
   El gancho no es "otro journal" sino *"¿sabes si tu negocio de evaluaciones es EV+?
   Esta captura sí lo sabe"*.
3. **Prueba social programática**: la página pública de track record (F5.2, plan Elite)
   hace que cada usuario avanzado sea un anuncio (footer "Powered by …" con UTM).
4. **Partnerships**: códigos de descuento cruzados con firms pequeñas/medianas (Lucid,
   Tradeify) y con creadores de contenido del nicho — ellos necesitan diferenciarse
   tanto como nosotros.
5. **Email post-registro** (fuera de alcance de código hasta F5): día 0 bienvenida, día 3
   "conecta tu Tradovate", día 10 "tu primer informe IA", día 13 aviso fin de trial.

---

## 6. Roadmap de implementación por fases

> **Regla general:** cada fase = un PR = compilar (`dotnet build`) sin warnings ni
> errores + toda la suite (`dotnet test`) en verde + los tests nuevos que pida la fase.

### F1 — Infraestructura de planes y feature-gating

**Objetivo:** que cada usuario tenga un plan (`Starter`/`Pro`/`Elite`), que exista un
único servicio que responda "¿puede este usuario hacer X?", y que los límites de cuentas
y de sync se apliquen. Sin pagos todavía (el plan se cambia solo desde código/BD o un
selector oculto de administración).

**Archivos a crear:**

| Archivo | Contenido |
|---|---|
| `src/FundedEdge.Domain/Enums/PlanTier.cs` | `public enum PlanTier { Starter = 0, Pro = 1, Elite = 2 }` (int explícito: se persiste) |
| `src/FundedEdge.Application/Abstractions/PlanLimits.cs` | Récord inmutable con: `MaxActiveAccounts (int?; null = ilimitado)`, `AutoSyncEnabled (bool)`, `FullRiskModule (bool)`, `AiReportsPerWindow (int)`, `AiReportWindowDays (int)`, `AiQuestionsPerMonth (int?)`, `AiDailyHardCap (int)`, `WeeklyAiReportEnabled (bool)`. Incluye `static PlanLimits For(PlanTier tier)` con los valores EXACTOS de la tabla de §3 |
| `src/FundedEdge.Application/Abstractions/IPlanService.cs` | `Task<PlanTier> GetTierAsync(CancellationToken ct = default)` (del usuario actual), `Task<PlanLimits> GetLimitsAsync(...)`, `Task<bool> CanCreateAccountAsync(...)`, `Task<bool> CanUseAutoSyncAsync(...)` |
| `src/FundedEdge.Infrastructure/Services/PlanService.cs` | Implementación: resuelve el usuario con `ICurrentUserAccessor` (patrón idéntico a `CurrencyPreferenceService`), lee `ApplicationUser.PlanTier` vía `IDbContextFactory<FundedEdgeDbContext>`, cuenta cuentas activas para `CanCreateAccountAsync` |
| `tests/FundedEdge.Application.Tests/PlanServiceTests.cs` | Ver DoD |

**Archivos a modificar:**

1. `src/FundedEdge.Infrastructure/Identity/ApplicationUser.cs`: añadir
   `public PlanTier PlanTier { get; set; } = PlanTier.Starter;` y
   `public DateTimeOffset? TrialEndsAt { get; set; }` (el trial de Pro se activa en el
   registro: en `Register.razor` y `ExternalLogin.razor`, al crear el usuario, poner
   `TrialEndsAt = DateTimeOffset.UtcNow.AddDays(14)`). Regla de resolución del tier
   efectivo (impleméntala en `PlanService`, único sitio):
   `if (TrialEndsAt > now && PlanTier == Starter) → tratar como Pro`.
2. `src/FundedEdge.Infrastructure/DependencyInjection.cs`: registrar
   `services.AddScoped<IPlanService, PlanService>();`.
3. `src/FundedEdge.Infrastructure/Services/TradingAccountService.cs` (`CreateAsync`):
   si `!await planService.CanCreateAccountAsync(ct)` lanzar
   `InvalidOperationException("Tu plan actual no permite más cuentas activas. Mejora a Pro o Elite en /plan.")`.
4. `src/FundedEdge.Infrastructure/Services/TradeSyncOrchestrator.cs`: en
   `SyncAllAccountsForUserAsync`, saltar (return 0 + log información) a los usuarios cuyo
   plan no tenga `AutoSyncEnabled`.
5. Página nueva `src/FundedEdge.Web/Components/Pages/Plan.razor` (`@page "/plan"`):
   muestra el plan actual, los límites de la tabla §3 y el uso actual (n.º de cuentas
   activas). Botones de upgrade **deshabilitados** con texto "Próximamente" (se activan
   en F4). Enlazarla en `NavMenu.razor`, sección "Sistema".
6. **Migración EF Core** (obligatoria, columnas nuevas en `AspNetUsers`):
   `dotnet ef migrations add AddUserPlanTier --project src/FundedEdge.Infrastructure --startup-project src/FundedEdge.Infrastructure`
   (requiere `~/.dotnet/tools` en el PATH).

**DoD F1:** ✅ Completada (ver PR correspondiente).
- [x] `dotnet build` 0 warnings / 0 errors; `dotnet test` todo verde (117/117: 45 Domain + 72 Application).
- [x] Tests nuevos (`PlanServiceTests.cs`, patrón `InMemoryDbContextFactory` +
      `FakeCurrentUserAccessor`): (a) usuario Starter con 2 cuentas activas no puede
      crear la 3ª; (b) Pro sí puede hasta 10; (c) Elite sin límite (crear 11);
      (d) trial vigente ⇒ límites de Pro; trial caducado ⇒ Starter; plan pagado no lo
      pisa un TrialEndsAt residual; usuario sin fila en BD ⇒ Starter por defecto;
      (e) `TradeSyncOrchestratorTests.SyncAllAccountsAsync_StarterPlan_DoesNotCallTradovate`.
- [x] La migración `AddUserPlanTier` está incluida y el snapshot actualizado.
- [x] `/plan` renderiza y aparece en la sidebar (sección "Sistema").

---

### F2 — Límites de IA por plan y medición de uso

**Objetivo:** convertir la IA en el motor de upgrade: contar informes/preguntas por
usuario y ventana temporal, bloquear con mensaje de upgrade al exceder, y elegir modelo/
esfuerzo según plan.

**Puntos técnicos clave (leer antes de tocar):**
- **No hace falta tabla nueva para contar informes**: `AiReports` ya persiste
  `UserId`, `Kind` (`Analysis` / `AdHocQuestion`) y `CreatedAt`. Contar con un `CountAsync`
  filtrado es suficiente y transaccionalmente honesto.
- El servicio a tocar es `src/FundedEdge.Infrastructure/Ai/ClaudeTradingAnalystService.cs`.
  Hoy tiene el modelo hardcodeado (`Model.ClaudeHaiku4_5`, `Effort.Low` — se puso barato
  a propósito para desarrollo). Esta fase lo hace **dependiente del plan**:
  Starter → Haiku/`Effort.Low`; Pro → Haiku/`Effort.Medium`; Elite → Opus
  (`Model.ClaudeOpus4_8`)/`Effort.High`. Añade estos 2 campos a `PlanLimits` (modelo y
  esfuerzo) en lugar de un `switch` dentro del servicio de IA.
- `WeeklyAiReportService.GenerateForAllUsersAsync` debe saltarse a los usuarios sin
  `WeeklyAiReportEnabled` en su plan.

**Pasos:**
1. Añadir a `IPlanService`: `Task<AiAllowance> GetAiAllowanceAsync(ct)` donde
   `AiAllowance(bool CanGenerateReport, bool CanAskQuestion, int ReportsUsed, int ReportsLimit, int QuestionsUsed, int? QuestionsLimit, DateTimeOffset WindowResetsAt)`.
   Implementación en `PlanService` contando en `AiReports` (informes: ventana deslizante
   de `AiReportWindowDays`; preguntas: mes natural UTC).
2. En `ClaudeTradingAnalystService.GenerateAnalysisReportAsync` / `AskQuestionAsync`:
   consultar el allowance ANTES de llamar a la API; si no puede, lanzar
   `InvalidOperationException` con mensaje en español que incluya cuándo se renueva el
   cupo y el plan que lo ampliaría. El tope anti-abuso `AiDailyHardCap` se comprueba
   siempre, también en Elite.
3. En `Ai.razor`: mostrar el contador de uso ("2 de 4 preguntas este mes · se renueva el
   …") y, si está agotado, un aviso con enlace a `/plan` en lugar del botón de generar.
4. `WeeklyAiReportService`: filtro por plan (paso descrito arriba).

**DoD F2:** ✅ Completada.
- [x] Tests en `PlanServiceTests.cs` (InMemory): (a) Starter con 1 informe este mes no
      puede generar otro (`ClaudeTradingAnalystService.EnsureAllowedAsync` lanza con
      "/plan" en el mensaje); (b) Pro puede 1/semana (informe hace 8 días ⇒ puede; hace 2
      días ⇒ no); (c) preguntas descuentan de su propio cupo, independiente del de
      informes; (d) Elite respeta `AiDailyHardCap` aunque las preguntas sean "ilimitadas";
      (e) `PlanLimits.For(tier)` selecciona el modelo/esfuerzo documentado por tier.
- [x] `Ai.razor` muestra uso restante (informes y preguntas) y enlaza a `/plan` cuando se agota.
- [x] Build + tests verdes (125/125: 45 Domain + 80 Application).

---

### F3 — Landing pública y página de precios

**Objetivo:** que un visitante sin cuenta entienda y desee el producto sin ver la app:
landing + página de precios públicas, y la marca centralizada para el futuro rebranding.

**Puntos técnicos clave:**
- Hoy `/` es el dashboard (privado). La landing vive en **`/bienvenida`** (`@page
  "/bienvenida"`) y `RedirectToLogin` **no cambia**; lo que cambia es que Login/Register
  enlazan a la landing y viceversa. (Alternativa de mover el dashboard: NO en esta fase.)
- Las páginas públicas siguen el patrón de las de `/Account`: estáticas SSR.
  **Estructura de carpetas (importante, ver aviso de trampa más abajo en §7): el layout va
  en `Public/PublicLayout.razor` (carpeta padre) y las páginas + su propio `_Imports.razor`
  van en la subcarpeta `Public/Pages/`** — nunca el layout en la misma carpeta que el
  `_Imports.razor` que lo declara. `Public/Pages/_Imports.razor` declara
  `@attribute [ExcludeFromInteractiveRouting]`, `@attribute [AllowAnonymous]` y
  `@layout PublicLayout`; `PublicLayout.razor` es un layout nuevo sin sidebar, con header
  público: logo + enlaces Precios / Iniciar sesión / Crear cuenta.
- Los estilos van en `app.css` (sección nueva `/* Landing pública */`), reutilizando los
  tokens y las animaciones existentes (`fade-up`, chips de instrumentos del
  `AuthVisualPanel`, guarda `prefers-reduced-motion`).

**Pasos:**
1. **Centralizar la marca**: crear `src/FundedEdge.Domain/Common/Brand.cs` con
   `public static class Brand { public const string Name = "FundedEdge"; public const string Tagline = "El copiloto financiero del trader de fondeo"; }`
   y sustituir el texto "FundedEdge" hardcodeado en: `NavMenu.razor`, `AuthCard.razor`,
   los `<PageTitle>` de todas las páginas y `App.razor`. (Los documentos MD no se tocan.)
2. `Public/PublicLayout.razor` + `Public/Pages/_Imports.razor` (patrón descrito arriba).
3. `Public/Pages/Landing.razor` (`/bienvenida`), secciones en este orden:
   héroe (titular = Tagline; subtítulo = propuesta de valor §2.1; CTA "Empieza gratis" →
   `/Account/Register`; captura del dashboard), 3 bloques de diferenciación (§2.2: KPIs
   de negocio, módulo de riesgo, IA), franja "anti-posicionamiento" (§2.3), tabla de
   precios resumida con CTA, footer.
4. `Public/Pages/Pricing.razor` (`/precios`): la tabla completa de §3 en HTML (no imagen),
   toggle mensual/anual (estático en SSR: dos columnas de precio), FAQ corta (5 preguntas:
   qué pasa al bajar de plan, qué cuenta como "cuenta activa", cómo funciona el trial,
   qué modelo de IA usa cada plan, cómo cancelo).
5. `Login.razor`/`Register.razor`: enlace discreto "← Volver a la web" hacia
   `/bienvenida`.

**DoD F3:** ✅ Completada.
- [x] `/bienvenida` y `/precios` cargan **sin sesión** (200, no 302) con CSS aplicado.
- [x] Todo texto de marca sale de `Brand.Name`/`Brand.Tagline`; `grep -rn "FundedEdge"
      src/FundedEdge.Web --include=*.razor` solo devuelve usos vía `@Brand.Name` (los
      namespaces C# y el archivo `FundedEdge.Web.styles.css` no cuentan).
- [x] Los precios de `/precios` coinciden con §3 (calculados desde `PlanLimits.For(tier)`,
      no hardcodeados dos veces).

> ⚠️ **Trampa encontrada al implementar F3** (déjala aquí para que no se repita): un
> componente de layout (`@layout X`) **nunca debe vivir en la misma carpeta** que el
> `_Imports.razor` que declara `@layout X`, porque ese `_Imports.razor` also se aplica AL
> PROPIO componente de layout, causando que se envuelva a sí mismo — recursión infinita,
> CPU al 100 % y cuelgue total de esa ruta (sin excepción, sin log, solo un hang que crece
> hasta matar el proceso). Patrón correcto (el que ya usa `Account/`): el layout vive en la
> carpeta padre (`Public/PublicLayout.razor`) y las páginas + su `_Imports.razor` con
> `@layout PublicLayout` van en una subcarpeta (`Public/Pages/`).
- [ ] Build + tests verdes.

---

### F4 — Pagos con Stripe

**Objetivo:** cobrar. Checkout de Stripe para Pro/Elite (mensual y anual), webhook que
actualiza `PlanTier`, y portal de cliente para cancelar/cambiar.

**Puntos técnicos clave:**
- Paquete NuGet `Stripe.net` en `FundedEdge.Web`.
- **Secretos SOLO por user-secrets/entorno** (regla §7): `Stripe:SecretKey`,
  `Stripe:WebhookSecret`, `Stripe:Prices:ProMonthly`, `Stripe:Prices:ProYearly`,
  `Stripe:Prices:EliteMonthly`, `Stripe:Prices:EliteYearly`. Si faltan, la app arranca
  igual y `/plan` muestra los botones deshabilitados con "Pagos no configurados" (mismo
  patrón condicional que Google OAuth en `Program.cs`).
- Guardar `StripeCustomerId` (string?) en `ApplicationUser` → **migración**
  `AddStripeCustomerId`.
- Endpoints minimal-API nuevos en `src/FundedEdge.Web/Endpoints/BillingEndpoints.cs`
  (patrón de `NinjaTraderIngestEndpoints.cs`):
  - `POST /api/billing/checkout` (requiere sesión): crea sesión de Stripe Checkout
    (mode=subscription, price según parámetro validado contra la lista blanca de 4
    prices, `success_url=/plan?ok=1`, `client_reference_id=userId`).
  - `POST /api/billing/portal` (requiere sesión): sesión del Billing Portal.
  - `POST /api/billing/webhook` (**`.AllowAnonymous()`** + verificación de firma con
    `Stripe:WebhookSecret`; rechazar con 400 si la firma no valida): manejar
    `checkout.session.completed` (asignar tier según price), y
    `customer.subscription.updated`/`deleted` (downgrade a Starter cuando corresponda).
    El mapeo price→tier vive en un único diccionario.
- `/plan` (de F1): activar los botones → POST a los endpoints de checkout/portal.

**DoD F4:** ✅ Completada.
- [x] Tests unitarios: `StripePriceCatalogTests` (mapeo price→tier bidireccional) y
      `BillingWebhookProcessorTests` (checkout.session.completed, subscription.deleted/updated,
      usuario desconocido, tipo de evento desconocido) — sobre `BillingWebhookEvent`, el DTO
      plano que separa la lógica de negocio del SDK de Stripe (no hace falta Stripe.net en el
      proyecto de tests; la verificación de firma de `EventUtility.ConstructEvent` es del propio
      SDK, no de nuestra lógica).
- [x] Webhook sin firma válida ⇒ 400 (verificado manualmente: sin `Stripe:WebhookSecret`
      configurado el endpoint devuelve 400 de inmediato); con evento válido,
      `checkout.session.completed` deja al usuario con el tier de la metadata `planTier` y su
      `StripeCustomerId` en BD (test).
- [x] Sin secretos configurados la app arranca, loguea el aviso y `/plan` muestra "Pagos no
      configurados" con los botones de upgrade deshabilitados (verificado con Playwright).
- [x] Migración `AddStripeCustomerId` incluida. Build + tests verdes (146/146: 45 Domain + 101 Application).
- [x] README: sección "Planes de suscripción y pagos (Stripe)" con los 6 secretos, cómo crear
      los productos/precios, configurar el webhook (incluye Stripe CLI para local) y tarjetas de
      prueba.

---

### F5 — Features de crecimiento

*(Cada sub-feature puede ser su propio PR; orden recomendado.)*

1. **F5.1 Export PDF del track record** (Pro/Elite): página `/export` que genera un
   resumen imprimible (KPIs de negocio + equity + funnel). Implementación simple y
   robusta: vista HTML optimizada para `window.print()` con CSS `@media print` — no
   añadir librerías PDF de pago. Gate: `IPlanService`.
2. **F5.2 Página pública de track record** (Elite): entidad nueva `PublicProfile`
   (UserId, Slug único, IsEnabled, qué secciones comparte) + migración; página SSR
   pública `/t/{slug}` (mismo patrón `Public/` de F3) que muestra SOLO datos agregados
   (nunca trades individuales ni importes de costes), con "Powered by @Brand.Name" +
   UTM. Toggle en `/plan`.
3. **F5.3 Calculadora pública de riesgo** (lead magnet, sin registro): `/calculadora` en
   `Public/`, un formulario (coste evaluación, pass rate estimado, payout medio, presupuesto
   mensual) que ejecuta `BankrollSimulator`/`EvCalculator` del dominio (¡ya existen, son
   deterministas con semilla!) con iteraciones reducidas (2.000) **sin tocar la BD**, y
   muestra P(ruina) + EV + CTA "Regístrate para usarlo con tus datos reales".
4. **F5.4 Alertas** (Elite): servicio hosted que detecta proximidad al drawdown (>80 %
   consumido) y elegibilidad de payout; primera entrega = aviso en la campana del
   dashboard (tabla `UserNotifications` + migración), email después.
5. **F5.5 Internacionalización EN**: extraer strings de UI a recursos `.resx` +
   `IStringLocalizer`. Hacerlo DESPUÉS del rebranding definitivo (§4.2) para no traducir
   dos veces. Fase grande: planificarla como serie de PRs por área.
6. **F5.6 Perfiles Elite** (Elite): ranking de traders por ROI de negocio sobre las páginas
   públicas activas (`IPeerDiscoveryService.GetLeaderboardAsync`) e informe de inspiración IA
   sobre la operativa de un par (`ITradingAnalystService.GeneratePeerInspirationReportAsync`,
   `AiReportKind.PeerInspiration`). Doble candado de privacidad: solo lo ve quien tiene plan
   Elite (`PlanLimits.CanBrowsePeers`) y solo se analiza a quien dio opt-in explícito
   (`PublicProfile.ShareOperativa` / `ShareEmotions`, ambos off por defecto). Siempre agregados
   —setups, franjas, R-múltiplos, frecuencias emocionales— nunca trades ni importes. Página
   `/peers`; toggles de compartir en `/plan`.

**DoD F5.x:** cada sub-feature con sus tests (los simuladores ya tienen patrón de test en
`tests/FundedEdge.Domain.Tests`), gates de plan verificados, y para F5.2/F5.3 verificación
explícita de que NO se filtra ningún dato sensible (revisar el DTO expuesto campo a campo).

**DoD F5:** ✅ Completada (F5.1–F5.4). F5.5 diferida explícitamente (ver nota).
- [x] **F5.1 Export PDF**: `/export` con tablas de KPIs + `<EquityChart>` y botón
      `window.print()` (`@media print` en `app.css`, oculta sidebar/toolbar). Gate:
      `PlanLimits.CanExportPdf` (Pro/Elite).
- [x] **F5.2 Track record público**: entidad `PublicProfile` (UserId, Slug único,
      IsEnabled) + migración `AddPublicProfile`; `IPublicProfileService`
      (`GetOwnSettingsAsync`/`EnableAsync`/`DisableAsync`/`GetPublicViewAsync`); página
      `/t/{slug}` en `Public/Pages/` (mismo patrón `PublicLayout` de F3). El DTO
      `PublicProfileView` expuesto solo trae `DisplayName`, `AccountsFunded`, `PassRate`,
      `TotalTrades`, `WinRate`, `ProfitFactor`, `AvgRMultiple` — ningún importe en $ ni
      trade individual (revisado campo a campo). Toggle activar/desactivar en `/plan`.
      Si el usuario deja de ser Elite, `GetPublicViewAsync` devuelve null sin borrar el
      perfil (se reactiva solo si vuelve a subir de plan). Tests en
      `PublicProfileServiceTests` (6).
- [x] **F5.3 Calculadora pública**: `/calculadora` (sibling de `PublicLayout.razor`, NO
      bajo `Public/Pages/`, porque necesita `@rendermode InteractiveServer` explícito;
      `Public/Pages/_Imports.razor` fuerza SSR estático vía `[ExcludeFromInteractiveRouting]`
      y sería incompatible). Reutiliza `EvCalculator`/`BankrollSimulator` con 2.000
      iteraciones, sin tocar la BD. Enlace desde `PublicLayout` (nav) y `Landing.razor`
      (hero).
- [x] **F5.4 Alertas de drawdown** (simplificado, ver nota): `IRiskAnalysisService.GetDrawdownAlertsAsync()`
      calcula, por cuenta activa (Evaluation/Funded) con `MaxDrawdown > 0`, el % de
      colchón de drawdown consumido a partir del equity real acumulado de sus trades
      (mismo modelo de suelo que `AccountSimulator`: trailing/EndOfDay siguen el pico,
      Static ancla a 0). Umbral fijo 80 %. Banner en el Dashboard (`Home.razor`).
      Tests en `RiskAnalysisServiceTests` (trailing, static, sin alerta, cuenta
      terminal ignorada).
- [x] Build + tests verdes (156/156: 45 Domain + 111 Application).

> **⚠️ Trampa encontrada al implementar F5.3**: con una política de autorización
> `FallbackPolicy = RequireAuthenticatedUser()` (la que usa este proyecto para proteger
> todo por defecto), **ninguna página anónima con `@rendermode InteractiveServer` puede
> abrir su circuito SignalR**, aunque la propia página tenga `[AllowAnonymous]`. El
> motivo: el transporte compartido del circuito (`/_blazor/*` — negociación, JS
> initializers) es un endpoint aparte sin metadata de página, así que el fallback lo
> bloquea igual y lo redirige a `/Account/Login`. Sin circuito, el `<EditForm>` cae a un
> POST estático de progressive-enhancement que el servidor rechaza con 400 (sin
> excepción visible en logs — solo se ve en Network/DevTools o inspeccionando el body
> `text/plain` vacío). **Fix** (`Program.cs`): cambiar el `FallbackPolicy` de
> `RequireAuthenticatedUser()` a un `RequireAssertion` que también permita rutas bajo
> `/_blazor`:
> ```csharp
> options.FallbackPolicy = new AuthorizationPolicyBuilder()
>     .RequireAssertion(ctx =>
>         ctx.User.Identity?.IsAuthenticated == true ||
>         (ctx.Resource is HttpContext httpContext && httpContext.Request.Path.StartsWithSegments("/_blazor")))
>     .Build();
> ```
> Ojo: **NO** uses `.AllowAnonymous()` sobre `app.MapRazorComponents<App>().AddInteractiveServerRenderMode()`
> como atajo — ese builder agrupa TODAS las páginas bajo un único endpoint dinámico, así
> que anula la protección de página a página (páginas protegidas dejan de redirigir a
> login y en su lugar intentan renderizar para un usuario anónimo, y revientan con
> `InvalidOperationException` al llamar a `RequireUserIdAsync()`). Verificado con
> Playwright: `/calculadora` interactivo funciona anónimo, y `/`, `/plan`, `/ai`,
> `/settings` siguen devolviendo 302 a `/Account/Login` sin sesión.

**Simplificaciones documentadas** (decisión propia, guía permite "elegir la opción más
simple" cuando no está cubierto explícitamente):
- F5.4 no crea la tabla `UserNotifications` ni un `HostedService` de fondo — se calcula
  al vuelo en cada carga del Dashboard (barato: una query por cuenta activa). No cubre
  "elegibilidad de payout" (no hay modelo de reglas de payout por firma en el dominio
  todavía). Gate en `PlanLimits.FullRiskModule` (Pro+Elite) en vez de restringirlo solo a
  Elite, ya que es una extensión natural del módulo de riesgo existente y no requería un
  campo nuevo en `PlanLimits`.
- F5.5 (i18n) queda diferida sin empezar, tal y como la propia guía recomienda
  explícitamente ("Fase grande: planificarla como serie de PRs por área", hacerla
  después del rebranding definitivo).

---

## 7. Convenciones obligatorias del repo

*(Resumen operativo para el modelo ejecutor; el detalle arquitectónico está en
`GUIA_IMPLEMENTACION.md`.)*

1. **Idioma:** UI, mensajes de error, comentarios y commits en **español**.
2. **Commits/PRs:** título prefijado con tipo (`feat:`, `fix:`, `chore:`, `refactor:`,
   `test:`, `docs:`). Al terminar cualquier cambio: commit + push + PR automáticamente.
   Si la PR anterior de la rama ya se fusionó, reiniciar la rama desde `origin/main`
   (`git fetch origin main && git checkout -B claude/funded-trading-tracker-hfbldh origin/main`)
   antes de commitear, y abrir PR nueva.
3. **Secretos:** JAMÁS en `appsettings.json` (está versionado). Solo User Secrets o
   variables de entorno. Features con secretos ausentes se degradan con aviso, nunca
   rompen el arranque (patrón Google OAuth / Stripe).
4. **Aislamiento por usuario:** todo servicio que lea/escriba datos de usuario recibe
   `ICurrentUserAccessor` y filtra/estampa `UserId`. Datos compartidos entre usuarios:
   SOLO PropFirms e Instruments. Tests: `FakeCurrentUserAccessor` (en
   `tests/FundedEdge.Application.Tests/TestHelpers.cs`).
5. **EF Core:** los tests usan el proveedor **InMemory** ⇒ **prohibido**
   `ExecuteUpdateAsync`/`ExecuteDeleteAsync` en servicios (usar entidades rastreadas +
   `SaveChangesAsync`). Cambios de esquema ⇒ migración con
   `dotnet ef migrations add <Nombre> --project src/FundedEdge.Infrastructure --startup-project src/FundedEdge.Infrastructure`
   (el binario está en `~/.dotnet/tools`; añadir al PATH si no se encuentra). `Program.cs`
   aplica migraciones al arrancar — no usar `EnsureCreated` en código de producción.
6. **Blazor:** la interactividad se declara en la raíz (`Routes` con
   `@rendermode="InteractiveServer"` en `App.razor`). Páginas que escriben cookies o
   deben ser públicas-estáticas (Account, Public) usan `[ExcludeFromInteractiveRouting]`
   + `[AllowAnonymous]` vía `_Imports.razor` de su carpeta y un layout propio.
   `FallbackPolicy` global exige sesión: cualquier endpoint/página pública nueva necesita
   `[AllowAnonymous]`/`.AllowAnonymous()` explícito.
7. **CSS:** tokens en `:root` de `app.css`; animaciones siempre dentro de
   `@media (prefers-reduced-motion: no-preference)`.
8. **Verificación visual sin SQL Server** (el entorno de CI no tiene): cambiar
   TEMPORALMENTE el provider a SQLite + `EnsureCreatedAsync`, `dotnet publish` (no
   `dotnet run`) y capturar con Playwright (`executable_path=/opt/pw-browsers/chromium`);
   **revertir el provider antes de commitear** (no debe aparecer SQLite en el diff).
9. **Calidad mínima de cada PR:** `dotnet build` con 0 warnings / 0 errors y
   `dotnet test` completo en verde.

---

## 8. Métricas de éxito (KPIs SaaS)

| Métrica | Objetivo 6 meses | Cómo se mide |
|---|---|---|
| Registros/mes | 300 | `AspNetUsers.Id` por mes |
| Activación (≥1 cuenta creada + ≥10 trades en 7 días) | 40 % de registros | consulta sobre `TradingAccounts`/`Trades` |
| Conversión trial→pago | 8–12 % | Stripe + `PlanTier` |
| MRR | 1.500 € | Stripe |
| Churn mensual (pago) | < 6 % | Stripe |
| Retención semana 4 | 30 % | último login (añadir `LastSeenAt` a `ApplicationUser` cuando se instrumente) |
| Uso de IA (usuarios de pago que consumen ≥50 % de su cupo) | 60 % | `AiReports` por usuario/ventana |

> Cuando se implemente analítica de producto, preferir eventos propios en BD (tabla
> `ProductEvents`) antes que un tercero, por privacidad y por coherencia con el
> posicionamiento "tus datos son tuyos".

---

*Documento vivo: al completar cada fase, marcar aquí su DoD y anotar desviaciones en el
PR correspondiente.*
