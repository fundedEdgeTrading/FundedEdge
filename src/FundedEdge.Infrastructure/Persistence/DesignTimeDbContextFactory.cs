using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FundedEdge.Infrastructure.Persistence;

/// <summary>
/// Permite ejecutar "dotnet ef migrations add" directamente sobre este proyecto,
/// sin necesitar levantar FundedEdge.Web. La cadena de conexión real en tiempo de
/// ejecución la aporta FundedEdge.Web vía appsettings.json / DependencyInjection.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FundedEdgeDbContext>
{
    public FundedEdgeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FundedEdgeDbContext>();
        optionsBuilder.UseNpgsql(ConnectionStrings.DefaultLocalExpress);
        return new FundedEdgeDbContext(optionsBuilder.Options);
    }
}
