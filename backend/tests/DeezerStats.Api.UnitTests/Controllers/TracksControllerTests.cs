using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DeezerStats.Api.Controllers;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats.TopTracks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeezerStats.Api.UnitTests.Controllers;

public class TracksControllerTests
{
    private readonly IGetTopTracksUseCase _useCase = Substitute.For<IGetTopTracksUseCase>();

    [Fact]
    public async Task GetTopTracksShouldReturn200OkWithTheAuthenticatedUserIdAndRequestedPaging()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expected = new PagedResult<TrackSummary>([], 3, 10, 25);

        _useCase
            .ExecuteAsync(
                Arg.Is<GetTopTracksQuery>(q => q != null && q.UserId == userId && q.Page == 3 && q.PageSize == 10),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        TracksController controller = CreateControllerForUser(userId);

        // Act
        IActionResult result = await controller.GetTopTracks(null, null, page: 3, pageSize: 10);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
    }

    private TracksController CreateControllerForUser(Guid userId)
    {
        Claim[] claims = [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

        return new TracksController(_useCase)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            },
        };
    }
}
