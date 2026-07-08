namespace TrackRecord.Infrastructure.Persistence;

public static class ConnectionStrings
{
    /// <summary>
    /// Cadena de conexión por defecto para SQL Server Express en local (instancia con
    /// nombre por defecto de la instalación "SQLEXPRESS"). Autenticación integrada de
    /// Windows; ajustar en appsettings.json si la instancia tiene otro nombre o se usa
    /// autenticación SQL.
    /// </summary>
    public const string DefaultLocalExpress =
        "Server=localhost\\SQLEXPRESS;Database=TrackRecord;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
}
