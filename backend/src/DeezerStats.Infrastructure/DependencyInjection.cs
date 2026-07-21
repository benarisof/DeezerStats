using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Infrastructure.Adapters.Excel;
using DeezerStats.Infrastructure.Adapters.Security;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
