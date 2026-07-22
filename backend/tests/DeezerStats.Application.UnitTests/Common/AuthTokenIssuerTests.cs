using DeezerStats.Application.Common;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
using Moq;

namespace DeezerStats.Application.UnitTests.Common
{
    public class AuthTokenIssuerTests
    {
        private readonly Mock<IAccessTokenGenerator> _accessTokenGeneratorMock;
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;

        private readonly AuthTokenIssuer _issuer;

        public AuthTokenIssuerTests()
        {
            _accessTokenGeneratorMock = new Mock<IAccessTokenGenerator>();
            _refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();

            _issuer = new AuthTokenIssuer(
                _accessTokenGeneratorMock.Object,
                _refreshTokenGeneratorMock.Object,
                _refreshTokenRepositoryMock.Object);
        }

        [Fact]
        public async Task IssueAsyncShouldReturnRawRefreshTokenAndPersistItsHash()
        {
            // Arrange
            var user = new User(Guid.NewGuid(), new Email("user@test.com"), "hash", "Sofiane");
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);

            _accessTokenGeneratorMock
                .Setup(x => x.Generate(user))
                .Returns(new AccessTokenDto("jwt-access-token", accessTokenExpiresAt));

            _refreshTokenGeneratorMock
                .Setup(x => x.GenerateToken())
                .Returns("raw-refresh-token");

            _refreshTokenGeneratorMock
                .Setup(x => x.Hash("raw-refresh-token"))
                .Returns("hashed-refresh-token");

            // Act
            AuthTokensDto result = await _issuer.IssueAsync(user);

            // Assert : la valeur brute est retournée au client, jamais le hash.
            Assert.Equal("jwt-access-token", result.AccessToken);
            Assert.Equal("raw-refresh-token", result.RefreshToken);
            Assert.InRange(result.ExpiresInSeconds, 890, 900);

            _refreshTokenRepositoryMock.Verify(
                x => x.AddAsync(
                    It.Is<RefreshToken>(t => t.UserId == user.Id && t.TokenHash == "hashed-refresh-token"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
