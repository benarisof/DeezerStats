namespace DeezerStats.Application.UseCases.Users
{
    public record AuthenticateUserCommand(
        string Email,
        string Password);
}
