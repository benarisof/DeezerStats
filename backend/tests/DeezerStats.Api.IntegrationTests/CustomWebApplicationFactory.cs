using DeezerStats.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Héberge l'API réelle (Program.cs tel quel : middleware, authentification JWT, FallbackPolicy,
/// validation des options au démarrage...) pour les tests d'intégration, en substituant uniquement
/// la base PostgreSQL par le provider EF Core InMemory. Une instance de base nommée avec un GUID par
/// factory garantit qu'aucun test ne peut affecter les autres.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"IntegrationTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
