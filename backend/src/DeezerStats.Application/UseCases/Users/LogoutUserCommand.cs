namespace DeezerStats.Application.UseCases.Users
{
    public record LogoutUserCommand(Guid UserId, string RefreshToken);
}
