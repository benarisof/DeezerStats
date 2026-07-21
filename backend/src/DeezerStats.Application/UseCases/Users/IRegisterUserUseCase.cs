using DeezerStats.Domain.Aggregates.UserAggregate;

namespace DeezerStats.Application.UseCases.Users
{
    public interface IRegisterUserUseCase
    {
        public Task<User> ExecuteAsync(RegisterUserCommand command, CancellationToken ct = default);
    }
}
