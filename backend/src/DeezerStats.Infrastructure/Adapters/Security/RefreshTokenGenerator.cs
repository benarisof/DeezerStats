using System.Security.Cryptography;
using System.Text;
using DeezerStats.Application.Ports.Security;

namespace DeezerStats.Infrastructure.Adapters.Security
{
    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private const int TokenSizeInBytes = 64;

        public string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeInBytes));

        public string Hash(string token)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexStringLower(hashBytes);
        }
    }
}
