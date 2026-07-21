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

            // Adapteurs
            services.AddScoped<IExcelParserPort, ClosedXmlExcelParser>();
            services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

            // Configuration de JwtSettings
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();

            return services;
        }
    }
}
