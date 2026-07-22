using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.UnitTests.Aggregates
{
    public class RefreshTokenTests
    {
        [Fact]
        public void ConstructorShouldCreateActiveToken()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            DateTime expiresAt = DateTime.UtcNow.AddDays(30);

            // Act
            var token = new RefreshToken(id, userId, "hashed-token", expiresAt);

            // Assert
            Assert.Equal(id, token.Id);
            Assert.Equal(userId, token.UserId);
            Assert.Equal("hashed-token", token.TokenHash);
            Assert.Equal(expiresAt, token.ExpiresAt);
            Assert.False(token.IsExpired);
            Assert.False(token.IsRevoked);
            Assert.True(token.IsActive);
            Assert.Null(token.RevokedAt);
        }

        [Fact]
        public void ConstructorShouldRejectEmptyUserId()
        {
            // Fonction locale
            RefreshToken CreateToken() => new(
                Guid.NewGuid(),
                Guid.Empty,
                "hashed-token",
                DateTime.UtcNow.AddDays(30));

            // Act & Assert
            DomainException exception = Assert.Throws<DomainException>(CreateToken);
            Assert.Equal(
                "Un refresh token doit être rattaché à un utilisateur.",
                exception.Message);
        }

        [Fact]
        public void ConstructorShouldRejectEmptyTokenHash()
        {
            // Fonction locale
            RefreshToken CreateToken() => new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                string.Empty,
                DateTime.UtcNow.AddDays(30));

            // Act & Assert
            DomainException exception = Assert.Throws<DomainException>(CreateToken);
            Assert.Equal(
                "Le hash du refresh token ne peut pas être vide.",
                exception.Message);
        }

        [Fact]
        public void IsExpiredShouldBeTrueWhenExpiresAtIsInThePast()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddDays(-1));

            // Assert
            Assert.True(token.IsExpired);
            Assert.False(token.IsActive);
        }

        [Fact]
        public void RevokeShouldMarkTokenAsRevokedAndInactive()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddDays(30));

            // Act
            token.Revoke();

            // Assert
            Assert.True(token.IsRevoked);
            Assert.False(token.IsActive);
            Assert.NotNull(token.RevokedAt);
        }

        [Fact]
        public void RevokeShouldSetReplacedByTokenIdWhenProvided()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddDays(30));
            var replacementId = Guid.NewGuid();

            // Act
            token.Revoke(replacementId);

            // Assert
            Assert.Equal(replacementId, token.ReplacedByTokenId);
        }

        [Fact]
        public void RevokeShouldBeIdempotentAndKeepOriginalRevokedAt()
        {
            // Arrange
            var token = new RefreshToken(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "hashed-token",
                DateTime.UtcNow.AddDays(30));

            token.Revoke();
            DateTime? firstRevokedAt = token.RevokedAt;

            // Act : seconde révocation, ne doit rien changer.
            token.Revoke(Guid.NewGuid());

            // Assert
            Assert.Equal(firstRevokedAt, token.RevokedAt);
            Assert.Null(token.ReplacedByTokenId);
        }
    }
}
