using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using FundedEdge.Application.Abstractions;
using FundedEdge.Infrastructure;
using FundedEdge.Infrastructure.Billing;
using FundedEdge.Infrastructure.Identity;
using FundedEdge.Infrastructure.Persistence;
using FundedEdge.Web.Components;
using FundedEdge.Web.Components.Account;
using FundedEdge.Web.Endpoints;
using FundedEdge.Web.Services;
using FundedEdge.Web.State;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Idiomas: español por defecto, inglés opcional. Claves = texto en español (ver SharedResources);
// el inglés vive en Resources/SharedResources.en.resx.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<CurrencyBroadcaster>();
builder.Services.AddScoped<CurrencyState>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

// Requiere sesión en todas las páginas por defecto — las de /Account (Login, Register...) y las
// públicas bajo [AllowAnonymous] (p.ej. /calculadora) se marcan explícitamente. Más seguro que
// anotar [Authorize] página a página. Excepción explícita para /_blazor/*: es el transporte
// compartido del circuito SignalR (negociación + JS initializers), no una página — si el fallback
// lo bloquea, NINGUNA página anónima con @rendermode InteractiveServer puede volverse interactiva
// (el circuito nunca conecta y el formulario cae a un post-back estático que el servidor rechaza
// con 400). Descubierto al verificar /calculadora (F5.3). La protección de cada página sigue
// vigente: solo se exime el transporte, no las rutas de páginas en sí.
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAssertion(ctx =>
            ctx.User.Identity?.IsAuthenticated == true ||
            (ctx.Resource is HttpContext httpContext && httpContext.Request.Path.StartsWithSegments("/_blazor")))
        .Build());

// Rate limiting [SEC-02, SEC-08, SEC-11]. Se limita solo el tráfico sensible (login/registro
// y perfiles públicos) mediante el GlobalLimiter, que devuelve NoLimiter para el resto — así NO
// se estrangula el transporte SignalR de Blazor (/_blazor) ni la carga de estáticos. Tras un
// proxy inverso, habilita UseForwardedHeaders para que RemoteIpAddress sea la IP real del
// cliente y no la del proxy.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = ctx.Request.Path;

        // Autenticación: ventana estrecha por IP para frenar la fuerza bruta de contraseñas.
        if (HttpMethods.IsPost(ctx.Request.Method) &&
            (path.StartsWithSegments("/Account/Login") ||
             path.StartsWithSegments("/Account/Register") ||
             path.StartsWithSegments("/Account/ResendEmailConfirmation")))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"auth:{ip}",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) });
        }

        // Perfiles públicos de track record (/t/{slug}): frena el scraping/enumeración.
        if (path.StartsWithSegments("/t"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"public:{ip}",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) });
        }

        return RateLimitPartition.GetNoLimiter<string>("noop");
    });
});

// Cookies de sesión seguras en producción [SEC-14]. SameSite=Lax es compatible con el retorno
// del login externo de Google; Secure=Always exige HTTPS (ya forzado por UseHttpsRedirection/HSTS).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    // Always en producción (HTTPS forzado); SameAsRequest en desarrollo para no romper el login
    // sobre http://localhost.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleConfigured = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});
authBuilder.AddIdentityCookies();

if (googleConfigured)
{
    // Nunca en appsettings.json versionado — configura vía User Secrets/entorno (ver README).
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
}

// Nunca en appsettings.json versionado — configura vía User Secrets/entorno (ver README).
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
var stripeWebhookSecret = builder.Configuration["Stripe:WebhookSecret"];
var stripeConfigured = !string.IsNullOrWhiteSpace(stripeSecretKey) && !string.IsNullOrWhiteSpace(stripeWebhookSecret);

var stripePrices = new StripePriceCatalog(
    builder.Configuration["Stripe:Prices:ProMonthly"],
    builder.Configuration["Stripe:Prices:ProYearly"],
    builder.Configuration["Stripe:Prices:EliteMonthly"],
    builder.Configuration["Stripe:Prices:EliteYearly"]);
builder.Services.AddSingleton(new BillingOptions(stripeConfigured, stripeWebhookSecret, stripePrices));
builder.Services.AddScoped<BillingWebhookProcessor>();

if (stripeConfigured)
{
    Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
}

var app = builder.Build();

if (!googleConfigured)
{
    app.Logger.LogInformation(
        "Login con Google desactivado: faltan Authentication:Google:ClientId/ClientSecret. Ver README.");
}

if (!stripeConfigured)
{
    app.Logger.LogInformation(
        "Pagos con Stripe desactivados: faltan Stripe:SecretKey/WebhookSecret. Ver README.");
}

// Aplica migraciones pendientes al arrancar. Cómodo para el MVP local, pero en despliegues con
// varias instancias conviene migrar como paso explícito del pipeline (usuario de BD con DDL) y
// desactivar esto con Database:AutoMigrate=false para evitar carreras al migrar en paralelo. [SEC-12]
var autoMigrate = !string.Equals(app.Configuration["Database:AutoMigrate"], "false", StringComparison.OrdinalIgnoreCase);
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FundedEdgeDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}
else
{
    app.Logger.LogInformation("Migración automática desactivada (Database:AutoMigrate=false). Aplica las migraciones como paso de despliegue.");
}

// Roles (Administrator/Support) y asignación del admin inicial vía Admin:Email. Idempotente.
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>().SeedAsync();
}

// Reglamentos de prop firms: carga por defecto de los .md incluidos (idempotente, no destructivo).
// No es crítico para arrancar, así que un fallo aquí se registra pero no tumba la aplicación.
{
    using var scope = app.Services.CreateScope();
    try
    {
        await scope.ServiceProvider.GetRequiredService<PropFirmRulesSeeder>().SeedAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "No se pudieron cargar los reglamentos de prop firms; se omite (no crítico).");
    }
}

// Datos de demostración (opt-in, ver DemoDataSeeder): usuario demo con 6 cuentas, trades,
// transiciones, resets, payouts y psicología. Solo con Database:SeedDemo=true (Development).
if (string.Equals(app.Configuration["Database:SeedDemo"], "true", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Cabeceras de seguridad [SEC-07]. La CSP es compatible con Blazor Server: el arranque
// (blazor.web.js) y los estilos/JS se sirven desde 'self'; el circuito usa WebSocket a 'self'.
// Bootstrap usa estilos inline, de ahí 'unsafe-inline' en style-src (endurecer con nonce a futuro).
// Para probar sin bloquear, cambia el nombre de la cabecera a "Content-Security-Policy-Report-Only".
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "DENY";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self'; " +
        "connect-src 'self' ws: wss:; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";
    await next();
});

app.UseRateLimiter();

// La cultura se decide por petición (cookie fijada por /culture/set, o Accept-Language del
// navegador) y el circuito de Blazor Server la hereda de la petición HTTP inicial — por eso
// cambiar de idioma requiere una recarga completa (ver CultureSelector.razor).
var supportedCultures = new[] { "es", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
app.MapAdminEndpoints();
app.MapBillingEndpoints();
app.MapSeoEndpoints();

// Cambia el idioma: fija la cookie de cultura y redirige a la página de origen (solo rutas locales).
app.MapGet("/culture/set", (string culture, string redirectUri, HttpContext http) =>
{
    if (supportedCultures.Contains(culture))
    {
        http.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
    }

    if (!redirectUri.StartsWith('/') || redirectUri.StartsWith("//", StringComparison.Ordinal))
    {
        redirectUri = "/";
    }
    return Results.LocalRedirect(redirectUri);
}).AllowAnonymous();

app.Run();
