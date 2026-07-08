using Anthropic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrackRecord.Application.Abstractions;
using TrackRecord.Application.Ai;
using TrackRecord.Domain.Common;
using TrackRecord.Infrastructure.Ai;
using TrackRecord.Infrastructure.Email;
using TrackRecord.Infrastructure.Identity;
using TrackRecord.Infrastructure.Integrations.NinjaTrader;
using TrackRecord.Infrastructure.Integrations.Tradovate;
using TrackRecord.Infrastructure.Persistence;
using TrackRecord.Infrastructure.Services;
using TrackRecord.Infrastructure.Settings;

namespace TrackRecord.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? ConnectionStrings.DefaultLocalExpress;

        services.AddDbContextFactory<TrackRecordDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName)));

        // ASP.NET Core Identity necesita poder inyectar un TrackRecordDbContext scoped "normal"
        // (sus stores no conocen IDbContextFactory); el resto de la app sigue usando el factory
        // para crear contextos de vida corta por operación.
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<TrackRecordDbContext>>().CreateDbContext());

        // Key ring persistido en disco: sin esto, los secretos guardados desde /settings
        // (IntegrationSettings, cifrados con IDataProtector) dejarían de poder descifrarse tras
        // reiniciar la app si el key ring por defecto no fuera estable en este entorno.
        var keysPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrackRecord", "keys");
        services.AddDataProtection()
            .SetApplicationName("TrackRecord")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        // Sin roles (AddIdentityCore, no AddIdentity): cada usuario ve exclusivamente sus propios
        // datos, no hay jerarquía de permisos. RequireConfirmedAccount=true: nadie inicia sesión
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
            .AddEntityFrameworkStores<TrackRecordDbContext>()
            .AddSignInManager()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        services.AddScoped<UserBackfillService>();

        services.AddScoped<IPropFirmService, PropFirmService>();
        services.AddScoped<IEvaluationProgramService, EvaluationProgramService>();
        services.AddScoped<IAccountProgressService, AccountProgressService>();
        services.AddScoped<IExternalFirmDataProvider, ManualExternalFirmDataProvider>();
        services.AddScoped<ITradingAccountService, TradingAccountService>();
        services.AddScoped<IKpiService, KpiService>();
        services.AddScoped<ITradeRebuildService, TradeRebuildService>();
        services.AddScoped<IExecutionIngestService, ExecutionIngestService>();
        services.AddScoped<ICsvTradeImportService, CsvTradeImportService>();
        services.AddScoped<ITradeSyncOrchestrator, TradeSyncOrchestrator>();
        services.AddScoped<IIntegrationSettingsStore, DataProtectedIntegrationSettingsStore>();
        services.AddScoped<IIntegrationSettingsService, IntegrationSettingsService>();
        services.AddScoped<IRiskAnalysisService, RiskAnalysisService>();
        services.AddScoped<IFirmFitService, FirmFitService>();
        services.AddScoped<ICurrencyPreferenceService, CurrencyPreferenceService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IPublicProfileService, PublicProfileService>();
        services.AddHostedService<TradeSyncService>();

        AddAi(services, configuration);
        AddTradovate(services, configuration);
        AddEmail(services, configuration);

        return services;
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

    private static void AddTradovate(IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Tradovate:BaseUrl"] ?? "https://live.tradovateapi.com/v1";

        services.AddScoped<ConfigurationTradovateCredentialStore>();
        services.AddScoped<DbTradovateCredentialStore>();
        services.AddScoped<ITradovateCredentialStore, TradovateCredentialStore>();
        services.AddTransient<TradovateAuthHandler>();

        // Cliente "en crudo" sin el auth handler adjunto: lo usa TradovateAuthHandler para
        // autenticar/renovar sin recursión sobre el propio pipeline.
        services.AddHttpClient("TradovateAuthRaw", c => c.BaseAddress = new Uri(baseUrl));

        services.AddHttpClient<ITradovateClient, TradovateClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<TradovateAuthHandler>()
            .AddStandardResilienceHandler();
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
    }
}
