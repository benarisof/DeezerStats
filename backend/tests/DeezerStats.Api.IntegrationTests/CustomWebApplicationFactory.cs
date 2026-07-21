using DeezerStats.Application.Ports.ExternalServices.Deezer;
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
/// validation des options au démarrage...) pour les tests d'intégration, en substituant :
/// - la base PostgreSQL par le provider EF Core InMemory (une instance nommée avec un GUID par
///   factory garantit qu'aucun test ne peut affecter les autres) ;
/// - le véritable adaptateur Deezer (HttpClient vers l'API publique, voir
///   DeezerHttpEnrichmentAdapter) par un faux (voir FakeDeezerEnrichmentPort), pour qu'un import
///   déclenchant un enrichissement en tâche de fond (voir EnrichmentBackgroundService) ne dépende
///   jamais d'un appel réseau sortant réel pendant les tests.
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

            services.RemoveAll<IDeezerEnrichmentPort>();
            services.AddSingleton<IDeezerEnrichmentPort, FakeDeezerEnrichmentPort>();
        });
    }
}
