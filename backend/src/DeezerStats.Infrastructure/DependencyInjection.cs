using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Infrastructure.Adapters.Catalog;
using DeezerStats.Infrastructure.Adapters.Deezer;
using DeezerStats.Infrastructure.Adapters.Excel;
using DeezerStats.Infrastructure.Adapters.Search;
using DeezerStats.Infrastructure.Adapters.Security;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Queries;
using DeezerStats.Infrastructure.Persistence.Repositories;
using Meilisearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace DeezerStats.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // PostgreSQL via Npgsql
            var connectionString = configuration.GetConnectionString("Default");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Repositories
            services.AddScoped<ITrackRepository, TrackRepository>();
            services.AddScoped<IAlbumRepository, AlbumRepository>();
            services.AddScoped<IArtistRepository, ArtistRepository>();
            services.AddScoped<IListeningEventRepository, ListeningEventRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Unit of Work : permet aux cas d'usage multi-agrégats
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Phase 9 — port de lecture des statistiques d'écoute (stats, tops, historique, item)
            services.AddScoped<IListeningStatsQueryPort, ListeningStatsQueryService>();

            // Adapteurs
            services.AddScoped<IExcelParserPort, ClosedXmlExcelParser>();
            services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

            // Adaptateur Deezer : HttpClient typé + résilience (retry/timeout) via Polly. Le
            // timeout par tentative et le nombre de tentatives sont volontairement plus courts que
            // les défauts d'AddStandardResilienceHandler, car l'enrichissement se fait à la demande
            // au fil des requêtes HTTP (voir CatalogEnrichmentCoordinator, GetAlbumDetailUseCase...)
            // et ne doit donc pas monopoliser un thread trop longtemps sur une API tierce.
            services.AddHttpClient<IDeezerEnrichmentPort, DeezerHttpEnrichmentAdapter>(client =>
            {
                client.BaseAddress = new Uri("https://api.deezer.com/");
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
            });

            // Enrichissement parallèle à concurrence bornée pour les listes (top albums/artistes/
            // morceaux, accueil) : Singleton car sans état propre, il ne dépend que d'IServiceScopeFactory
            // (lui-même Singleton) pour créer un scope isolé par élément enrichi (voir
            // CatalogEnrichmentCoordinator).
            services.AddSingleton<ICatalogEnrichmentCoordinator, CatalogEnrichmentCoordinator>();

            // Configuration de JwtSettings
            services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();
            services.AddOptions<JwtSettings>()
                .Bind(configuration.GetSection(JwtSettings.SectionName))
                .ValidateOnStart();
            services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();
            services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();

            // Enregistrement des Options
            services.Configure<MeilisearchOptions>(configuration.GetSection(MeilisearchOptions.SectionName));

            // Enregistrement du Client Meilisearch en Singleton
            services.AddSingleton(sp =>
            {
                MeilisearchOptions options = configuration.GetSection(MeilisearchOptions.SectionName).Get<MeilisearchOptions>()
                    ?? throw new InvalidOperationException($"La configuration '{MeilisearchOptions.SectionName}' est introuvable ou mal formatée.");
                return new MeilisearchClient(options.Url, options.MasterKey);
            });

            // Enregistrement de l'Adaptateur
            services.AddScoped<ISearchEnginePort, MeilisearchAdapter>();

            // Enregistrement du service d'initialisation au démarrage
            services.AddHostedService<MeilisearchInitializerService>();

            return services;
        }
    }
}
