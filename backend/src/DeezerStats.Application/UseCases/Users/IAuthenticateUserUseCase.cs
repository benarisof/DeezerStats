using DeezerStats.Application.DTOs;

namespace DeezerStats.Application.UseCases.Users
{
    public interface IAuthenticateUserUseCase
    {
        public Task<AccessTokenDto> ExecuteAsync(AuthenticateUserCommand command, CancellationToken ct = default);
    }
}
