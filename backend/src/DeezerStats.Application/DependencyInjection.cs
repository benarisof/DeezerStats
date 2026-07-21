using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Application.UseCases.Imports;
using DeezerStats.Application.UseCases.Tracks;
using DeezerStats.Application.UseCases.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DeezerStats.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
            services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
            services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
            services.AddScoped<IImportListeningHistoryUseCase, ImportListeningHistoryUseCase>();
            services.AddScoped<IGetOrEnrichTrackUseCase, GetOrEnrichTrackUseCase>();
            services.AddScoped<IGetOrEnrichAlbumUseCase, GetOrEnrichAlbumUseCase>();
            services.AddScoped<IGetOrEnrichArtistUseCase, GetOrEnrichArtistUseCase>();

            return services;
        }
    }
}
