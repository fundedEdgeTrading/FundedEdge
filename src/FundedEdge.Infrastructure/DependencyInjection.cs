using Anthropic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FundedEdge.Application.Abstractions;
using FundedEdge.Application.Ai;
using FundedEdge.Domain.Common;
using FundedEdge.Infrastructure.Ai;
using FundedEdge.Infrastructure.Email;
using FundedEdge.Infrastructure.Identity;
using FundedEdge.Infrastructure.Integrations.Csv;
using FundedEdge.Infrastructure.Persistence;
using FundedEdge.Infrastructure.Services;
using FundedEdge.Infrastructure.Settings;

namespace FundedEdge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? ConnectionStrings.DefaultLocalExpress;
        connectionString = NormalizePostgresConnectionString(connectionString);

        services.AddDbContextFactory<FundedEdgeDbContext>(options =>
            options.UseNpgsql(connectionString, sql => sql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName)));

        // ASP.NET Core Identity necesita poder inyectar un FundedEdgeDbContext scoped "normal"
        // (sus stores no conocen IDbContextFactory); el resto de la app sigue usando el factory
        // para crear contextos de vida corta por operación.
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<FundedEdgeDbContext>>().CreateDbContext());

        // Key ring persistido en disco: sin esto, los valores guardados en IntegrationSettings
        // (cifrados con IDataProtector — hoy solo preferencias de UI como la divisa) dejarían de
        // poder descifrarse tras reiniciar la app si el key ring por defecto no fuera estable.
        // En Render el disco del contenedor es efímero: se define DataProtection:KeysPath
        // (env var DataProtection__KeysPath) apuntando a un disco persistente; en local, si no se
        // configura, se usa LocalApplicationData.
        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath))
        {
            keysPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FundedEdge", "keys");
        }
        Directory.CreateDirectory(keysPath);
        services.AddDataProtection()
            .SetApplicationName("FundedEdge")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        // Roles habilitados (AddRoles): solo para jerarquía operativa (Administrator/Support,
        // ver AppRoles) — cada usuario sigue viendo exclusivamente sus propios datos.
        // RequireConfirmedAccount=true: nadie inicia sesión
        // sin confirmar su email (frena registros de bots). El envío real depende de EmailOptions
        // (ver AddEmail); sin SMTP configurado el enlace se muestra en pantalla (solo desarrollo).
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;

                // Lockout explícito (no depender de los defaults): la comprobación real la activa
                // el login con lockoutOnFailure:true (ver Login.razor). Frena la fuerza bruta de
                // contraseñas contra una cuenta conocida. [SEC-01]
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // Política de contraseñas robusta (el default es longitud 6). [SEC-03]
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false; // frases largas > símbolos obligatorios
                options.Password.RequiredUniqueChars = 4;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<FundedEdgeDbContext>()
            .AddSignInManager()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        services.AddScoped<UserBackfillService>();
        services.AddScoped<IdentityDataSeeder>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<DemoDataSeeder>();

        services.AddScoped<IPropFirmService, PropFirmService>();
        services.AddScoped<IEvaluationProgramService, EvaluationProgramService>();
        services.AddScoped<IAccountProgressService, AccountProgressService>();
        services.AddScoped<IExternalFirmDataProvider, ManualExternalFirmDataProvider>();
        services.AddScoped<ITradingAccountService, TradingAccountService>();
        services.AddScoped<ITradeSetupService, TradeSetupService>();
        services.AddScoped<IKpiService, KpiService>();
        services.AddScoped<ICsvTradeImportService, CsvTradeImportService>();
        services.AddScoped<IGenericCsvImportService, Integrations.GenericCsv.GenericCsvImportService>();
        services.AddScoped<IIntegrationSettingsStore, DataProtectedIntegrationSettingsStore>();
        services.AddScoped<IRiskAnalysisService, RiskAnalysisService>();
        services.AddScoped<IFirmFitService, FirmFitService>();
        services.AddScoped<ICurrencyPreferenceService, CurrencyPreferenceService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IPublicProfileService, PublicProfileService>();
        services.AddScoped<IPeerDiscoveryService, PeerDiscoveryService>();
        services.AddScoped<IPsychologyService, PsychologyService>();
        services.AddScoped<IRuleComplianceService, RuleComplianceService>();

        AddAi(services, configuration);
        AddEmail(services, configuration);

        return services;
    }

    // Render (y otros PaaS) entregan la cadena de conexión en formato URI
    // postgresql://usuario:password@host[:puerto]/basedatos, pero Npgsql espera formato clave=valor.
    // Si detectamos el esquema URI lo convertimos; si ya viene en clave=valor lo dejamos igual.
    private static string NormalizePostgresConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        // La conexión interna de Render (host tipo dpg-...-a, sin dominio) no requiere SSL; la
        // externa (host con dominio, p. ej. ...-a.frankfurt-postgres.render.com) sí.
        var ssl = uri.Host.Contains('.') ? "Ssl Mode=Require;Trust Server Certificate=true;" : string.Empty;
        return $"Host={uri.Host};Port={port};Database={database};" +
               $"Username={username};Password={password};{ssl}";
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        // Nunca en appsettings.json versionado — configura vía User Secrets/entorno (ver README).
        var host = configuration["Email:SmtpHost"];
        var from = configuration["Email:From"];
        var isConfigured = !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(from);

        services.AddSingleton(new EmailOptions(
            isConfigured,
            host,
            int.TryParse(configuration["Email:SmtpPort"], out var port) ? port : 587,
            configuration["Email:SmtpUser"],
            configuration["Email:SmtpPassword"],
            from,
            configuration["Email:FromName"] ?? Brand.Name));

        if (isConfigured)
        {
            services.AddScoped<IAppEmailSender, SmtpAppEmailSender>();
            services.AddScoped<IEmailSender<ApplicationUser>, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IAppEmailSender, NoOpAppEmailSender>();
            services.AddScoped<IEmailSender<ApplicationUser>, NoOpEmailSender>();
        }
    }

    private static void AddAi(IServiceCollection services, IConfiguration configuration)
    {
        // Preferimos ANTHROPIC_API_KEY (leída automáticamente por el SDK); "Ai:ApiKey" en
        // appsettings.Development.json/user-secrets es un fallback explícito para desarrollo.
        // Nunca se lee de appsettings.json versionado. Ver README.md.
        var apiKey = configuration["Ai:ApiKey"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        var isConfigured = !string.IsNullOrWhiteSpace(apiKey);

        services.AddSingleton(new AiOptions(isConfigured));
        services.AddSingleton(_ => string.IsNullOrWhiteSpace(apiKey)
            ? new AnthropicClient()
            : new AnthropicClient { ApiKey = apiKey });

        services.AddScoped<ITradingAnalystService, ClaudeTradingAnalystService>();
        services.AddHostedService<WeeklyAiReportService>();
        services.AddHostedService<ProactiveInsightService>();
    }
}
