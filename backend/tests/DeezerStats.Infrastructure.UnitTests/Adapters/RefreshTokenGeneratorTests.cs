using DeezerStats.Infrastructure.Adapters.Security;

namespace DeezerStats.Infrastructure.UnitTests.Adapters
{
    public class RefreshTokenGeneratorTests
    {
        private readonly RefreshTokenGenerator _generator = new();

        [Fact]
        public void GenerateTokenShouldProduceDifferentValuesOnEachCall()
        {
            // Act
            var first = _generator.GenerateToken();
            var second = _generator.GenerateToken();

            // Assert
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void HashShouldBeDeterministicForTheSameToken()
        {
            // Arrange
            var token = _generator.GenerateToken();

            // Act
            var firstHash = _generator.Hash(token);
            var secondHash = _generator.Hash(token);

            // Assert : contrairement au hash de mot de passe (BCrypt, salé), le hash d'un refresh
            // token doit être déterministe pour permettre une recherche par égalité exacte en base.
            Assert.Equal(firstHash, secondHash);
        }

        [Fact]
        public void HashShouldProduceDifferentValuesForDifferentTokens()
        {
            // Act
            var firstHash = _generator.Hash("token-a");
            var secondHash = _generator.Hash("token-b");

            // Assert
            Assert.NotEqual(firstHash, secondHash);
        }

        [Fact]
        public void HashShouldReturnAValueDifferentFromTheRawToken()
        {
            // Arrange
            var token = _generator.GenerateToken();

            // Act
            var hash = _generator.Hash(token);

            // Assert
            Assert.NotEqual(token, hash);
        }
    }
}
