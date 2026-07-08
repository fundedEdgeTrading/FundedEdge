using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TrackRecord.Infrastructure.Persistence;

/// <summary>
/// Permite ejecutar "dotnet ef migrations add" directamente sobre este proyecto,
/// sin necesitar levantar TrackRecord.Web. La cadena de conexión real en tiempo de
/// ejecución la aporta TrackRecord.Web vía appsettings.json / DependencyInjection.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrackRecordDbContext>
{
    public TrackRecordDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TrackRecordDbContext>();
        optionsBuilder.UseSqlServer(ConnectionStrings.DefaultLocalExpress);
        return new TrackRecordDbContext(optionsBuilder.Options);
    }
}
