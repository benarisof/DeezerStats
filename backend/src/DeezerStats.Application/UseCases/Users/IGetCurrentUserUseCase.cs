using DeezerStats.Application.DTOs;

namespace DeezerStats.Application.UseCases.Users
{
    public interface IGetCurrentUserUseCase
    {
        public Task<UserProfileDto> ExecuteAsync(Guid userId, CancellationToken ct = default);
    }
}
