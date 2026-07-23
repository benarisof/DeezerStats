using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.UseCases.Search;
using FluentAssertions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Search
{
    public class SearchCatalogUseCaseTests
    {
        private readonly ISearchEnginePort _searchEnginePort = Substitute.For<ISearchEnginePort>();
        private readonly SearchCatalogUseCase _useCase;

        public SearchCatalogUseCaseTests()
        {
            _useCase = new SearchCatalogUseCase(_searchEnginePort);
        }

        [Fact]
        public async Task ExecuteAsyncWithValidQueryShouldDelegateToSearchEnginePortWithTrimmedQuery()
        {
            // Arrange
            var expected = new SearchResultsPageDto { Items = [new SearchSuggestionDto()], Page = 2, PageSize = 10, TotalItems = 1, TotalPages = 1 };
            _searchEnginePort
                .SearchAsync("Daft Punk", 2, 10, Arg.Any<CancellationToken>())
                .Returns(expected);

            // Act
            SearchResultsPageDto result = await _useCase.ExecuteAsync("  Daft Punk  ", page: 2, pageSize: 10, CancellationToken.None);

            // Assert : la requête brute contient des espaces superflus (comme saisis par un
            // utilisateur) -- le port ne doit recevoir que la version nettoyée.
            result.Should().BeSameAs(expected);
            await _searchEnginePort.Received(1).SearchAsync("Daft Punk", 2, 10, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWithEmptyQueryShouldReturnEmptyResultsWithoutCallingSearchEnginePort()
        {
            // Act
            SearchResultsPageDto result = await _useCase.ExecuteAsync(string.Empty, page: 1, pageSize: 20, CancellationToken.None);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalItems.Should().Be(0);
            result.TotalPages.Should().Be(0);
            await _searchEnginePort.DidNotReceiveWithAnyArgs().SearchAsync(default!, default, default, default);
        }

        [Fact]
        public async Task ExecuteAsyncWithWhitespaceOnlyQueryShouldReturnEmptyResultsWithoutCallingSearchEnginePort()
        {
            // Act
            SearchResultsPageDto result = await _useCase.ExecuteAsync("    ", page: 1, pageSize: 20, CancellationToken.None);

            // Assert
            result.Items.Should().BeEmpty();
            await _searchEnginePort.DidNotReceiveWithAnyArgs().SearchAsync(default!, default, default, default);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task ExecuteAsyncWithPageBelowOneShouldClampToOne(int requestedPage)
        {
            // Act
            await _useCase.ExecuteAsync("Daft Punk", page: requestedPage, pageSize: 20, CancellationToken.None);

            // Assert
            await _searchEnginePort.Received(1).SearchAsync("Daft Punk", 1, 20, Arg.Any<CancellationToken>());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task ExecuteAsyncWithPageSizeBelowOneShouldClampToTwenty(int requestedPageSize)
        {
            // Act
            await _useCase.ExecuteAsync("Daft Punk", page: 1, pageSize: requestedPageSize, CancellationToken.None);

            // Assert
            await _searchEnginePort.Received(1).SearchAsync("Daft Punk", 1, 20, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWithEmptyQueryAndInvalidPagingShouldStillClampPageAndPageSizeInTheEmptyResult()
        {
            // Arrange : le clamp de pagination doit s'appliquer même sur le court-circuit "requête
            // vide", pour que la page renvoyée reste cohérente avec ce que ferait un appel valide.
            // Act
            SearchResultsPageDto result = await _useCase.ExecuteAsync(string.Empty, page: -3, pageSize: -1, CancellationToken.None);

            // Assert
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(20);
        }
    }
}
