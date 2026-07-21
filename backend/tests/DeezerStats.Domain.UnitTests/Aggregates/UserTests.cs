using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Domain.UnitTests.Aggregates
{
    public class UserTests
    {
        [Fact]
        public void ConstructorShouldCreateUserWithValidData()
        {
            // Arrange
            var id = Guid.NewGuid();
            var email = new Email("user@test.com");

            // Act
            var user = new User(
                id,
                email,
                "hashed-password",
                "Sofiane");

            // Assert
            Assert.Equal(id, user.Id);
            Assert.Equal(email, user.Email);
            Assert.Equal("Sofiane", user.DisplayName);
            Assert.Equal("hashed-password", user.PasswordHash);
            Assert.NotEqual(default, user.CreatedAt);
        }

        [Fact]
        public void ConstructorShouldRejectEmptyPasswordHash()
        {
            // Arrange
            var email = new Email("user@test.com");

            // Fonction locale
            User CreateUser() => new(
                Guid.NewGuid(),
                email,
                string.Empty,
                "Sofiane");

            // Act & Assert
            DomainException exception = Assert.Throws<DomainException>(CreateUser);
            Assert.Equal(
                "Le mot de passe haché ne peut pas être vide.",
                exception.Message);
        }

        [Fact]
        public void ConstructorShouldRejectEmptyDisplayName()
        {
            // Arrange
            var email = new Email("user@test.com");

            // Fonction locale
            User CreateUser() => new(
                Guid.NewGuid(),
                email,
                "hashed-password",
                string.Empty);

            // Act & Assert
            DomainException exception = Assert.Throws<DomainException>(CreateUser);
            Assert.Equal(
                "Le nom d'affichage est obligatoire.",
                exception.Message);
        }

        [Fact]
        public void ConstructorShouldTrimDisplayName()
        {
            // Arrange
            var email = new Email("user@test.com");

            // Act
            var user = new User(
                Guid.NewGuid(),
                email,
                "hashed-password",
                "  Sofiane  ");

            // Assert
            Assert.Equal("Sofiane", user.DisplayName);
        }

        [Fact]
        public void UpdateProfileShouldUpdateDisplayName()
        {
            // Arrange
            var email = new Email("user@test.com");
            var user = new User(
                Guid.NewGuid(),
                email,
                "hashed-password",
                "Sofiane");

            // Act
            user.UpdateProfile("  Nouveau nom  ");

            // Assert
            Assert.Equal("Nouveau nom", user.DisplayName);
        }

        [Fact]
        public void UpdateProfileShouldRejectEmptyDisplayName()
        {
            // Arrange
            var email = new Email("user@test.com");
            var user = new User(
                Guid.NewGuid(),
                email,
                "hashed-password",
                "Sofiane");

            // Fonction locale (void)
            void UpdateProfile() => user.UpdateProfile(string.Empty);

            // Act & Assert
            Assert.Throws<DomainException>(UpdateProfile);
        }
    }
}
