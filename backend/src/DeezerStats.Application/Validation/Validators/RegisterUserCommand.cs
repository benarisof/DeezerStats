namespace DeezerStats.Application.Validation.Validators
{
    public record RegisterUserCommand(string Email, string Password, string DisplayName);
}
