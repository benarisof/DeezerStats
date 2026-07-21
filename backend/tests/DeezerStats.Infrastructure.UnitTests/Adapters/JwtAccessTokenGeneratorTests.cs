using System.IdentityModel.Tokens.Jwt;
using DeezerStats.Application.DTOs;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Adapters.Security;
using Microsoft.Extensions.Options;

namespace DeezerStats.Infrastructure.UnitTests.Adapters
{
    public class JwtAccessTokenGeneratorTests
    {
        private readonly JwtAccessTokenGenerator _tokenGenerator;

        private readonly JwtSettings _jwtSettings = new()
        {
            Key = "this-is-a-very-long-secret-key-for-tests-123456",
            Issuer = "DeezerStats.Api",
            Audience = "DeezerStats.Client",
            ExpirationInMinutes = 60,
        };

        public JwtAccessTokenGeneratorTests()
        {
            _tokenGenerator = new JwtAccessTokenGenerator(
                Options.Create(_jwtSettings));
        }

        [Fact]
        public void GenerateShouldReturnAnAccessToken()
        {
            // Arrange
            User user = CreateUser();

            // Act
            AccessTokenDto accessToken = _tokenGenerator.Generate(user);

            // Assert
            Assert.NotNull(accessToken);
            Assert.False(string.IsNullOrWhiteSpace(accessToken.Token));
            Assert.True(accessToken.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public void GenerateShouldIncludeUserIdAsSubjectClaim()
        {
            // Arrange
            User user = CreateUser();

            // Act
            AccessTokenDto accessToken = _tokenGenerator.Generate(user);

            JwtSecurityToken jwt = ReadToken(accessToken.Token);

            // Assert
            var subject = jwt.Claims
                .First(x => x.Type == JwtRegisteredClaimNames.Sub)
                .Value;

            Assert.Equal(
                user.Id.ToString(),
                subject);
        }

        [Fact]
        public void GenerateShouldIncludeUserEmailClaim()
        {
            // Arrange
            User user = CreateUser();

            // Act
            AccessTokenDto accessToken = _tokenGenerator.Generate(user);

            JwtSecurityToken jwt = ReadToken(accessToken.Token);

            // Assert
            var email = jwt.Claims
                .First(x => x.Type == JwtRegisteredClaimNames.Email)
                .Value;

            Assert.Equal(
                user.Email.Value,
                email);
        }

        [Fact]
        public void GenerateShouldIncludeIssuer()
        {
            // Arrange
            User user = CreateUser();

            // Act
            AccessTokenDto accessToken = _tokenGenerator.Generate(user);

            JwtSecurityToken jwt = ReadToken(accessToken.Token);

            // Assert
            Assert.Equal(
                _jwtSettings.Issuer,
                jwt.Issuer);
        }

        [Fact]
        public void GenerateShouldIncludeAudience()
        {
            // Arrange
            User user = CreateUser();

            // Act
            AccessTokenDto accessToken = _tokenGenerator.Generate(user);

            JwtSecurityToken jwt = ReadToken(accessToken.Token);

            // Assert
            Assert.Contains(
                _jwtSettings.Audience,
                jwt.Audiences);
        }

        [Fact]
        public void GenerateShouldGenerateDifferentTokensForSameUser()
        {
            // Arrange
            User user = CreateUser();

            // Act
            AccessTokenDto firstToken = _tokenGenerator.Generate(user);
            AccessTokenDto secondToken = _tokenGenerator.Generate(user);

            // Assert
            Assert.NotEqual(
                firstToken.Token,
                secondToken.Token);
        }

        private static User CreateUser()
        {
            return new User(
                Guid.NewGuid(),
                new Email("user@test.com"),
                "hashed-password",
                "Sofiane");
        }

        private static JwtSecurityToken ReadToken(string token)
        {
            return new JwtSecurityTokenHandler()
                .ReadJwtToken(token);
        }
    }
}
