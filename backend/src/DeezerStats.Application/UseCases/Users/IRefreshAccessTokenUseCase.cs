using DeezerStats.Application.DTOs;

namespace DeezerStats.Application.UseCases.Users
{
    public interface IRefreshAccessTokenUseCase
    {
        public Task<AuthTokensDto> ExecuteAsync(RefreshAccessTokenCommand command, CancellationToken ct = default);
    }
}
