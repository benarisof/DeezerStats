using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.UseCases.Users;
using FluentAssertions;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Tests d'intégration bout-en-bout pour POST /api/v1/imports : hébergent la vraie API (voir
/// CustomWebApplicationFactory) et un vrai fichier .xlsx (voir ExcelHistoryFixture), sans mocker ni
/// le parseur Excel, ni les use cases, ni les repositories -- seule la base PostgreSQL est
/// remplacée par le provider EF Core InMemory.
/// </summary>
public class ImportsEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;

    [Fact]
    public async Task ImportWithDuplicatesAndInvalidRowsShouldReturnAccurateReport()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "listener1@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        DateTime listenedAt1 = new(2026, 1, 5, 20, 0, 0, DateTimeKind.Utc);
        DateTime listenedAt2 = new(2026, 1, 6, 21, 0, 0, DateTimeKind.Utc);

        (string, string, string, string, int, DateTime)[] rows =
        [
            ("Blinding Lights", "The Weeknd", "After Hours", "USUM71607007", 200, listenedAt1),

            // Doublon exact du précédent (même ISRC, même date d'écoute), présent deux fois DANS le
            // même fichier -- doit être compté comme "skipped", pas comme une deuxième importation.
            ("Blinding Lights", "The Weeknd", "After Hours", "USUM71607007", 200, listenedAt1),

            ("Save Your Tears", "The Weeknd", "After Hours", "USUM71607008", 215, listenedAt2),

            // ISRC au format invalide -> doit être comptée en erreur, pas en import ni en skip.
            ("Ligne corrompue", "Inconnu", "Inconnu", "PAS-UN-ISRC", 180, listenedAt1),
        ];

        // Act
        using HttpResponseMessage response = await PostImportAsync(client, rows);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ImportReport? report = await response.Content.ReadFromJsonAsync<ImportReport>();
        report.Should().NotBeNull();
        report!.ImportedCount.Should().Be(2, "Blinding Lights et Save Your Tears sont chacune importées une fois");
        report.SkippedCount.Should().Be(1, "le doublon exact dans le fichier");
        report.ErrorCount.Should().Be(1, "l'ISRC invalide");
        report.Errors.Should().ContainSingle(e => e.Message.Contains("ISRC"));

        // Act 2 : réimporter exactement le même fichier -> preuve que la déduplication survit entre
        // deux requêtes distinctes (contrainte d'unicité en base, voir ListeningEventConfiguration),
        // pas seulement à l'intérieur d'un seul fichier.
        using HttpResponseMessage secondResponse = await PostImportAsync(client, rows);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ImportReport? secondReport = await secondResponse.Content.ReadFromJsonAsync<ImportReport>();
        secondReport.Should().NotBeNull();
        secondReport!.ImportedCount.Should().Be(0, "les trois écoutes valides existent déjà en base");
        secondReport.SkippedCount.Should().Be(3);
        secondReport.ErrorCount.Should().Be(1, "l'ISRC invalide est toujours rejetée, indépendamment de la base");
    }

    [Fact]
    public async Task ImportWithUnreadableFileShouldReturn400BadRequest()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "listener2@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent("ceci n'est pas un fichier Excel"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "notes.txt");

        // Act
        using HttpResponseMessage response = await client.PostAsync("/api/v1/imports", content);

        // Assert : "fichier illisible", voir ClosedXmlExcelParser.OpenWorkbook -> DomainException -> 400.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportWithoutTokenShouldReturn401Unauthorized()
    {
        // Arrange : aucune authentification -> le FallbackPolicy global (voir Program.cs) doit
        // rejeter la requête avant même d'atteindre le controller.
        using HttpClient client = _factory.CreateClient();

        (string, string, string, string, int, DateTime)[] rows =
        [
            ("Track", "Artist", "Album", "USUM71607009", 200, DateTime.UtcNow.AddDays(-1)),
        ];

        // Act
        using HttpResponseMessage response = await PostImportAsync(client, rows);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
}
