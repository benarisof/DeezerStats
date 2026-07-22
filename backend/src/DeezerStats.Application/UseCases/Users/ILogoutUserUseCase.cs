namespace DeezerStats.Application.UseCases.Users
{
    public interface ILogoutUserUseCase
    {
        public Task ExecuteAsync(LogoutUserCommand command, CancellationToken ct = default);
    }
}
