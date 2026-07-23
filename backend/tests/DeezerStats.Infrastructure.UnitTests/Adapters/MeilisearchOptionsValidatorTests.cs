using DeezerStats.Infrastructure.Adapters.Search;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DeezerStats.Infrastructure.UnitTests.Adapters
{
    public class MeilisearchOptionsValidatorTests
    {
        private readonly MeilisearchOptionsValidator _validator = new();

        [Fact]
        public void ValidateWithACompleteConfigurationShouldSucceed()
        {
            // Arrange
            var options = new MeilisearchOptions
            {
                Url = "http://meilisearch:7700",
                MasterKey = "a-real-master-key",
                IndexName = "catalog",
            };

            // Act
            ValidateOptionsResult result = _validator.Validate(null, options);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public void ValidateWithMissingUrlShouldFail()
        {
            // Arrange
            var options = new MeilisearchOptions
            {
                Url = string.Empty,
                MasterKey = "a-real-master-key",
                IndexName = "catalog",
            };

            // Act
            ValidateOptionsResult result = _validator.Validate(null, options);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().ContainSingle(f => f.Contains("Meilisearch:Url"));
        }

        [Fact]
        public void ValidateWithMissingMasterKeyShouldFail()
        {
            // Arrange : ne teste PAS la valeur placeholder de développement elle-même (voir
            // Program.cs, qui la refuse hors Development) -- uniquement l'absence structurelle,
            // seule chose que ce validateur peut vérifier sans connaître l'environnement d'exécution.
            var options = new MeilisearchOptions
            {
                Url = "http://meilisearch:7700",
                MasterKey = "   ",
                IndexName = "catalog",
            };

            // Act
            ValidateOptionsResult result = _validator.Validate(null, options);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().ContainSingle(f => f.Contains("Meilisearch:MasterKey"));
        }

        [Fact]
        public void ValidateWithMissingIndexNameShouldFail()
        {
            // Arrange
            var options = new MeilisearchOptions
            {
                Url = "http://meilisearch:7700",
                MasterKey = "a-real-master-key",
                IndexName = string.Empty,
            };

            // Act
            ValidateOptionsResult result = _validator.Validate(null, options);

            // Assert
            result.Failed.Should().BeTrue();
            result.Failures.Should().ContainSingle(f => f.Contains("Meilisearch:IndexName"));
        }

        [Fact]
        public void ValidateWithAllFieldsMissingShouldReportEveryFailure()
        {
            // Arrange
            var options = new MeilisearchOptions
            {
                Url = string.Empty,
                MasterKey = string.Empty,
                IndexName = string.Empty,
            };

            // Act
            ValidateOptionsResult result = _validator.Validate(null, options);

            // Assert
            result.Failures.Should().HaveCount(3);
        }
    }
}
