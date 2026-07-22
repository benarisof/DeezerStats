using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.UseCases.Users;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Tests d'intégration bout-en-bout qui prouvent, à travers la vraie API (voir
/// CustomWebApplicationFactory), les deux garanties introduites par le découplage import/
/// enrichissement (voir ImportListeningHistoryUseCase et GetAlbumDetailUseCase) :
/// - la recherche fonctionne immédiatement après un import, sans qu'aucune page de détail n'ait
///   jamais été consultée (l'indexation est couplée à l'import, pas à l'enrichissement) ;
/// - une couverture Deezer n'apparaît qu'à la première consultation du détail d'un album (l'import
///   n'enrichit plus rien).
/// </summary>
public class SearchAndEnrichmentEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;

    [Fact]
    public async Task ImportThenSearchWithoutVisitingAnyDetailPageShouldFindTheNewCatalogEntities()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "searcher@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        (string, string, string, string, int, DateTime)[] rows =
        [
            ("One More Time", "Daft Punk", "Discovery", "FRZ039900212", 320, DateTime.UtcNow.AddHours(-1)),
        ];

        // Act : import, puis recherche directe -- aucun GET /albums/{id} ni /artists/{id} entre les deux.
        using HttpResponseMessage importResponse = await PostImportAsync(client, rows);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using HttpResponseMessage albumSearchResponse = await client.GetAsync("/api/v1/search?q=Discovery");
        using HttpResponseMessage artistSearchResponse = await client.GetAsync("/api/v1/search?q=Daft%20Punk");
        using HttpResponseMessage trackSearchResponse = await client.GetAsync("/api/v1/search?q=One%20More%20Time");

        // Assert : le nouvel artiste, son nouvel album ET son nouveau morceau sont bien trouvables --
        // voir ImportListeningHistoryUseCase.IndexNewCatalogEntitiesAsync, appelé pour chaque import.
        albumSearchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        SearchResultsPageDto? albumResults = await albumSearchResponse.Content.ReadFromJsonAsync<SearchResultsPageDto>();
        albumResults.Should().NotBeNull();
        albumResults!.Items.Should().ContainSingle(i => i.Type == "album" && i.Label == "Discovery" && i.Subtitle == "Daft Punk");

        SearchResultsPageDto? artistResults = await artistSearchResponse.Content.ReadFromJsonAsync<SearchResultsPageDto>();
        artistResults.Should().NotBeNull();
        artistResults!.Items.Should().ContainSingle(i => i.Type == "artist" && i.Label == "Daft Punk");

        SearchResultsPageDto? trackResults = await trackSearchResponse.Content.ReadFromJsonAsync<SearchResultsPageDto>();
        trackResults.Should().NotBeNull();
        trackResults!.Items.Should().ContainSingle(i => i.Type == "track" && i.Label == "One More Time");
    }

    [Fact]
    public async Task FirstAlbumDetailConsultationShouldEnrichTheCoverThatImportNeverSet()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "enricher@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Simule une réponse Deezer réelle pour cet album précis -- par défaut, FakeDeezerEnrichmentPort
        // ne renvoie jamais de métadonnée (voir son commentaire), ce qui empêcherait de prouver
        // qu'une cover apparaît : on configure donc l'instance Singleton résolue depuis le conteneur
        // DI du factory, avant d'appeler l'endpoint.
        var fakeDeezerPort = (FakeDeezerEnrichmentPort)_factory.Services.GetRequiredService<IDeezerEnrichmentPort>();
        fakeDeezerPort.AlbumMetadataFactory = (albumTitle, artistName) =>
            albumTitle == "Discovery" && artistName == "Daft Punk"
                ? new DeezerAlbumMetadata("https://cdn-images.dzcdn.net/images/cover/fake/1000x1000.jpg", new DateOnly(2001, 3, 7), new Duration(3600))
                : null;

        (string, string, string, string, int, DateTime)[] rows =
        [
            ("One More Time", "Daft Punk", "Discovery", "FRZ039900212", 320, DateTime.UtcNow.AddHours(-1)),
        ];

        using HttpResponseMessage importResponse = await PostImportAsync(client, rows);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert 1 : avant toute consultation de page (détail OU liste -- voir GetTopAlbumsUseCase,
        // qui enrichit lui aussi à la demande), l'album n'a pas de cover en base : ImportListeningHistoryUseCase
        // n'enrichit plus rien à l'import. Lu directement en base plutôt que via /albums/top, car cet
        // endpoint déclencherait lui-même l'enrichissement (CatalogEnrichmentCoordinator) et fausserait
        // ce contrôle "avant".
        Album discovery = await GetAlbumFromDatabaseAsync("Discovery");
        discovery.CoverUrl.Should().BeNull();

        // Act : la première consultation du détail déclenche l'enrichissement à la demande (voir
        // GetAlbumDetailUseCase -> GetOrEnrichAlbumUseCase).
        using HttpResponseMessage albumDetailResponse = await client.GetAsync($"/api/v1/albums/{discovery.Id}");

        // Assert 2
        albumDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AlbumDetail? albumDetail = await albumDetailResponse.Content.ReadFromJsonAsync<AlbumDetail>();
        albumDetail.Should().NotBeNull();
        albumDetail!.CoverUrl.Should().Be("https://cdn-images.dzcdn.net/images/cover/fake/1000x1000.jpg");
    }

    private static async Task<HttpResponseMessage> PostImportAsync(
        HttpClient client,
        IReadOnlyList<(string TrackTitle, string ArtistName, string AlbumTitle, string Isrc, int DurationInSeconds, DateTime ListenedAt)> rows)
    {
        using MemoryStream excelStream = ExcelHistoryFixture.Build(rows);

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(excelStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "historique.xlsx");

        return await client.PostAsync("/api/v1/imports", content);
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string email)
    {
        var registerCommand = new RegisterUserCommand(email, "StrongPass123!", "Test Listener");
        using HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);
        registerResponse.EnsureSuccessStatusCode();

        var loginCommand = new AuthenticateUserCommand(email, "StrongPass123!");
        using HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        loginResponse.EnsureSuccessStatusCode();

        AuthTokensDto? tokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensDto>();
        tokens.Should().NotBeNull();

        return tokens!.AccessToken;
    }

    private async Task<Album> GetAlbumFromDatabaseAsync(string title)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Albums.SingleAsync(a => a.Title == title);
    }
}
