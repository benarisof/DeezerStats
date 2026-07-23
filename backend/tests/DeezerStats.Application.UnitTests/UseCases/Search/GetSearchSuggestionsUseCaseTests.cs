using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.UseCases.Search;
using FluentAssertions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Search
{
    public class GetSearchSuggestionsUseCaseTests
    {
        private readonly ISearchEnginePort _searchEnginePort = Substitute.For<ISearchEnginePort>();
        private readonly GetSearchSuggestionsUseCase _useCase;

        public GetSearchSuggestionsUseCaseTests()
        {
            _useCase = new GetSearchSuggestionsUseCase(_searchEnginePort);
        }

        [Fact]
        public async Task ExecuteAsyncWithAtLeastFourCharactersShouldDelegateToSearchEnginePortWithTrimmedQuery()
        {
            // Arrange
            IEnumerable<SearchSuggestionDto> expected = [new SearchSuggestionDto()];
            _searchEnginePort
                .GetSuggestionsAsync("Daft", Arg.Any<CancellationToken>())
                .Returns(expected);

            // Act
            IEnumerable<SearchSuggestionDto> result = await _useCase.ExecuteAsync("  Daft  ", CancellationToken.None);

            // Assert : la requête brute contient des espaces superflus -- le port ne doit recevoir
            // que la version nettoyée.
            result.Should().BeSameAs(expected);
            await _searchEnginePort.Received(1).GetSuggestionsAsync("Daft", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWithExactlyFourCharactersShouldDelegateToSearchEnginePort()
        {
            // Arrange : cas limite de la règle métier ("au moins 4 caractères", voir
            // GetSearchSuggestionsUseCase et le contrat OpenAPI) -- 4 doit déclencher la recherche,
            // pas seulement 5+.
            _searchEnginePort
                .GetSuggestionsAsync("SCH1", Arg.Any<CancellationToken>())
                .Returns([]);

            // Act
            await _useCase.ExecuteAsync("SCH1", CancellationToken.None);

            // Assert
            await _searchEnginePort.Received(1).GetSuggestionsAsync("SCH1", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWithFewerThanFourCharactersShouldReturnEmptyWithoutCallingSearchEnginePort()
        {
            // Act
            IEnumerable<SearchSuggestionDto> result = await _useCase.ExecuteAsync("SCH", CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
            await _searchEnginePort.DidNotReceiveWithAnyArgs().GetSuggestionsAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncWithEmptyQueryShouldReturnEmptyWithoutCallingSearchEnginePort()
        {
            // Act
            IEnumerable<SearchSuggestionDto> result = await _useCase.ExecuteAsync(string.Empty, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
            await _searchEnginePort.DidNotReceiveWithAnyArgs().GetSuggestionsAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncWithWhitespaceOnlyQueryShouldReturnEmptyWithoutCallingSearchEnginePort()
        {
            // Act
            IEnumerable<SearchSuggestionDto> result = await _useCase.ExecuteAsync("     ", CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
            await _searchEnginePort.DidNotReceiveWithAnyArgs().GetSuggestionsAsync(default!, default);
        }
    }
}
