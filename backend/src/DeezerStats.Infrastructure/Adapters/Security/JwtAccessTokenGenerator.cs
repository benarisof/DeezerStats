using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DeezerStats.Infrastructure.Adapters.Security
{
    public class JwtAccessTokenGenerator(
        IOptions<JwtSettings> jwtSettings)
        : IAccessTokenGenerator
    {
        private readonly JwtSettings _jwtSettings = jwtSettings.Value;

        public AccessTokenDto Generate(User user)
        {
            DateTime expiresAt = DateTime.UtcNow.AddMinutes(
                _jwtSettings.ExpirationInMinutes);

            Claim[] claims =
            [
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email.Value),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),
            ];

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Key));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenValue = new JwtSecurityTokenHandler()
                .WriteToken(token);

            return new AccessTokenDto(
                tokenValue,
                expiresAt);
        }
    }
}
