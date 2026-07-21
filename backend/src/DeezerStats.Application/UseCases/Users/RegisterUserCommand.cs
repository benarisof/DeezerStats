namespace DeezerStats.Application.UseCases.Users
{
    public record RegisterUserCommand(string Email, string Password, string DisplayName);
}
