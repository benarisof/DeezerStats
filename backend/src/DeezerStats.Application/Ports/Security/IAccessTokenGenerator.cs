using DeezerStats.Application.DTOs;
using DeezerStats.Domain.Aggregates.UserAggregate;

namespace DeezerStats.Application.Ports.Security
{
    public interface IAccessTokenGenerator
    {
        public AccessTokenDto Generate(User user);
    }
}
