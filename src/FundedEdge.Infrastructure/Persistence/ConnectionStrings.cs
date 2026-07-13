namespace FundedEdge.Infrastructure.Persistence;

public static class ConnectionStrings
{
    /// <summary>
    /// Cadena de conexión por defecto para PostgreSQL en local. Ajustar en
    /// appsettings.json si el servidor usa otro host, puerto o credenciales.
    /// </summary>
    public const string DefaultLocalExpress =
        "Host=localhost;Port=5432;Database=FundedEdge;Username=postgres;Password=postgres";
}
