using DeezerStats.Application.DTOs;

namespace DeezerStats.Application.UseCases.Users
{
    public interface IRegisterUserUseCase
    {
        public Task<AuthTokensDto> ExecuteAsync(RegisterUserCommand command, CancellationToken ct = default);
    }
}
