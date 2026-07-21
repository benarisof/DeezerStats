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

public class HistoryControllerTests
{
    private readonly IGetHistoryUseCase _useCase = Substitute.For<IGetHistoryUseCase>();

    [Fact]
    public async Task GetHistoryShouldReturn200OkWithTheAuthenticatedUserIdAndDateRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 6, 30);
        var expected = new PagedResult<HistoryEntry>([], 1, 20, 0);

        _useCase
            .ExecuteAsync(
                Arg.Is<GetHistoryQuery>(q => q != null && q.UserId == userId && q.From == from && q.To == to),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        HistoryController controller = CreateControllerForUser(userId);

        // Act
        IActionResult result = await controller.GetHistory(from, to, page: 1, pageSize: 20);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
    }

    private HistoryController CreateControllerForUser(Guid userId)
    {
        Claim[] claims = [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

        return new HistoryController(_useCase)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            },
        };
    }
}
