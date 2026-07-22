using DeezerStats.Application.DTOs;

namespace DeezerStats.Application.UseCases.Users
{
    public interface IAuthenticateUserUseCase
    {
        public Task<AuthTokensDto> ExecuteAsync(AuthenticateUserCommand command, CancellationToken ct = default);
    }
}
