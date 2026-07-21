using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DeezerStats.Api.Controllers;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeezerStats.Api.UnitTests.Controllers;

public class AlbumsControllerTests
{
    private readonly IGetTopAlbumsUseCase _getTopAlbumsUseCase = Substitute.For<IGetTopAlbumsUseCase>();
    private readonly IGetAlbumDetailUseCase _getAlbumDetailUseCase = Substitute.For<IGetAlbumDetailUseCase>();

    [Fact]
    public async Task GetTopAlbumsShouldReturn200OkWithTheAuthenticatedUserIdAndDefaultPaging()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new PagedResult<AlbumSummary>([], 1, 20, 0);

        _getTopAlbumsUseCase
            .ExecuteAsync(
                Arg.Is<GetTopAlbumsQuery>(q => q != null && q.UserId == userId && q.Page == 1 && q.PageSize == 20),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        AlbumsController controller = CreateControllerForUser(userId);

        // Act
        IActionResult result = await controller.GetTopAlbums(null, null, page: 1, pageSize: 20);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetAlbumDetailWhenFoundShouldReturn200OkWithTheDetail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        var expected = new AlbumDetail(albumId, "Discovery", Guid.NewGuid(), "Daft Punk", null, null, null, 1.0, 5, []);

        _getAlbumDetailUseCase
            .ExecuteAsync(
                Arg.Is<GetAlbumDetailQuery>(q => q != null && q.UserId == userId && q.AlbumId == albumId),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        AlbumsController controller = CreateControllerForUser(userId);

        // Act
        IActionResult result = await controller.GetAlbumDetail(albumId, null, null, CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetAlbumDetailWhenNotFoundShouldReturn404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var albumId = Guid.NewGuid();

        _getAlbumDetailUseCase
            .ExecuteAsync(Arg.Any<GetAlbumDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns((AlbumDetail?)null);

        AlbumsController controller = CreateControllerForUser(userId);

        // Act
        IActionResult result = await controller.GetAlbumDetail(albumId, null, null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    private AlbumsController CreateControllerForUser(Guid userId)
    {
        Claim[] claims = [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

        return new AlbumsController(_getTopAlbumsUseCase, _getAlbumDetailUseCase)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            },
        };
    }
}
