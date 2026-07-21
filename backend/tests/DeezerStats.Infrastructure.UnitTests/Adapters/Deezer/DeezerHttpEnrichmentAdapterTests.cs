using System.Net;
using System.Text;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Adapters.Deezer;
using FluentAssertions;

namespace DeezerStats.Infrastructure.UnitTests.Adapters.Deezer
{
    public class DeezerHttpEnrichmentAdapterTests
    {
        [Fact]
        public async Task FetchTrackMetadataAsyncWhenTrackFoundShouldReturnMetadata()
        {
            // Arrange : cas "cache miss" côté GetOrEnrichTrackUseCase — Deezer répond avec les
            // métadonnées attendues.
            const string json = """
                {
                    "duration": 243,
                    "album": {
                        "cover_medium": "https://cdn-deezer.com/medium.jpg",
                        "cover_big": "https://cdn-deezer.com/big.jpg",
                        "cover_xl": "https://cdn-deezer.com/xl.jpg"
                    }
                }
                """;
            DeezerHttpEnrichmentAdapter adapter = CreateAdapter(new StubHttpMessageHandler(StubHttpMessageHandler.Json(json)));

            // Act
            DeezerTrackMetadata? result = await adapter.FetchTrackMetadataAsync(new Isrc("USCM51300736"));

            // Assert : la cover_xl (meilleure résolution disponible) est préférée à cover_big/cover_medium.
            result.Should().NotBeNull();
            result!.Duration.TotalSeconds.Should().Be(243);
            result.CoverUrl.Should().Be("https://cdn-deezer.com/xl.jpg");
        }

        [Fact]
        public async Task FetchTrackMetadataAsyncWhenDeezerReturnsErrorBodyShouldReturnNull()
        {
            // Arrange : Deezer renvoie un HTTP 200 avec un corps d'erreur pour un ISRC inconnu
            // (comportement documenté de l'API publique), et non un 404.
            const string json = """{"error": {"type": "DataException", "message": "no data", "code": 800}}""";
            DeezerHttpEnrichmentAdapter adapter = CreateAdapter(new StubHttpMessageHandler(StubHttpMessageHandler.Json(json)));

            // Act
            DeezerTrackMetadata? result = await adapter.FetchTrackMetadataAsync(new Isrc("USCM51300736"));

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task FetchTrackMetadataAsyncWhenHttpCallFailsShouldReturnNullWithoutThrowing()
        {
            // Arrange : simule une panne réseau / indisponibilité de l'API Deezer (le HttpClient
            // typé a déjà épuisé ses tentatives de la politique de résilience — voir
            // Infrastructure.DependencyInjection). L'adaptateur doit absorber l'échec plutôt que de
            // le laisser remonter, pour ne jamais faire échouer l'import ou la consultation associée.
            DeezerHttpEnrichmentAdapter adapter = CreateAdapter(
                new StubHttpMessageHandler(StubHttpMessageHandler.Throwing(new HttpRequestException("Deezer indisponible."))));

            // Act
            DeezerTrackMetadata? result = await adapter.FetchTrackMetadataAsync(new Isrc("USCM51300736"));

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task FetchAlbumMetadataAsyncWhenAlbumFoundShouldReturnMetadata()
        {
            // Arrange : deux appels HTTP successifs (recherche puis détails — voir
            // DeezerHttpEnrichmentAdapter.FetchAlbumMetadataAsync), consommés dans l'ordre.
            const string searchJson = """{"data": [{"id": 302127}], "total": 1}""";
            const string detailsJson = """
                {
                    "cover_medium": "https://cdn-deezer.com/medium.jpg",
                    "cover_xl": "https://cdn-deezer.com/xl.jpg",
                    "release_date": "2013-05-17",
                    "duration": 2694
                }
                """;
            DeezerHttpEnrichmentAdapter adapter = CreateAdapter(new StubHttpMessageHandler(
                StubHttpMessageHandler.Json(searchJson),
                StubHttpMessageHandler.Json(detailsJson)));

            // Act
            DeezerAlbumMetadata? result = await adapter.FetchAlbumMetadataAsync("Random Access Memories", "Daft Punk");

            // Assert
            result.Should().NotBeNull();
            result!.CoverUrl.Should().Be("https://cdn-deezer.com/xl.jpg");
            result.ReleaseDate.Should().Be(new DateOnly(2013, 5, 17));
            result.Duration!.TotalSeconds.Should().Be(2694);
        }

        [Fact]
        public async Task FetchAlbumMetadataAsyncWhenNoSearchResultsShouldReturnNull()
        {
            // Arrange
            const string searchJson = """{"data": [], "total": 0}""";
            DeezerHttpEnrichmentAdapter adapter = CreateAdapter(new StubHttpMessageHandler(StubHttpMessageHandler.Json(searchJson)));

            // Act
            DeezerAlbumMetadata? result = await adapter.FetchAlbumMetadataAsync("Album Inconnu", "Artiste Inconnu");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task FetchAlbumMetadataAsyncWhenHttpCallFailsShouldReturnNullWithoutThrowing()
        {
            // Arrange
            DeezerHttpEnrichmentAdapter adapter = CreateAdapter(
                new StubHttpMessageHandler(StubHttpMessageHandler.Throwing(new HttpRequestException("Deezer indisponible."))));

            // Act
            DeezerAlbumMetadata? result = await adapter.FetchAlbumMetadataAsync("Random Access Memories", "Daft Punk");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task FetchArtistMetadataAsyncWhenArtistFoundShouldReturnMetadata()
        {
            // Arrange : contrairement à la recherche d'album, un seul appel HTTP suffit — les
            // photos sont déjà présentes dans les résultats de recherche d'artiste.
            const string searchJson = """
                {
                    "data": [
                        {
                            "picture_medium": "https://cdn-deezer.com/medium.jpg",
                            "picture_big": "https://cdn-deezer.com/big.jpg",
                            "picture_xl": "https://cdn-deezer.com/xl.jpg"
                        }
                    ]
                }
                """;
            DeezerHttpEnrichmentAdapter adapter = CreateAdapter(new StubHttpMessageHandler(StubHttpMessageHandler.Json(searchJson)));

            // Act
            DeezerArtistMetadata? result = await adapter.FetchArtistMetadataAsync("Daft Punk");

            // Assert
            result.Should().NotBeNull();
            result!.CoverUrl.Should().Be("https://cdn-deezer.com/xl.jpg");
        }

        [Fact]
        public async Task FetchArtistMetadataAsyncWhenNoSearchResultsShouldReturnNull()
        {
            // Arrange
            const string searchJson = """{"data": [], "total": 0}""";
            DeezerHttpEnrichmentAdapter adapter = CreateAdapter(new StubHttpMessageHandler(StubHttpMessageHandler.Json(searchJson)));

            // Act
            DeezerArtistMetadata? result = await adapter.FetchArtistMetadataAsync("Artiste Inconnu");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task FetchArtistMetadataAsyncWhenHttpCallFailsShouldReturnNullWithoutThrowing()
        {
            // Arrange
            DeezerHttpEnrichmentAdapter adapter = CreateAdapter(
                new StubHttpMessageHandler(StubHttpMessageHandler.Throwing(new HttpRequestException("Deezer indisponible."))));

            // Act
            DeezerArtistMetadata? result = await adapter.FetchArtistMetadataAsync("Daft Punk");

            // Assert
            result.Should().BeNull();
        }

        private static DeezerHttpEnrichmentAdapter CreateAdapter(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.deezer.com/") };
            return new DeezerHttpEnrichmentAdapter(httpClient);
        }

        /// <summary>
        /// Handler HTTP de test : rejoue, dans l'ordre, une réponse (ou une exception) par appel à
        /// <c>SendAsync</c>. Permet de tester DeezerHttpEnrichmentAdapter sans appel réseau réel ni
        /// dépendance de mocking supplémentaire (System.Net.Http n'expose pas SendAsync comme
        /// substituable autrement qu'en héritant de HttpMessageHandler).
        /// </summary>
        private sealed class StubHttpMessageHandler(params Func<HttpResponseMessage>[] responders) : HttpMessageHandler
        {
            private readonly Queue<Func<HttpResponseMessage>> _responders = new(responders);

            public static Func<HttpResponseMessage> Json(string json) => () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            public static Func<HttpResponseMessage> Throwing(Exception exception) => () => throw exception;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(_responders.Dequeue()());
        }
    }
}
