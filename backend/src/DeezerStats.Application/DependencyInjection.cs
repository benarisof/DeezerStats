using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Application.UseCases.Import;
using DeezerStats.Application.UseCases.Stats;
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

            // Phase 9 — Endpoints de consultation (stats, tops, historique, item)
            services.AddScoped<IGetHomeStatsUseCase, GetHomeStatsUseCase>();
            services.AddScoped<IGetTopAlbumsUseCase, GetTopAlbumsUseCase>();
            services.AddScoped<IGetTopArtistsUseCase, GetTopArtistsUseCase>();
            services.AddScoped<IGetTopTracksUseCase, GetTopTracksUseCase>();
            services.AddScoped<IGetHistoryUseCase, GetHistoryUseCase>();
            services.AddScoped<IGetAlbumDetailUseCase, GetAlbumDetailUseCase>();
            services.AddScoped<IGetArtistDetailUseCase, GetArtistDetailUseCase>();

            return services;
        }
    }
}
