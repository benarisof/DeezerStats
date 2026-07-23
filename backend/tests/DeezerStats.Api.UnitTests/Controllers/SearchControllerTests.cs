using DeezerStats.Api.Controllers;
using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.UseCases.Search;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeezerStats.Api.UnitTests.Controllers;

public class SearchControllerTests
{
    private readonly IGetSearchSuggestionsUseCase _getSearchSuggestionsUseCase = Substitute.For<IGetSearchSuggestionsUseCase>();
    private readonly ISearchCatalogUseCase _searchCatalogUseCase = Substitute.For<ISearchCatalogUseCase>();
    private readonly SearchController _controller;

    public SearchControllerTests()
    {
        // Contrairement aux autres contrôleurs (voir StatsControllerTests, TracksControllerTests),
        // la recherche n'est pas scopée à l'utilisateur authentifié : aucun ClaimsPrincipal à
        // configurer, ni GetAuthenticatedUserId() appelé par les actions ci-dessous.
        _controller = new SearchController(_getSearchSuggestionsUseCase, _searchCatalogUseCase);
    }

    [Fact]
    public async Task GetSuggestionsShouldReturn200OkWithTheSuggestionsFromTheUseCase()
    {
        // Arrange
        IEnumerable<SearchSuggestionDto> expected = [new SearchSuggestionDto { Label = "Daft Punk" }];
        _getSearchSuggestionsUseCase
            .ExecuteAsync("Daft", Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        IActionResult result = await _controller.GetSuggestions("Daft", CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task SearchShouldReturn200OkWithTheResultsFromTheUseCaseUsingDefaultPaging()
    {
        // Arrange
        var expected = new SearchResultsPageDto { Page = 1, PageSize = 20 };
        _searchCatalogUseCase
            .ExecuteAsync("Daft Punk", 1, 20, Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act : page/pageSize non fournis -> valeurs par défaut du contrôleur (1/20).
        IActionResult result = await _controller.Search("Daft Punk", cancellationToken: CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
        await _searchCatalogUseCase.Received(1).ExecuteAsync("Daft Punk", 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchShouldReturn200OkWithTheRequestedPaging()
    {
        // Arrange
        var expected = new SearchResultsPageDto { Page = 3, PageSize = 10 };
        _searchCatalogUseCase
            .ExecuteAsync("Daft Punk", 3, 10, Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        IActionResult result = await _controller.Search("Daft Punk", page: 3, pageSize: 10, CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
    }
}
