# TrackRecord

Aplicación .NET 10 (Blazor Server) para registrar cuentas de fondeo (Lucid Trading, Tradeify, Apex y otras), su ciclo de vida, costes y payouts, y un dashboard de KPIs de trading y de negocio.

> Diseño completo y roadmap por fases: [`GUIA_IMPLEMENTACION.md`](./GUIA_IMPLEMENTACION.md).
> Estrategia de producto, suscripciones y roadmap de monetización:
> [`GUIA_MONETIZACION_Y_MARKETING.md`](./GUIA_MONETIZACION_Y_MARKETING.md).
> Este README cubre solo la puesta en marcha del **MVP (Fase 1)** ya implementado.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **SQL Server Express** (local) — [descarga](https://www.microsoft.com/sql-server/sql-server-downloads). Durante la instalación, deja la instancia con nombre por defecto `SQLEXPRESS`.

## Puesta en marcha

1. Instala SQL Server Express y confirma que el servicio `MSSQL$SQLEXPRESS` está en marcha.
2. Restaura y compila la solución:

   ```powershell
   dotnet restore
   dotnet build
   ```

3. Arranca la app:

   ```powershell
   dotnet run --project src/TrackRecord.Web
   ```

   Al arrancar, `Program.cs` aplica automáticamente las migraciones pendientes (`db.Database.MigrateAsync()`), crea la base de datos `TrackRecord` si no existe, y siembra las firmas Lucid Trading, Tradeify y Apex Trader Funding además de un catálogo de instrumentos comunes (ES, MES, NQ, MNQ, GC, CL).

4. Abre `https://localhost:5001` (o el puerto que indique la consola).

### Cadena de conexión

Por defecto (`appsettings.json`):

```
Server=localhost\SQLEXPRESS;Database=TrackRecord;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Si tu instancia de SQL Server Express tiene otro nombre, autenticación SQL, o corre en otra máquina, sobrescribe `ConnectionStrings:Default` en `appsettings.Development.json` (no versionado) o vía variable de entorno:

```powershell
$env:ConnectionStrings__Default = "Server=MIPC\SQLEXPRESS;Database=TrackRecord;User Id=sa;Password=...;TrustServerCertificate=True"
```

## Autenticación y datos por usuario

La app requiere iniciar sesión (ASP.NET Core Identity) — todas las páginas exigen usuario
autenticado salvo login/registro. El registro está **abierto**: cualquiera puede crear una cuenta
desde `/Account/Register`, con email/contraseña o con Google.

**Verificación de email obligatoria**: tras registrarse, la cuenta no puede iniciar sesión hasta
confirmar el email (`RequireConfirmedAccount = true`) — frena el alta automatizada de bots. Con
Google, el email ya viene verificado por el proveedor y solo se pide confirmación si el usuario lo
cambia manualmente en el formulario de alta. Ver "Enviar emails de confirmación (SMTP)" más abajo
para que el enlace de confirmación se envíe de verdad; sin configurar, se muestra en pantalla
(solo apto para desarrollo).

- **Cuentas, cuentas de fondeo, trades, costes, payouts, ajustes de integraciones (Tradovate/
  NinjaTrader/IA) y divisa preferida son privados por usuario** — cada usuario solo ve y gestiona
  los suyos.
- El **catálogo de firmas de fondeo (`/firms`) e instrumentos es compartido**: lo ve y usa
  cualquier usuario registrado, igual que antes de añadir autenticación.
- **El primer usuario que se registra en una instalación nueva hereda automáticamente** las
  cuentas/informes de IA que ya existieran sin dueño (por ejemplo, los datos de una base de datos
  sembrada o migrada desde una versión anterior sin login). Los usuarios registrados después
  parten sin datos.

### Iniciar sesión con Google (opcional)

Sin configurar nada, el registro/login por email y contraseña funciona igualmente. Para habilitar
también el botón "Iniciar sesión con Google":

1. En [Google Cloud Console](https://console.cloud.google.com/apis/credentials), crea unas
   credenciales **OAuth 2.0 Client ID** de tipo "Web application".
2. Añade como **Authorized redirect URI** la URL de tu app + `/signin-google`, por ejemplo
   `https://localhost:5001/signin-google` en desarrollo local.
3. Configura el Client ID y el Client Secret vía User Secrets (nunca en `appsettings.json`):

   ```powershell
   dotnet user-secrets set "Authentication:Google:ClientId" "..." --project src/TrackRecord.Web
   dotnet user-secrets set "Authentication:Google:ClientSecret" "..." --project src/TrackRecord.Web
   ```

El botón de Google solo aparece en `/Account/Login` y `/Account/Register` si ambos valores están
configurados; si no, la app sigue funcionando solo con email/contraseña.

### Enviar emails de confirmación (SMTP)

Sin SMTP configurado, la app arranca igual y el registro sigue funcionando: en vez de recibir un
email, el enlace de confirmación se muestra directamente en la pantalla "Confirma tu email" (con
un aviso explícito). Para que se envíe un email de verdad, configura vía User Secrets (nunca en
`appsettings.json`):

```powershell
dotnet user-secrets set "Email:SmtpHost" "smtp.tuservidor.com" --project src/TrackRecord.Web
dotnet user-secrets set "Email:SmtpPort" "587" --project src/TrackRecord.Web
dotnet user-secrets set "Email:SmtpUser" "usuario" --project src/TrackRecord.Web
dotnet user-secrets set "Email:SmtpPassword" "..." --project src/TrackRecord.Web
dotnet user-secrets set "Email:From" "no-reply@tudominio.com" --project src/TrackRecord.Web
```

Solo `Email:SmtpHost` y `Email:From` son obligatorios para activarlo; `Email:FromName` es opcional
(por defecto "TrackRecord"). El envío usa [MailKit](https://github.com/jstedfast/MailKit) por
STARTTLS (puerto 587 típico). Cualquier proveedor SMTP transaccional (SendGrid, Mailgun, Amazon SES
vía SMTP, etc.) funciona con estas 5 claves.

## Idioma (ES/EN)

La UI está en español e inglés. El idioma se decide, por orden: cookie explícita (si el usuario
usó el selector ES/EN de la barra lateral o del pie de las páginas públicas) → cabecera
`Accept-Language` del navegador → español por defecto. Cambiar de idioma recarga la página
(`/culture/set?culture=en&redirectUri=...`) para que el circuito de Blazor Server arranque ya con
la cultura correcta. Las claves de traducción son el propio texto en español
(`IStringLocalizer<SharedResources>`); el inglés vive en
`src/TrackRecord.Web/Resources/SharedResources.en.resx`. Para añadir un idioma nuevo: crear
`SharedResources.{cultura}.resx` con las mismas claves y añadir la cultura a la lista de
`supportedCultures` en `Program.cs`.

## Planes de suscripción y pagos (Stripe)

TrackRecord tiene 3 planes (Starter/Pro/Elite — ver `/precios` y
[`GUIA_MONETIZACION_Y_MARKETING.md`](./GUIA_MONETIZACION_Y_MARKETING.md) §3). Todo nuevo registro
recibe automáticamente 14 días de Pro sin tarjeta; al terminar, cae a Starter. Sin Stripe
configurado, cada usuario se queda en el plan que tenga en base de datos indefinidamente y los
botones de upgrade en `/plan` aparecen deshabilitados con "Pagos no configurados".

### Configurar Stripe (opcional)

1. Crea una cuenta en [Stripe](https://dashboard.stripe.com/register) y activa el **modo de
   pruebas** (test mode) para desarrollar sin cobros reales.
2. Crea 2 productos ("Pro" y "Elite") con un precio recurrente mensual y otro anual cada uno (4
   Price Id en total) desde el [catálogo de productos](https://dashboard.stripe.com/test/products).
3. Configura los 6 secretos vía User Secrets (nunca en `appsettings.json`):

   ```powershell
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project src/TrackRecord.Web
   dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project src/TrackRecord.Web
   dotnet user-secrets set "Stripe:Prices:ProMonthly" "price_..." --project src/TrackRecord.Web
   dotnet user-secrets set "Stripe:Prices:ProYearly" "price_..." --project src/TrackRecord.Web
   dotnet user-secrets set "Stripe:Prices:EliteMonthly" "price_..." --project src/TrackRecord.Web
   dotnet user-secrets set "Stripe:Prices:EliteYearly" "price_..." --project src/TrackRecord.Web
   ```

4. Configura un webhook apuntando a `https://tu-dominio/api/billing/webhook`, suscrito a
   `checkout.session.completed`, `customer.subscription.updated` y
   `customer.subscription.deleted`. En local, usa el
   [Stripe CLI](https://docs.stripe.com/stripe-cli) para reenviar eventos:

   ```powershell
   stripe listen --forward-to https://localhost:5001/api/billing/webhook
   ```

   El comando anterior imprime el `whsec_...` que debes usar como `Stripe:WebhookSecret` en local
   (es distinto del de producción).
5. Prueba el checkout con [tarjetas de prueba de Stripe](https://docs.stripe.com/testing)
   (`4242 4242 4242 4242`, cualquier fecha futura y CVC).

Sin el paso 4 (webhook), el checkout se completa en Stripe pero la app nunca se entera de que debe
subir el plan del usuario — imprescindible para que `/plan` funcione de extremo a extremo.

## Configurar el análisis con Claude (IA)

La página `/ai` llama a la API de Claude (Anthropic) para analizar tus KPIs. **La aplicación no
trae ninguna API key incluida** — hay que generar una propia desde
[console.anthropic.com](https://console.anthropic.com/settings/keys) (requiere una cuenta con
facturación activa) y configurarla localmente. Sin ella, `/ai` sigue funcionando pero muestra un
aviso y no deja generar informes.

Elige una de estas dos vías (nunca pegues la key en `appsettings.json`, que sí está versionado):

**Opción A — variable de entorno** (más simple):

```powershell
$env:ANTHROPIC_API_KEY = "sk-ant-..."
dotnet run --project src/TrackRecord.Web
```

**Opción B — User Secrets** (recomendado si desarrollas en la misma máquina de forma continuada; el proyecto ya tiene `UserSecretsId` configurado):

```powershell
dotnet user-secrets set "Ai:ApiKey" "sk-ant-..." --project src/TrackRecord.Web
```

La app resuelve la key en este orden: `Ai:ApiKey` (configuración) → `ANTHROPIC_API_KEY` (entorno). El modelo usado es `claude-haiku-4-5` (esfuerzo bajo) para mantener el coste por informe mínimo — ideal para probar la funcionalidad; cámbialo en `ClaudeTradingAnalystService` por `claude-opus-4-8`/esfuerzo alto si quieres análisis más profundos en producción.

### Informe semanal automático (opcional)

Con la key configurada, puedes activar un informe de análisis automático cada 7 días poniendo
`"Ai": { "WeeklyReportEnabled": true }` en `appsettings.json` (o user-secrets). Está **desactivado
por defecto** porque cada informe consume créditos de la API. El intervalo se ajusta con
`Ai:WeeklyReportIntervalDays`.

Además de quedar guardado en "Análisis IA", el informe se **envía por email** al usuario (gancho
de retención semanal) si SMTP está configurado (ver "Enviar emails de confirmación (SMTP)" más
arriba) — reutiliza la misma configuración, no hace falta nada adicional. Sin SMTP configurado,
el informe se sigue generando y guardando con normalidad, simplemente no se envía email.

## Módulo de riesgo (`/risk`, Fase 3)

Responde a la pregunta central del negocio: *¿es estadísticamente viable y cuánto bankroll
necesita?* Todo se calcula con tus datos reales, nunca con supuestos genéricos:

- **EV por evaluación** observado, con intervalo de confianza 95 % (bootstrap sobre tus
  evaluaciones terminadas) y semáforo: verde (EV > 0 con ≥ 20 evaluaciones), ámbar (EV > 0 con
  muestra pequeña), rojo (EV ≤ 0) con el desglose de qué variable lo arregla antes.
- **Planner de bankroll (Monte Carlo, 10.000 iteraciones)**: cada mes se compran hasta N
  evaluaciones, cada una pasa con tu pass rate y las fondeadas cobran payouts muestreados de tu
  distribución histórica. Devuelve P(ruina), abanico P5–mediana–P95 del bankroll final, meses
  hasta breakeven y el **bankroll mínimo recomendado** para P(ruina) < 5 % (búsqueda binaria).
  Los inputs (pass rate, costes) se pueden sobrescribir para análisis de sensibilidad.
- **Riesgo intra-cuenta**: muestrea tu distribución real de PnL por trade contra el profit target
  y el drawdown (trailing/EOD/estático) de una cuenta concreta → P(pasar la evaluación),
  P(quemar la fondeada antes del primer payout) y trades esperados hasta cada desenlace.
- **Fracción de Kelly** orientativa (con recomendación de operar a ½ Kelly).

Estas métricas alimentan también el prompt del análisis con IA: Claude recibe el EV con su
intervalo, Kelly y la P(ruina) de un escenario estándar documentado.

## Sincronización automática de trades (Fase 2)

La app puede recibir trades sin captura manual desde dos fuentes: **Tradovate** (sincronización
periódica en segundo plano) y **NinjaTrader 8** (push desde un AddOn). Ambas comparten el mismo
pipeline de ingesta idempotente (`IExecutionIngestService` + `TradeBuilder` FIFO), así que un trade
importado, sincronizado o manual es indistinguible salvo por el `Source` de sus `Execution`.

### Configurar credenciales — página `/settings`

La forma recomendada de configurar Tradovate y la API key de ingesta de NinjaTrader es la página
**`/settings`** de la propia app: las credenciales se guardan cifradas en la base de datos
(`IntegrationSettings`, vía `IDataProtector`) y tienen prioridad sobre lo que haya en configuración.

- **Tradovate**: usuario, contraseña, CID y Secret de tu app registrada en Tradovate (no tu login
  habitual de terceros — ver `GUIA_IMPLEMENTACION.md` §5 para cómo solicitarla). Tras guardarlas,
  cada cuenta con Feed = Tradovate se sincroniza sola cada `Sync:IntervalMinutes` (10 min por
  defecto, configurable en `appsettings.json`); también hay un botón "Sincronizar ahora" en
  `/settings` (todas las cuentas) y en cada ficha de cuenta (`/accounts/{id}`, solo esa cuenta).
- **NinjaTrader 8**: genera una API key propia (cualquier cadena aleatoria larga) y guárdala ahí.
  Es la misma clave que debes poner en la variable de entorno `TRACKRECORD_API_KEY` al instalar el
  AddOn — ver [`integrations/ninjatrader/README.md`](./integrations/ninjatrader/README.md).

Alternativamente, ambas también se pueden configurar sin tocar la UI vía User Secrets/entorno
(nunca en `appsettings.json` versionado) — útil para despliegues automatizados:

```powershell
dotnet user-secrets set "Tradovate:Name" "..." --project src/TrackRecord.Web
dotnet user-secrets set "Tradovate:Password" "..." --project src/TrackRecord.Web
dotnet user-secrets set "Tradovate:Cid" "..." --project src/TrackRecord.Web
dotnet user-secrets set "Tradovate:Sec" "..." --project src/TrackRecord.Web
dotnet user-secrets set "Ingest:NinjaTraderApiKey" "..." --project src/TrackRecord.Web
```

Lo guardado desde `/settings` (base de datos) siempre tiene prioridad sobre estos valores de
configuración si ambos están presentes.

### Conectar una cuenta a un feed

Cada cuenta de fondeo tiene un `Feed` (`Manual` / `Tradovate` / `NinjaTrader`) y un `ExternalAccountId`
(el nombre/ID de esa cuenta en la plataforma correspondiente). Se editan desde la sección "Conexión"
en la ficha de la cuenta (`/accounts/{id}`) — no hace falta recrear la cuenta para conectarla después.

### NinjaTrader 8

El AddOn `integrations/ninjatrader/TrackRecordExporter.cs` envía cada fill por HTTP al arrancar
NT8; ver su README para instalación, mapeo de cuentas y la cola de reintentos local. Es un archivo
de referencia deliberadamente fuera de la solución .NET (depende de ensamblados de NinjaTrader que
no son paquetes NuGet) — se copia dentro de NinjaTrader 8 (`Documents\NinjaTrader 8\bin\Custom\AddOns`).

## Estructura de la solución

```
TrackRecord.slnx
├── src/
│   ├── TrackRecord.Domain          # Entidades y lógica de dominio (sin dependencias externas)
│   ├── TrackRecord.Application     # Interfaces de servicio, DTOs, definición de KPIs
│   ├── TrackRecord.Infrastructure  # EF Core + SQL Server, implementación de servicios, migraciones
│   └── TrackRecord.Web             # Blazor Server: dashboard y páginas CRUD
└── tests/
    ├── TrackRecord.Domain.Tests       # Ciclo de vida, TradeBuilder FIFO, simuladores Monte Carlo/EV
    └── TrackRecord.Application.Tests  # KPIs, ingesta/CSV/Tradovate/settings/riesgo, con EF Core InMemory
```

## Qué incluye esta versión (Fases 1–3 completas)

- CRUD de firmas de fondeo (`/firms`), sembradas con Lucid Trading, Tradeify y Apex Trader Funding.
- Alta de cuentas de fondeo (`/accounts`) con tamaño, profit target, drawdown y coste de evaluación.
- Ficha de cuenta (`/accounts/{id}`) con:
  - Transición de etapa (Evaluación → Fondeada → Fallida/Retirada/Expirada) con historial auditable.
  - Registro de costes (evaluación, activación, reset, cuota mensual...).
  - Registro de payouts (solicitado, recibido, estado).
  - Journal manual de trades (símbolo, dirección, P&L, comisiones, riesgo → R-múltiplo).
  - Importación de CSV de NinjaTrader 8 (export "Trade Performance"), idempotente al reimportar.
  - Conexión a Tradovate/NinjaTrader (feed + ID externo) y botón "Sincronizar ahora".
- Dashboard (`/`) con KPIs de negocio (pass rate, coste por cuenta fondeada, payout medio, ROI, cashflow neto) y de trading (win rate, profit factor, expectancy, drawdown, rachas).
- **Análisis con IA (`/ai`)**: informe completo bajo demanda (fortalezas, fugas, viabilidad del negocio, plan de acción) y preguntas puntuales sobre tu operativa, usando Claude (`claude-haiku-4-5`, esfuerzo bajo) sobre los KPIs agregados. Histórico de informes persistido en `AiReports`.
- **Sincronización automática (Fase 2)**: `TradeBuilder` reconstruye trades FIFO a partir de fills
  individuales (soporta escalados, cierres parciales y flips); `TradeSyncService` sincroniza
  Tradovate en segundo plano; un endpoint de ingesta (`POST /api/ingest/ninjatrader/executions`,
  protegido con API key) recibe fills en tiempo real del AddOn de NinjaTrader 8. Ver `/settings`.
- **Módulo de riesgo (`/risk`, Fase 3)**: EV por evaluación con IC 95 % (bootstrap), Monte Carlo
  de ruina del bankroll con bankroll mínimo recomendado, Monte Carlo intra-cuenta contra las
  reglas de la firma, fracción de Kelly, e integración de todo ello en el prompt de IA. Informe
  semanal automático opcional (`Ai:WeeklyReportEnabled`).
- **Divisa de visualización**: selector $ USD / € EUR en la barra lateral (USD por defecto). Solo
  cambia el símbolo/formato con el que se muestran los importes en toda la app (dashboard, cuentas,
  riesgo, gráficas) — no hay conversión de tipo de cambio, los importes se guardan tal cual se
  introducen. La preferencia se persiste por instancia (`CurrencyPreferenceService`).

### Trades manuales y la entidad `Execution` (preparado para Tradovate/NT8)

El journal manual de trades **no** escribe directamente un `Trade` suelto: `ManualTradeFactory`
(en `TrackRecord.Domain.Trades`) construye el `Trade` junto con las dos `Execution` (entrada y
salida, `Source = Manual`) que lo representan — la misma entidad `Execution` que usará el futuro
`TradeSyncService` para los fills reales de Tradovate (`Source = Tradovate`) y NinjaTrader 8
(`Source = NinjaTraderAddOn`) descritos en la guía (§5-7). Un trade manual y uno importado son
indistinguibles a nivel de almacenamiento; solo cambia el origen de sus `Execution`. Al eliminar
un trade solo se borran sus `Execution` manuales (sintéticas); los fills de fuentes reales se
conservan huérfanos para que un futuro `TradeBuilder` (FIFO) pueda reconstruir trades a partir de
ellos.

**Fuera de alcance de esta versión** (Fase 4 del roadmap de la guía): sincronización en tiempo
real vía WebSocket de Tradovate (hoy es *pull* periódico, cada `Sync:IntervalMinutes`), alertas de
proximidad a drawdown/elegibilidad de payout, etiquetado de trades asistido por IA y export PDF
del track record.

## Ejecutar los tests

```powershell
dotnet test
```

## Ejecutar/gestionar migraciones manualmente

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add NombreMigracion --project src/TrackRecord.Infrastructure --startup-project src/TrackRecord.Infrastructure
dotnet ef database update --project src/TrackRecord.Infrastructure --startup-project src/TrackRecord.Infrastructure
```
