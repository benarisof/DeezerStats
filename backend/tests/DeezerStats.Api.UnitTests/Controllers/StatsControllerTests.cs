using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DeezerStats.Api.Controllers;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats.Home;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeezerStats.Api.UnitTests.Controllers;

public class StatsControllerTests
{
    private readonly IGetHomeStatsUseCase _useCase = Substitute.For<IGetHomeStatsUseCase>();

    [Fact]
    public async Task GetHomeStatsShouldReturn200OkWithTheAuthenticatedUserIdAndDateRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 6, 30);
        var expected = new HomeStatsResponse([], [], []);

        _useCase
            .ExecuteAsync(
                Arg.Is<GetHomeStatsQuery>(q => q != null && q.UserId == userId && q.From == from && q.To == to),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        StatsController controller = CreateControllerForUser(userId);

        // Act
        IActionResult result = await controller.GetHomeStats(from, to, CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
    }

    private StatsController CreateControllerForUser(Guid userId)
    {
        Claim[] claims = [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

        return new StatsController(_useCase)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            },
        };
    }
}
