using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Users;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Tests d'intégration bout-en-bout pour les endpoints de consultation de la Phase 9 (stats,
/// tops, historique, item album/artiste) : hébergent la vraie API (voir CustomWebApplicationFactory)
/// et exercent un vrai import Excel avant d'interroger les endpoints, sans mocker les use cases ni
/// le port de requêtes -- seule la base PostgreSQL est remplacée par le provider EF Core InMemory.
/// </summary>
public class StatsEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;

    [Fact]
    public async Task ImportThenQueryAllConsultationEndpointsShouldReturnConsistentStats()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "listener-stats@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        DateTime t1 = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime t2 = new(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);
        DateTime t3 = new(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc);
        DateTime t4 = new(2026, 1, 4, 10, 0, 0, DateTimeKind.Utc);

        (string, string, string, string, int, DateTime)[] rows =
        [
            ("Blinding Lights", "The Weeknd", "After Hours", "USUM71607007", 200, t1),
            ("Blinding Lights", "The Weeknd", "After Hours", "USUM71607007", 200, t2),
            ("Save Your Tears", "The Weeknd", "After Hours", "USUM71607008", 215, t3),
            ("One More Time", "Daft Punk", "Discovery", "FRZ039900212", 320, t4),
        ];

        using HttpResponseMessage importResponse = await PostImportAsync(client, rows);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act + Assert : /stats/home
        using HttpResponseMessage homeResponse = await client.GetAsync("/api/v1/stats/home");
        homeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        HomeStatsResponse? home = await homeResponse.Content.ReadFromJsonAsync<HomeStatsResponse>();
        home.Should().NotBeNull();
        home!.TopTracks.Should().Contain(t => t.Title == "Blinding Lights" && t.PlayCount == 2);

        // Act + Assert : /albums/top -> After Hours (3 écoutes cumulées) avant Discovery (1 écoute).
        using HttpResponseMessage topAlbumsResponse = await client.GetAsync("/api/v1/albums/top");
        topAlbumsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<AlbumSummary>? topAlbums = await topAlbumsResponse.Content.ReadFromJsonAsync<PagedResult<AlbumSummary>>();
        topAlbums.Should().NotBeNull();
        topAlbums!.Items.Select(a => a.Title).Should().ContainInOrder("After Hours", "Discovery");
        AlbumSummary afterHours = topAlbums.Items.Single(a => a.Title == "After Hours");
        afterHours.PlayCount.Should().Be(3);

        // Act + Assert : /artists/top -> The Weeknd (3) avant Daft Punk (1).
        using HttpResponseMessage topArtistsResponse = await client.GetAsync("/api/v1/artists/top");
        PagedResult<ArtistSummary>? topArtists = await topArtistsResponse.Content.ReadFromJsonAsync<PagedResult<ArtistSummary>>();
        topArtists.Should().NotBeNull();
        topArtists!.Items.Select(a => a.Name).Should().ContainInOrder("The Weeknd", "Daft Punk");
        ArtistSummary theWeeknd = topArtists.Items.Single(a => a.Name == "The Weeknd");

        // Act + Assert : /tracks/top -> Blinding Lights (2) en tête.
        using HttpResponseMessage topTracksResponse = await client.GetAsync("/api/v1/tracks/top");
        PagedResult<TrackSummary>? topTracks = await topTracksResponse.Content.ReadFromJsonAsync<PagedResult<TrackSummary>>();
        topTracks.Should().NotBeNull();
        topTracks!.Items[0].Title.Should().Be("Blinding Lights");
        topTracks.Items[0].PlayCount.Should().Be(2);

        // Act + Assert : /history -> triée du plus récent au plus ancien (4 événements).
        using HttpResponseMessage historyResponse = await client.GetAsync("/api/v1/history");
        PagedResult<HistoryEntry>? history = await historyResponse.Content.ReadFromJsonAsync<PagedResult<HistoryEntry>>();
        history.Should().NotBeNull();
        history!.TotalItems.Should().Be(4);
        history.Items.Select(h => h.ListenedAt).Should().BeInDescendingOrder();
        history.Items[0].Title.Should().Be("One More Time");

        // Act + Assert : /albums/{id} -> détail de "After Hours" (2 morceaux, 3 écoutes cumulées).
        using HttpResponseMessage albumDetailResponse = await client.GetAsync($"/api/v1/albums/{afterHours.Id}");
        albumDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AlbumDetail? albumDetail = await albumDetailResponse.Content.ReadFromJsonAsync<AlbumDetail>();
        albumDetail.Should().NotBeNull();
        albumDetail!.TotalPlayCount.Should().Be(3);
        albumDetail.Tracks.Should().HaveCount(2);
        albumDetail.Tracks[0].Title.Should().Be("Blinding Lights");

        // Act + Assert : /artists/{id} -> détail de "The Weeknd" (1 album distinct écouté, 2 morceaux distincts).
        using HttpResponseMessage artistDetailResponse = await client.GetAsync($"/api/v1/artists/{theWeeknd.Id}");
        artistDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ArtistDetail? artistDetail = await artistDetailResponse.Content.ReadFromJsonAsync<ArtistDetail>();
        artistDetail.Should().NotBeNull();
        artistDetail!.DistinctAlbumsCount.Should().Be(1);
        artistDetail.DistinctTracksCount.Should().Be(2);
        artistDetail.TotalPlayCount.Should().Be(3);

        // Act + Assert : album inconnu -> 404.
        using HttpResponseMessage notFoundResponse = await client.GetAsync($"/api/v1/albums/{Guid.NewGuid()}");
        notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConsultationEndpointsWithoutTokenShouldReturn401Unauthorized()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();

        // Act
        using HttpResponseMessage response = await client.GetAsync("/api/v1/stats/home");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TopAlbumsWithLargeVolumeShouldCapAtBusinessRuleLimitAndRespondWithinBudget()
    {
        // Arrange : contourne l'import Excel (trop lent pour 150 lignes) en insérant directement en
        // base, comme le ferait une volumétrie réelle -- voir ticket 9.6 "tests d'intégration et de
        // perf".
        using HttpClient client = _factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "poweruser@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Guid userId = ExtractUserIdFromToken(token);

        await SeedLargeListeningHistoryAsync(userId, albumCount: 150);

        var stopwatch = Stopwatch.StartNew();

        // Act
        using HttpResponseMessage response = await client.GetAsync("/api/v1/albums/top?pageSize=100");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<AlbumSummary>? page = await response.Content.ReadFromJsonAsync<PagedResult<AlbumSummary>>();
        page.Should().NotBeNull();
        page!.TotalItems.Should().Be(100, "le classement est plafonné à StatsRules.MaxRankedResults malgré 150 albums en base");
        page.Items.Should().HaveCount(100);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5), "une requête de classement ne doit pas dégénérer même avec un volume réaliste");

        // Act 2 : pagination par défaut (20) sur ce même volume.
        using HttpResponseMessage defaultPageResponse = await client.GetAsync("/api/v1/albums/top");
        PagedResult<AlbumSummary>? defaultPage = await defaultPageResponse.Content.ReadFromJsonAsync<PagedResult<AlbumSummary>>();

        // Assert
        defaultPage.Should().NotBeNull();
        defaultPage!.Items.Should().HaveCount(20);
        defaultPage.TotalPages.Should().Be(5);
    }

    private static Guid ExtractUserIdFromToken(string token)
    {
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var subject = jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        return Guid.Parse(subject);
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

        AccessTokenDto? token = await loginResponse.Content.ReadFromJsonAsync<AccessTokenDto>();
        token.Should().NotBeNull();

        return token!.Token;
    }

    private async Task SeedLargeListeningHistoryAsync(Guid userId, int albumCount)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var artist = new Artist(Guid.NewGuid(), "Prolific Artist");
        context.Artists.Add(artist);

        DateTime baseline = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < albumCount; i++)
        {
            var album = new Album(Guid.NewGuid(), $"Perf Album {i:D3}", artist.Id);
            var track = new Track(Guid.NewGuid(), new Isrc($"USUM7{5000000 + i}"), $"Perf Track {i:D3}", artist.Id, album.Id);
            context.Albums.Add(album);
            context.Tracks.Add(track);

            // Une seule écoute par morceau suffit ici : seuls le volume total (150 > 100) et le
            // comportement de plafonnement/pagination sont vérifiés, pas un ordre de classement
            // précis (qui serait arbitraire en cas d'égalité de PlayCount).
            context.ListeningEvents.Add(new ListeningEvent(
                Guid.NewGuid(), userId, track.Id, new Duration(200), baseline.AddMinutes(albumCount - i)));
        }

        await context.SaveChangesAsync();
    }
}
