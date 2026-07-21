using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.BackgroundJobs;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Infrastructure.Adapters.Deezer;
using DeezerStats.Infrastructure.Adapters.Excel;
using DeezerStats.Infrastructure.Adapters.Security;
using DeezerStats.Infrastructure.BackgroundJobs;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Repositories;
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

            // Unit of Work : permet aux cas d'usage multi-agrégats
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Adapteurs
            services.AddScoped<IExcelParserPort, ClosedXmlExcelParser>();
            services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

            // Adaptateur Deezer : HttpClient typé + résilience (retry/timeout) via Polly. Le
            // timeout par tentative et le nombre de tentatives sont volontairement plus courts que
            // les défauts d'AddStandardResilienceHandler, car l'enrichissement est un traitement
            // d'arrière-plan (voir EnrichmentBackgroundService) qui ne doit pas monopoliser un
            // thread trop longtemps sur une API tierce.
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

            // Enrichissement en tâche de fond après import (voir ImportListeningHistoryUseCase et
            // le contrat OpenAPI de POST /imports) : la file est Singleton pour être partagée entre
            // les requêtes HTTP (producteurs, Scoped) et le service d'arrière-plan (consommateur).
            services.AddSingleton<EnrichmentJobChannel>();
            services.AddSingleton<IEnrichmentJobScheduler>(sp => sp.GetRequiredService<EnrichmentJobChannel>());
            services.AddSingleton<IEnrichmentJobReader>(sp => sp.GetRequiredService<EnrichmentJobChannel>());
            services.AddHostedService<EnrichmentBackgroundService>();

            // Configuration de JwtSettings
            services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();
            services.AddOptions<JwtSettings>()
                .Bind(configuration.GetSection(JwtSettings.SectionName))
                .ValidateOnStart();
            services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();

            return services;
        }
    }
}
