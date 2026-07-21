using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DeezerStats.Api.Controllers;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.UseCases.Imports;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeezerStats.Api.UnitTests.Controllers;

public class ImportsControllerTests
{
    private readonly IImportListeningHistoryUseCase _useCaseMock = Substitute.For<IImportListeningHistoryUseCase>();

    [Fact]
    public async Task ImportShouldReturn200OkWithReportAndTheAuthenticatedUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedReport = new ImportReport(10, 2, 0, []);

        _useCaseMock
            .ExecuteAsync(
                Arg.Is<ImportListeningHistoryCommand>(c => c != null && c.UserId == userId),
                Arg.Any<CancellationToken>())
            .Returns(expectedReport);

        ImportsController controller = CreateControllerForUser(userId);

        IFormFile file = CreateFormFile("historique.xlsx", "contenu factice");

        // Act
        IActionResult result = await controller.Import(file, CancellationToken.None);

        // Assert : l'UserId transmis au use case vient bien du claim "sub" du token JWT, pas d'un
        // paramètre manipulable par le client (voir ImportsController.GetAuthenticatedUserId).
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedReport);

        await _useCaseMock.Received(1).ExecuteAsync(
            Arg.Is<ImportListeningHistoryCommand>(c => c != null && c.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportWhenTokenHasNoSubjectClaimShouldThrowInvalidOperationException()
    {
        // Arrange : ne devrait jamais se produire derrière le FallbackPolicy (voir Program.cs), mais
        // documente l'invariant plutôt que de laisser fuiter une FormatException opaque.
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        ImportsController controller = new(_useCaseMock)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            },
        };

        IFormFile file = CreateFormFile("historique.xlsx", "contenu factice");

        // Act
        Func<Task> act = () => controller.Import(file, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static IFormFile CreateFormFile(string fileName, string content)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        IFormFile file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.Length.Returns(stream.Length);
        file.OpenReadStream().Returns(stream);

        return file;
    }

    private ImportsController CreateControllerForUser(Guid userId)
    {
        Claim[] claims = [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

        return new ImportsController(_useCaseMock)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            },
        };
    }
}
