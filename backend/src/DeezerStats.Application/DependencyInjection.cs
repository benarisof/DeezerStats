using DeezerStats.Application.Common;
using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Application.UseCases.Import;
using DeezerStats.Application.UseCases.Search;
using DeezerStats.Application.UseCases.Stats.Album;
using DeezerStats.Application.UseCases.Stats.Artist;
using DeezerStats.Application.UseCases.Stats.History;
using DeezerStats.Application.UseCases.Stats.Home;
using DeezerStats.Application.UseCases.Stats.TopAlbums;
using DeezerStats.Application.UseCases.Stats.TopArtists;
using DeezerStats.Application.UseCases.Stats.TopTracks;
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
            services.AddScoped<IAuthTokenIssuer, AuthTokenIssuer>();
            services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
            services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
            services.AddScoped<IRefreshAccessTokenUseCase, RefreshAccessTokenUseCase>();
            services.AddScoped<ILogoutUserUseCase, LogoutUserUseCase>();
            services.AddScoped<IGetCurrentUserUseCase, GetCurrentUserUseCase>();
            services.AddScoped<IImportListeningHistoryUseCase, ImportListeningHistoryUseCase>();
            services.AddScoped<IGetOrEnrichTrackUseCase, GetOrEnrichTrackUseCase>();
            services.AddScoped<IGetOrEnrichAlbumUseCase, GetOrEnrichAlbumUseCase>();
            services.AddScoped<IGetOrEnrichArtistUseCase, GetOrEnrichArtistUseCase>();
            services.AddScoped<IGetHomeStatsUseCase, GetHomeStatsUseCase>();
            services.AddScoped<IGetTopAlbumsUseCase, GetTopAlbumsUseCase>();
            services.AddScoped<IGetTopArtistsUseCase, GetTopArtistsUseCase>();
            services.AddScoped<IGetTopTracksUseCase, GetTopTracksUseCase>();
            services.AddScoped<IGetHistoryUseCase, GetHistoryUseCase>();
            services.AddScoped<IGetAlbumDetailUseCase, GetAlbumDetailUseCase>();
            services.AddScoped<IGetArtistDetailUseCase, GetArtistDetailUseCase>();
            services.AddScoped<IGetSearchSuggestionsUseCase, GetSearchSuggestionsUseCase>();
            services.AddScoped<ISearchCatalogUseCase, SearchCatalogUseCase>();
            return services;
        }
    }
}
