using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DeezerStats.Api.Controllers;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats.Artist;
using DeezerStats.Application.UseCases.Stats.TopArtists;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeezerStats.Api.UnitTests.Controllers;

public class ArtistsControllerTests
{
    private readonly IGetTopArtistsUseCase _getTopArtistsUseCase = Substitute.For<IGetTopArtistsUseCase>();
    private readonly IGetArtistDetailUseCase _getArtistDetailUseCase = Substitute.For<IGetArtistDetailUseCase>();

    [Fact]
    public async Task GetTopArtistsShouldReturn200OkWithTheAuthenticatedUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new PagedResult<ArtistSummary>([], 1, 20, 0);

        _getTopArtistsUseCase
            .ExecuteAsync(
                Arg.Is<GetTopArtistsQuery>(q => q != null && q.UserId == userId),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        ArtistsController controller = CreateControllerForUser(userId);

        // Act
        IActionResult result = await controller.GetTopArtists(null, null, page: 1, pageSize: 20);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetArtistDetailWhenFoundShouldReturn200OkWithTheDetail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var expected = new ArtistDetail(artistId, "Daft Punk", null, 1, 3, 2.0, 8, []);

        _getArtistDetailUseCase
            .ExecuteAsync(
                Arg.Is<GetArtistDetailQuery>(q => q != null && q.UserId == userId && q.ArtistId == artistId),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        ArtistsController controller = CreateControllerForUser(userId);

        // Act
        IActionResult result = await controller.GetArtistDetail(artistId, null, null, CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetArtistDetailWhenNotFoundShouldReturn404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var artistId = Guid.NewGuid();

        _getArtistDetailUseCase
            .ExecuteAsync(Arg.Any<GetArtistDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns((ArtistDetail?)null);

        ArtistsController controller = CreateControllerForUser(userId);

        // Act
        IActionResult result = await controller.GetArtistDetail(artistId, null, null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    private ArtistsController CreateControllerForUser(Guid userId)
    {
        Claim[] claims = [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

        return new ArtistsController(_getTopArtistsUseCase, _getArtistDetailUseCase)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            },
        };
    }
}
