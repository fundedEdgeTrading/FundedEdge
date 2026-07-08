# Guía funcional y técnica de seguridad — TrackRecord

> **Objetivo.** Servir de plan de acción, revisable y accionable, para llevar TrackRecord de un MVP
> funcional a una aplicación web endurecida y lista para producción multiusuario. Documenta las
> vulnerabilidades y brechas detectadas en el código actual, su impacto, su ubicación exacta y la
> solución concreta (con código .NET listo para pegar), organizadas **por fases** de prioridad
> decreciente.
>
> **Alcance del análisis.** Rama `claude/web-app-security-guide-jo2of8`. Se revisó el 100 % del código
> de aplicación (`src/`), las integraciones (`integrations/`) y la configuración. La guía se apoya en
> **OWASP Top 10 (2021)** y **OWASP ASVS** como marcos de referencia.
>
> **Cómo leer esta guía.** Cada hallazgo lleva un identificador (`SEC-nn`), severidad, la referencia
> `archivo:línea`, el porqué del riesgo y la corrección. La sección [Inventario](#inventario-de-hallazgos)
> resume todo en una tabla; las [Fases](#hoja-de-ruta-por-fases) lo ordenan como plan de trabajo; el
> [Checklist de producción](#checklist-de-despliegue-a-producción) es la puerta de salida antes de exponer
> la app a Internet.

---

## Índice

1. [Resumen ejecutivo](#resumen-ejecutivo)
2. [Postura de seguridad actual (lo que ya está bien)](#postura-de-seguridad-actual-lo-que-ya-está-bien)
3. [Inventario de hallazgos](#inventario-de-hallazgos)
4. [Hoja de ruta por fases](#hoja-de-ruta-por-fases)
   - [Fase 0 — Endurecimiento inmediato (horas)](#fase-0--endurecimiento-inmediato-horas)
   - [Fase 1 — Autenticación y control de acceso](#fase-1--autenticación-y-control-de-acceso)
   - [Fase 2 — Secretos, criptografía y transporte](#fase-2--secretos-criptografía-y-transporte)
   - [Fase 3 — Endpoints, integraciones y APIs](#fase-3--endpoints-integraciones-y-apis)
   - [Fase 4 — Cabeceras, CSP y front-end](#fase-4--cabeceras-csp-y-front-end)
   - [Fase 5 — Observabilidad, auditoría y operaciones](#fase-5--observabilidad-auditoría-y-operaciones)
   - [Fase 6 — Cumplimiento, privacidad y mejora continua](#fase-6--cumplimiento-privacidad-y-mejora-continua)
5. [Checklist de despliegue a producción](#checklist-de-despliegue-a-producción)

---

## Resumen ejecutivo

TrackRecord parte de una base **más sólida de lo habitual en un MVP**: el aislamiento de datos por
usuario está aplicado de forma consistente (toda consulta filtra por `UserId`), los secretos de
integración se cifran con `IDataProtector`, el antiforgery está activo globalmente, el contenido
generado por IA se renderiza con HTML deshabilitado, y las comparaciones de API key son en tiempo
constante. **No se detectaron vulnerabilidades críticas de tipo inyección SQL, XSS almacenado ni
acceso horizontal a datos de otros usuarios (IDOR).**

Los riesgos que sí existen se concentran en **defensas de perímetro que aún no se han añadido**
porque el proyecto nació como MVP local monousuario:

| Prioridad | Tema | Hallazgos |
|-----------|------|-----------|
| 🔴 Alta | Fuerza bruta sin freno: lockout desactivado y **cero rate limiting** | SEC-01, SEC-02, SEC-08 |
| 🟠 Media-alta | Gestión de claves de Data Protection sin protección en reposo; TLS a SQL sin validar | SEC-05, SEC-06 |
| 🟠 Media | Política de contraseñas débil; sin cabeceras de seguridad ni CSP; `AllowedHosts:*` | SEC-03, SEC-07, SEC-09 |
| 🟡 Baja-media | Fugas de detalle en mensajes de error; enumeración de usuarios/slugs; migraciones al arranque | SEC-04, SEC-10, SEC-11, SEC-12 |

Ninguno exige un rediseño: son adiciones de configuración y de middleware. El grueso del trabajo de
**Fase 0 y Fase 1 se puede completar en 1–2 días** y cierra el 80 % del riesgo real de explotación.

---

## Postura de seguridad actual (lo que ya está bien)

Conviene documentar los controles existentes para **no romperlos** durante el endurecimiento:

- **Autorización por defecto (deny-by-default).** `Program.cs:44-49` aplica una `FallbackPolicy` que
  exige autenticación en toda página salvo las marcadas `[AllowAnonymous]`. Es más seguro que anotar
  `[Authorize]` página a página.
- **Aislamiento de datos por usuario.** Todos los servicios (`TradingAccountService`,
  `PublicProfileService`, `ClaudeTradingAnalystService`, `ExecutionIngestService`…) resuelven el
  `userId` y filtran **cada** consulta por `a.UserId == userId`, incluyendo comprobaciones de
  propiedad antes de escribir (`EnsureAccountOwnedAsync`). No se detectó IDOR.
- **Secretos de integración cifrados en BD** con `IDataProtector`
  (`DataProtectedIntegrationSettingsStore.cs`), nunca en claro.
- **Comparación de API key en tiempo constante** (`CryptographicOperations.FixedTimeEquals`,
  `DataProtectedIntegrationSettingsStore.cs:89`), mitigando timing attacks.
- **Webhook de Stripe con firma verificada** (`EventUtility.ConstructEvent`, `BillingEndpoints.cs:116`).
- **Antiforgery global** (`app.UseAntiforgery()`, `Program.cs:132`); los endpoints con `[FromForm]`
  quedan protegidos y la ingesta JSON no es explotable por CSRF.
- **Markdown de IA sin HTML.** `Ai.razor:91` usa `.DisableHtml()` en Markdig → no hay XSS por la
  respuesta del LLM. Razor codifica por defecto el resto de interpolaciones (`@_view.DisplayName`, etc.).
- **Confirmación de email obligatoria** (`RequireConfirmedAccount = true`) y redirección local segura
  en `/culture/set` (valida que `redirectUri` empiece por `/` y no por `//`).
- **Secretos fuera del control de versiones:** `appsettings.json` no contiene claves reales; el
  README documenta User Secrets/variables de entorno; `.gitignore` cubre `*.env`, `appsettings.*.local.json`, etc.

---

## Inventario de hallazgos

| ID | Severidad | Categoría OWASP | Título | Ubicación |
|----|-----------|-----------------|--------|-----------|
| SEC-01 | 🔴 Alta | A07 Fallos de identificación/autenticación | Lockout de cuenta desactivado en el login | `Login.razor:96` |
| SEC-02 | 🔴 Alta | A04 Diseño inseguro / A07 | Sin rate limiting en toda la aplicación | `Program.cs` (ausente) |
| SEC-03 | 🟠 Media | A07 | Política de contraseñas débil (mínimo 6, sin config explícita) | `Register.razor:127`, `DependencyInjection.cs:48` |
| SEC-04 | 🟡 Baja-media | A05 Configuración incorrecta | Enumeración de usuarios por mensajes de login/registro | `Login.razor:109-113` |
| SEC-05 | 🟠 Media-alta | A02 Fallos criptográficos | Key ring de Data Protection sin proteger en reposo | `DependencyInjection.cs:40-42` |
| SEC-06 | 🟠 Media-alta | A02 / A05 | `TrustServerCertificate=True` deshabilita validación TLS a SQL | `appsettings.json:11` |
| SEC-07 | 🟠 Media | A05 | Faltan cabeceras de seguridad y CSP | `App.razor`, `Program.cs` (ausente) |
| SEC-08 | 🟠 Media | A04 / A07 | Ingesta NinjaTrader: API key sin exigencia de fortaleza ni límite de intentos | `NinjaTraderIngestEndpoints.cs:23`, `IntegrationSettingsService.cs:75` |
| SEC-09 | 🟠 Media | A05 / A10 SSRF-adyacente | `AllowedHosts:*` + URLs de Stripe construidas con `Request.Host` | `appsettings.json:9`, `BillingEndpoints.cs:51,87` |
| SEC-10 | 🟡 Baja-media | A05 / A09 | Detalles de excepción expuestos en la UI | `Settings.razor:214`, `Ai.razor:135`, `TradovateClient.cs:71` |
| SEC-11 | 🟡 Baja | A01 Control de acceso roto (perímetro) | Slug público de 32 bits, enumerable y sin throttling | `PublicProfileService.cs:121` |
| SEC-12 | 🟡 Baja | A05 / A08 | Migraciones EF aplicadas automáticamente al arrancar | `Program.cs:106-111` |
| SEC-13 | 🟡 Baja | A09 Fallos de logging y monitorización | Sin auditoría de eventos de seguridad | Transversal |
| SEC-14 | 🟡 Baja | A05 | Cookies de Identity sin política `Secure`/`SameSite` explícita para producción | `Program.cs:55-60` |
| SEC-15 | 🟡 Baja | A06 Componentes vulnerables | Sin escaneo de dependencias en CI | `*.csproj` |
| SEC-16 | 🟡 Informativo | A08 Integridad datos/software | Webhook de Stripe sin idempotencia por `event.id` | `BillingEndpoints.cs:98`, `BillingWebhookProcessor.cs` |

### Estado de implementación

Correcciones aplicadas en el código (rama `claude/web-app-security-guide-jo2of8`), de mayor a menor prioridad:

| ID | Estado | Notas |
|----|--------|-------|
| SEC-01 | ✅ Implementado | `lockoutOnFailure: true` + lockout configurado (5 intentos → 15 min). |
| SEC-02 | ✅ Implementado | Rate limiter para login/registro (10/min·IP), ingesta (120/min·IP) y perfiles públicos (60/min·IP). |
| SEC-03 | ✅ Implementado | Contraseña mínima 10, con dígito/mayúscula/minúscula y 4 chars únicos. |
| SEC-07 | ✅ Implementado | Cabeceras de seguridad + CSP (compatible con Blazor Server). |
| SEC-08 | ✅ Implementado | Generación de API key en servidor (256 bits) + validación de longitud mínima (24) + rate limiting. |
| SEC-09 | ✅ Implementado | URLs de Stripe desde `App:BaseUrl` en lugar de `Request.Host`. |
| SEC-10 | ✅ Implementado | Mensajes de error genéricos en `Settings.razor`; detalle solo al log. |
| SEC-11 | ✅ Implementado | Rate limiting en `/t/{slug}`. |
| SEC-14 | ✅ Implementado | Cookies `HttpOnly` + `Secure` (Always en prod) + `SameSite=Lax`. |
| SEC-04 | ✅ Implementado | Anti-enumeración en registro: un email duplicado responde como un alta correcta. |
| SEC-12 | ✅ Implementado | Migración automática desactivable con `Database:AutoMigrate=false` (multi-instancia). |
| SEC-13 | ✅ Implementado | Auditoría de inicios de sesión fallidos con IP (sin volcar PII). |
| SEC-15 | ✅ Implementado | Workflow de CI: build, tests y `dotnet list package --vulnerable` como gate. |
| SEC-16 | ✅ Implementado | Tabla `ProcessedWebhookEvents` + dedup por `event.id` en el procesador (con migración). |
| SEC-05 | 📋 Pendiente (infra) | Requiere almacén compartido + KMS/certificado; ver Fase 2. |
| SEC-06 | 📋 Pendiente (config) | Cadena de producción con `Encrypt=True;TrustServerCertificate=False` vía entorno. |

> Para activar SEC-09 en producción, define `App:BaseUrl` (p. ej. `https://trackrecord.app`) y restringe
> `AllowedHosts` a tus dominios, ambos vía variables de entorno.

---

## Hoja de ruta por fases

### Fase 0 — Endurecimiento inmediato (horas)

> Cambios de bajo riesgo y alto impacto que se pueden desplegar el mismo día. Cierran la puerta a los
> ataques automatizados más comunes.

#### SEC-01 · Reactivar el lockout de cuenta

**Riesgo.** En `Login.razor:96` el inicio de sesión llama a `PasswordSignInAsync(..., lockoutOnFailure: false)`.
Con `false`, los intentos fallidos **nunca** incrementan el contador de bloqueo, de modo que aunque
ASP.NET Identity trae lockout por defecto (5 intentos → 5 min), **está anulado**. Un atacante puede
probar contraseñas sin límite contra una cuenta conocida.

**Solución.** Activar el lockout en fallo y configurarlo explícitamente.

```csharp
// Login.razor — LoginUser()
var result = await SignInManager.PasswordSignInAsync(
    Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true); // ← true
```

```csharp
// DependencyInjection.cs — AddIdentityCore
services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    // Lockout explícito (no depender de los defaults).
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
```

**Verificación.** 6 intentos fallidos seguidos deben redirigir a `Account/Lockout`
(la página ya existe) y bloquear la cuenta 15 minutos.

#### SEC-07 · Añadir cabeceras de seguridad HTTP

**Riesgo.** No se emiten `X-Content-Type-Options`, `X-Frame-Options`/`frame-ancestors` (clickjacking),
`Referrer-Policy` ni `Content-Security-Policy`. Blazor Server mitiga parte del XSS, pero una CSP
estricta es defensa en profundidad y `X-Frame-Options` evita el secuestro del circuito por iframe.

**Solución.** Middleware de cabeceras en `Program.cs` (antes de `MapRazorComponents`):

```csharp
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "DENY";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // CSP: Blazor Server necesita conexión WebSocket a 'self' y estilos inline de Bootstrap.
    // Empieza en modo Report-Only para no romper nada y endurécela progresivamente.
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self'; " +
        "connect-src 'self' wss://; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";
    await next();
});
```

> **Nota Blazor.** El script de arranque `blazor.web.js` y los *JS initializers* se sirven desde
> `'self'`, así que `script-src 'self'` es compatible. Si en el futuro se añaden scripts inline,
> usa `nonce` en lugar de `'unsafe-inline'`. Prueba primero con `Content-Security-Policy-Report-Only`.

#### SEC-06 · Endurecer la conexión a SQL Server

**Riesgo.** `appsettings.json:11` usa `TrustServerCertificate=True`, que **desactiva la validación
del certificado TLS** de SQL Server. En producción (BD en otro host) esto permite MITM sobre la
conexión de base de datos.

**Solución.** En producción, usar `Encrypt=True;TrustServerCertificate=False` con un certificado válido
en el servidor SQL. La cadena de producción va por variable de entorno (nunca en `appsettings.json`):

```
ConnectionStrings__Default=Server=tu-sql;Database=TrackRecord;User Id=trackrecord_app;Password=...;Encrypt=True;TrustServerCertificate=False
```

Deja el `TrustServerCertificate=True` **solo** en el `appsettings.Development.json` local si hace falta.

#### SEC-14 · Forzar cookies seguras en producción

**Solución.** Configurar la política de cookies de Identity en `Program.cs`:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax; // Lax: compatible con el retorno OAuth de Google
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});
```

---

### Fase 1 — Autenticación y control de acceso

#### SEC-02 · Rate limiting en endpoints sensibles

**Riesgo.** No existe `AddRateLimiter` en ningún punto del pipeline (verificado: solo aparece en un
test). Login, registro, reenvío de confirmación, checkout/portal de Stripe y **especialmente el
endpoint público de ingesta de NinjaTrader** (`/api/ingest/ninjatrader/executions`) aceptan peticiones
sin límite. Esto habilita fuerza bruta de contraseñas y de API keys, y abuso de recursos.

**Solución.** Rate limiter nativo de ASP.NET Core (.NET 10) con políticas por tipo de tráfico:

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Autenticación: por IP, ventana estrecha.
    options.AddPolicy("auth", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    // Ingesta por API key: por IP, permite ráfagas legítimas del AddOn.
    options.AddPolicy("ingest", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));

    // Límite global de fondo para el resto.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1) }));
});

// ...tras app.Build():
app.UseRateLimiter();

// Aplica la política a la ingesta:
app.MapPost("/api/ingest/ninjatrader/executions", HandleIngestAsync)
   .AllowAnonymous()
   .RequireRateLimiting("ingest");
```

Para las páginas Razor de `/Account/*` (Login/Register), aplica la política `"auth"` mediante un
convention o un endpoint filter sobre el grupo de Identity, o un middleware condicional por ruta.

> **Detrás de un proxy inverso** (Nginx, Azure Front Door, Cloudflare): habilita
> `app.UseForwardedHeaders()` con `KnownProxies`/`KnownNetworks` configurados para que
> `RemoteIpAddress` sea la IP real del cliente y no la del proxy. Sin esto, el particionado por IP
> agrupa a todos los usuarios bajo la IP del proxy.

#### SEC-03 · Política de contraseñas robusta

**Riesgo.** `Register.razor:127` exige mínimo 6 caracteres y en `DependencyInjection.cs` no se
configura `options.Password`, por lo que se heredan los defaults (longitud 6). 6 caracteres es
insuficiente frente a fuerza bruta offline si alguna vez se filtra el hash.

**Solución.** Elevar la política y alinear el `StringLength` del `InputModel`:

```csharp
// DependencyInjection.cs — AddIdentityCore
options.Password.RequiredLength = 10;
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = false; // frases largas > símbolos obligatorios
options.Password.RequiredUniqueChars = 4;
```

Actualiza el `MinimumLength = 10` y el texto “Mínimo 10 caracteres” en `Register.razor`. Opcional
(recomendado): comprobar contraseñas contra el corpus filtrado de **HaveIBeenPwned** (k-anonymity)
con un `IPasswordValidator<ApplicationUser>` personalizado.

#### SEC-04 · Reducir la enumeración de usuarios

**Riesgo.** En `Login.razor:109-113`, cuando el email existe pero no está confirmado se muestra
“confirma tu email antes de iniciar sesión”, revelando que la cuenta existe. El registro también
puede confirmar existencia por el error de email duplicado.

**Solución.** Es una decisión de compromiso UX/seguridad. Como mínimo:
- Mantén el mensaje genérico “email o contraseña incorrectos” cuando las credenciales no coinciden
  (ya se hace).
- Para email sin confirmar, considera un mensaje neutro (“Si tu cuenta existe y no está confirmada,
  te hemos reenviado el enlace”) y reenvía automáticamente, en lugar de afirmar que existe.
- En el registro, responde siempre con la pantalla de “te hemos enviado un email” aunque el email ya
  estuviera registrado (y, en ese caso, envía un aviso de intento de re-registro a la dirección real).
- Combínalo con el rate limiting `"auth"` de SEC-02 para que la enumeración masiva no sea viable.

---

### Fase 2 — Secretos, criptografía y transporte

#### SEC-05 · Proteger el key ring de Data Protection en reposo

**Riesgo.** `DependencyInjection.cs:40-42` persiste las claves de Data Protection en el sistema de
archivos **sin cifrarlas en reposo** (`PersistKeysToFileSystem` a solas). Ese key ring descifra
**todos** los secretos de integración de todos los usuarios (credenciales de Tradovate, API keys de
NinjaTrader). Si se filtra el disco o un backup, todos esos secretos quedan expuestos. Además, en un
despliegue con **múltiples instancias o contenedores efímeros**, `LocalApplicationData` no es
compartido ni persistente → las claves divergen y los secretos guardados dejan de descifrarse tras
un reinicio/escalado (justo el fallo que el `catch (CryptographicException)` enmascara).

**Solución.** Persistir el key ring en un almacén compartido y cifrarlo con una clave gestionada:

```csharp
// Producción (ejemplo Azure). Ajusta al proveedor real.
services.AddDataProtection()
    .SetApplicationName("TrackRecord")
    .PersistKeysToAzureBlobStorage(blobUri, credential)     // almacén compartido y persistente
    .ProtectKeysWithAzureKeyVault(keyVaultKeyId, credential); // cifrado en reposo con KMS
```

Alternativas equivalentes: `PersistKeysToDbContext<TrackRecordDbContext>()` (comparte vía la propia
BD) + `ProtectKeysWithCertificate(cert)`; o en Linux/K8s, un volumen persistente cifrado + certificado.
Deja el `PersistKeysToFileSystem` sin protección **solo** para desarrollo local.

> **Rotación.** Documenta que rotar/perder el key ring invalida los secretos ya cifrados: los usuarios
> tendrían que volver a introducir sus credenciales de Tradovate y su API key. El diseño ya lo tolera
> sin caerse, pero conviene avisarlo en operaciones.

#### SEC-06 · TLS a SQL Server

Tratado en Fase 0 (misma corrección). En Fase 2, complétalo con un **usuario de BD de mínimo
privilegio**: `trackrecord_app` con permisos solo sobre el esquema de la aplicación (`db_datareader`,
`db_datawriter` y ejecución de migraciones en un despliegue controlado), **nunca** `sa`. El README
muestra hoy un ejemplo con `sa` — sustitúyelo.

#### Gestión de secretos de aplicación

Verificado que hoy es correcto (User Secrets/variables de entorno, nada en `appsettings.json`
versionado). Para producción, formaliza el origen de secretos:

- **Anthropic** (`ANTHROPIC_API_KEY`), **Stripe** (`Stripe:*`), **Google OAuth**, **SMTP**: inyéctalos
  como variables de entorno desde un gestor de secretos (Azure Key Vault, AWS Secrets Manager, Doppler…),
  no como ficheros en el host.
- Añade rotación periódica y un runbook de “clave comprometida” (revocar en el proveedor + redeploy).

---

### Fase 3 — Endpoints, integraciones y APIs

#### SEC-08 · Fortaleza y ciclo de vida de la API key de ingesta

**Riesgo.** `IntegrationSettingsService.SaveIngestApiKeyAsync` (`:75`) guarda **cualquier** cadena
que el usuario escriba, sin exigir longitud ni entropía; la UI (`Settings.razor:107`) solo sugiere
“genera una cadena aleatoria larga”. Una clave débil, combinada con la ausencia de rate limiting
(SEC-02) sobre un endpoint `[AllowAnonymous]` que busca la clave entre todos los usuarios, es
fuerza-bruteable. Aunque la comparación es en tiempo constante, la política de la clave la marca el usuario.

**Solución.**
1. **Genera la clave en el servidor** en lugar de pedirla. Añade un botón “Generar API key” que cree
   un token aleatorio criptográficamente seguro y lo muestre **una sola vez**:

   ```csharp
   // 32 bytes → 43 chars base64url, ~256 bits de entropía
   public static string GenerateApiKey() =>
       Base64UrlTextEncoder.Encode(RandomNumberGenerator.GetBytes(32));
   ```

2. **Valida en el servidor** si aún permites entrada manual: rechaza claves < 24 caracteres.
3. **Rate limiting** en el endpoint (política `"ingest"` de SEC-02).
4. **Recomendado**: enlaza Kestrel a `localhost` cuando NT8 corre en la misma máquina (ya sugerido en
   el XML-doc del endpoint) y documenta que la clave se envía por `X-Api-Key` solo sobre HTTPS.
5. **Opcional (defensa en profundidad)**: guarda un **hash** de la API key en lugar del valor reversible.
   Hoy se cifra con Data Protection (reversible) porque se busca por valor; si migras a hash + índice,
   el endpoint puede resolver el usuario por hash sin descifrar y sin recorrer a todos los usuarios.

#### SEC-16 · Idempotencia del webhook de Stripe

**Riesgo.** `BillingWebhookProcessor` aplica el evento sin registrar el `event.id` procesado. La firma
(`ConstructEvent`) ya incluye timestamp con tolerancia por defecto (~5 min), lo que limita el replay,
pero Stripe **puede reenviar** el mismo evento legítimamente. Reaplicar es idempotente en la práctica
para los casos actuales (fija un tier concreto), así que el riesgo es informativo.

**Solución (recomendada al añadir eventos con efectos acumulativos).** Persistir los `event.id`
procesados y descartar duplicados:

```csharp
if (await db.ProcessedWebhookEvents.AnyAsync(e => e.Id == stripeEvent.Id, ct))
    return Results.Ok(); // ya procesado
```

#### SEC-10 · No filtrar detalles de excepción a la UI

**Riesgo.** Varios `catch (Exception ex) { _errorMessage = ex.Message; }`
(`Settings.razor:214`, `Ai.razor:135`) muestran el mensaje interno al usuario. Peor:
`TradovateClient.cs:71` incluye el **cuerpo crudo de la respuesta upstream** en la excepción, que
puede acabar en pantalla o en logs, filtrando detalles de la API de Tradovate.

**Solución.** Mensaje genérico al usuario + detalle solo al log del servidor:

```csharp
catch (Exception ex)
{
    Logger.LogError(ex, "Fallo al guardar credenciales de Tradovate.");
    _errorMessage = "No se pudieron guardar las credenciales. Inténtalo de nuevo.";
}
```

Para `TradovateApiException`, registra el body por logging estructurado pero **no** lo propagues al
mensaje mostrado. Verifica también que `ex.Message` de reglas de negocio legítimas (p. ej. límites de
plan) sí se pueda mostrar: distíngue excepciones de dominio (mostrables) de las técnicas (genéricas),
por ejemplo con un tipo `DomainException` propio.

---

### Fase 4 — Cabeceras, CSP y front-end

- **SEC-07** (cabeceras + CSP): implementado en Fase 0 en modo permisivo. En Fase 4, **endurece la CSP**:
  pasa de `Content-Security-Policy-Report-Only` a enforcing, elimina `'unsafe-inline'` de `style-src`
  migrando los estilos inline a clases, y añade un endpoint de recolección de informes
  (`report-to`/`report-uri`) para detectar violaciones antes de bloquear a usuarios reales.
- **`lang`/`data-bs-theme` y superficie estática.** `App.razor` sirve Bootstrap y CSS propios desde
  `'self'`, compatible con la CSP. No hay CDNs externos que abrir en la política — mantenerlo así.
- **`X-Frame-Options: DENY`** protege el circuito Blazor de clickjacking; si en el futuro necesitas
  embeber el track record público en iframes de terceros, crea una ruta específica con `frame-ancestors`
  acotado en lugar de relajar la política global.
- **Subresource Integrity / assets.** `MapStaticAssets` de .NET 10 ya versiona y cachea con fingerprint;
  mantén las librerías (`wwwroot/lib`) actualizadas (ver SEC-15).

---

### Fase 5 — Observabilidad, auditoría y operaciones

#### SEC-13 · Auditoría de eventos de seguridad

**Riesgo.** Hoy solo se registran mensajes informativos genéricos (“Usuario autenticado”,
`Login.razor:100`) sin correlación ni contexto. No hay traza de **logins fallidos con IP**, uso de
API key, cambios de plan, ni de credenciales. Ante un incidente no habría forma de reconstruir qué pasó.

**Solución.** Introduce logging estructured para eventos de seguridad (sin PII sensible ni secretos):

- Login OK/KO con `userId` (no email), IP y user-agent.
- Bloqueo de cuenta (lockout).
- Alta/baja/uso de API key de ingesta y de credenciales de Tradovate.
- Cambios de `PlanTier` vía webhook (ya se loguea parcialmente en `BillingWebhookProcessor`).
- Errores 4xx/5xx anómalos y rechazos 429 del rate limiter.

Envía los logs a un backend centralizado (Seq, Application Insights, ELK) con retención y alertas
(p. ej. “N lockouts en M minutos”). **Nunca** loguees contraseñas, tokens, API keys ni cuerpos de
respuesta de terceros con secretos.

#### SEC-12 · Migraciones automáticas al arranque

**Riesgo.** `Program.cs:106-111` ejecuta `db.Database.MigrateAsync()` en cada arranque. Cómodo para
el MVP local, arriesgado en producción: exige que la cuenta de BD de la app tenga permisos DDL, y con
**múltiples instancias** puede haber carreras al migrar en paralelo.

**Solución.** Separa la migración del arranque de la app:
- Ejecuta migraciones como **paso explícito del pipeline de despliegue** (`dotnet ef database update`
  o un contenedor/job dedicado con un usuario con privilegios DDL).
- El usuario de runtime de la app usa un login de **mínimo privilegio** sin DDL.
- Si mantienes la migración en código para entornos pequeños, protégela con un lock distribuido o
  ejecútala solo en una instancia designada.

#### Robustez operativa adicional

- **Health checks** (`AddHealthChecks`) para BD y dependencias externas, expuestos en una ruta interna.
- **`app.UseForwardedHeaders`** correctamente configurado tras el proxy (ver nota de SEC-02).
- **Backups cifrados** de la BD y del key ring, con pruebas de restauración.

---

### Fase 6 — Cumplimiento, privacidad y mejora continua

#### SEC-09 · `AllowedHosts` y construcción de URLs con `Request.Host`

**Riesgo.** `appsettings.json:9` fija `AllowedHosts: "*"`. En `BillingEndpoints.cs:51,87` las URLs de
retorno de Stripe se construyen con `context.Request.Host`. Con hosts sin restringir, una cabecera
`Host` manipulada podría envenenar esas URLs (o las de emails absolutos), habilitando redirecciones
inesperadas. El impacto real es limitado (requiere sesión autenticada y el flujo lo controla Stripe),
pero es una configuración insegura evitable.

**Solución.**
- Restringe `AllowedHosts` a tus dominios reales en producción: `"trackrecord.app;www.trackrecord.app"`.
- Mejor aún, no dependas de `Request.Host`: define una `App:BaseUrl` de configuración y úsala para
  construir `SuccessUrl`/`CancelUrl`/`ReturnUrl` y los enlaces absolutos de email.

```csharp
var baseUrl = configuration["App:BaseUrl"]!; // p. ej. https://trackrecord.app
```

#### SEC-11 · Slug público: enumeración y scraping

**Riesgo.** `PublicProfileService.cs:121` genera slugs de 8 hex (32 bits). Aunque el espacio es grande,
la página `/t/{slug}` es anónima y sin rate limiting, exponiendo métricas agregadas + `DisplayName`.
Es un riesgo bajo (datos que el usuario eligió publicar), pero conviene evitar el scraping masivo.

**Solución.**
- Aplica rate limiting a la ruta pública `/t/{slug}` (política por IP).
- Mantén slugs de al menos 8–12 hex (ya es aceptable) y no correlaciones el slug con el `UserId`.
- Recuerda que `DisplayName` lo introduce el usuario: Razor lo codifica (sin XSS), pero valida longitud
  (ya hay `[StringLength(200)]`) y considera moderación si el perfil es indexable públicamente.

#### SEC-15 · Escaneo de dependencias y actualizaciones

**Riesgo.** No hay comprobación de vulnerabilidades conocidas en los paquetes NuGet (Stripe.net,
Markdig, Microsoft.AspNetCore.*) ni en las librerías de `wwwroot/lib`.

**Solución.**
- En CI: `dotnet list package --vulnerable --include-transitive` como *gate* que falle el build.
- Habilita **Dependabot**/renovate para NuGet y para las librerías front.
- Fija y actualiza periódicamente Bootstrap y demás activos estáticos.

#### Privacidad y datos enviados a terceros

- **Anthropic (Claude).** `ClaudeTradingAnalystService` envía **KPIs agregados**, nunca trades
  individuales (verificado, y así lo documenta el system prompt). Aun así, son datos financieros del
  usuario enviados a un tercero: refléjalo en la política de privacidad, ofrece opt-out y firma el DPA
  correspondiente. El riesgo de *prompt injection* es bajo (la salida solo la ve el propio usuario y el
  markdown va sin HTML), pero mantén el límite de tokens y el control de cupo por plan.
- **Stripe / Google / SMTP.** Documenta subencargados y bases legales (GDPR si aplican usuarios de la UE).
- **Derechos ARCO/GDPR.** Prevé exportación y borrado de cuenta (el borrado en cascada ya existe a nivel
  de `TradingAccount`; añade el borrado completo de usuario, sus `AiReports`, `PublicProfile` y secretos).

#### Pruebas de seguridad continuas

- **SAST**: integra el analizador de seguridad de .NET y reglas de CodeQL en CI.
- **DAST**: pasa OWASP ZAP contra un entorno de staging.
- **Pentest** antes del lanzamiento público y tras cambios mayores de autenticación o pagos.

---

## Checklist de despliegue a producción

Puerta de salida antes de exponer TrackRecord a Internet. No marques “listo” hasta que **todas** las
casillas 🔴/🟠 estén verdes.

**Autenticación y acceso**
- [ ] 🔴 `lockoutOnFailure: true` y lockout configurado (SEC-01).
- [ ] 🔴 Rate limiting activo en login, registro e ingesta (SEC-02, SEC-08).
- [ ] 🟠 Política de contraseñas ≥ 10 caracteres (SEC-03).
- [ ] 🟡 Mensajes de login/registro que no enumeran usuarios (SEC-04).
- [ ] 🟡 Cookies `Secure` + `HttpOnly` + `SameSite` (SEC-14).

**Criptografía y transporte**
- [ ] 🟠 Key ring de Data Protection en almacén compartido y cifrado en reposo (SEC-05).
- [ ] 🟠 `Encrypt=True;TrustServerCertificate=False` hacia SQL con usuario de mínimo privilegio (SEC-06).
- [ ] 🟠 HSTS activo (ya lo está fuera de Development) y HTTPS forzado.
- [ ] 🟠 Secretos (Anthropic, Stripe, Google, SMTP, cadena de BD) inyectados desde gestor de secretos, no en ficheros.

**Perímetro web**
- [ ] 🟠 Cabeceras de seguridad + CSP en enforcing (SEC-07, Fase 4).
- [ ] 🟠 `AllowedHosts` restringido y URLs construidas desde `App:BaseUrl` (SEC-09).
- [ ] 🟡 Rate limiting en `/t/{slug}` (SEC-11).
- [ ] 🟡 Mensajes de error genéricos en UI; detalle solo en logs (SEC-10).

**Operaciones**
- [ ] 🟡 Migraciones ejecutadas como paso de despliegue, no al arrancar (SEC-12).
- [ ] 🟡 Logging de seguridad centralizado con alertas (SEC-13).
- [ ] 🟡 `dotnet list package --vulnerable` en verde en CI; Dependabot activo (SEC-15).
- [ ] 🟡 Health checks, `ForwardedHeaders` y backups cifrados verificados.
- [ ] 🟡 Idempotencia de webhook si se añaden efectos acumulativos (SEC-16).

**Cumplimiento**
- [ ] Política de privacidad con terceros (Anthropic, Stripe, Google) y DPA firmados.
- [ ] Flujo de exportación y borrado total de cuenta.
- [ ] Pentest previo al lanzamiento superado.

---

> **Mantenimiento de esta guía.** Revísala en cada cambio de autenticación, pagos o integraciones
> nuevas, y tras cada auditoría. Actualiza el inventario de hallazgos con su estado (abierto / mitigado
> / aceptado) para que sirva de registro vivo de la postura de seguridad del producto.
