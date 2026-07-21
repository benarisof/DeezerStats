using DeezerStats.Application.UseCases.Users;
using DeezerStats.Application.Validation.Validators;
using FluentValidation.TestHelper;

namespace DeezerStats.Application.UnitTests.Validation
{
    public class RegisterUserCommandValidatorTests
    {
        private readonly RegisterUserCommandValidator _validator = new();

        [Fact]
        public void ValidateWhenEmailIsInvalidShouldHaveValidationError()
        {
            // Arrange
            var command = new RegisterUserCommand("invalid-email", "Password123!", "Alex");

            // Act
            TestValidationResult<RegisterUserCommand> result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("L'adresse email n'est pas valide.");
        }

        [Fact]
        public void ValidateWhenPasswordIsTooShortShouldHaveValidationError()
        {
            // Arrange
            var command = new RegisterUserCommand("alex@example.com", "123", "Alex");

            // Act
            TestValidationResult<RegisterUserCommand> result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage("Le mot de passe doit contenir au moins 8 caractères.");
        }
    }
}
