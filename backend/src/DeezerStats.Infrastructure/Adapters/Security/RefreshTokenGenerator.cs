using System.Security.Cryptography;
using System.Text;
using DeezerStats.Application.Ports.Security;

namespace DeezerStats.Infrastructure.Adapters.Security
{
    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private const int _tokenSizeInBytes = 64;

        public string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(_tokenSizeInBytes));

        public string Hash(string token)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexStringLower(hashBytes);
        }
    }
}
