using DeezerStats.Infrastructure.Adapters.Security;

namespace DeezerStats.Infrastructure.UnitTests.Adapters
{
    public class BCryptPasswordHasherTests
    {
        private readonly BCryptPasswordHasher _hasher = new();

        [Fact]
        public void HashShouldReturnAHashDifferentFromPlainText()
        {
            // Arrange
            const string plainTextPassword = "password123";

            // Act
            var hashedPassword = _hasher.Hash(plainTextPassword);

            // Assert
            Assert.NotEqual(
                plainTextPassword,
                hashedPassword);
        }

        [Fact]
        public void HashShouldReturnAVerifiableHash()
        {
            // Arrange
            const string plainTextPassword = "password123";

            // Act
            var hashedPassword = _hasher.Hash(plainTextPassword);

            // Assert
            Assert.True(
                _hasher.Verify(
                    plainTextPassword,
                    hashedPassword));
        }

        [Fact]
        public void HashShouldProduceDifferentHashesForSamePassword()
        {
            // Arrange
            const string plainTextPassword = "password123";

            // Act
            var firstHash = _hasher.Hash(plainTextPassword);
            var secondHash = _hasher.Hash(plainTextPassword);

            // Assert
            Assert.NotEqual(firstHash, secondHash);
        }

        [Fact]
        public void HashShouldThrowWhenPasswordIsEmpty()
        {
            // Act
            string Action() => _hasher.Hash(string.Empty);

            // Assert
            ArgumentException exception = Assert.Throws<ArgumentException>((Func<string>)Action);

            Assert.Equal(
                "plainTextPassword",
                exception.ParamName);
        }

        [Fact]
        public void HashShouldThrowWhenPasswordIsWhitespace()
        {
            // Act
            string Action() => _hasher.Hash("   ");

            // Assert
            Assert.Throws<ArgumentException>((Func<string>)Action);
        }

        [Fact]
        public void VerifyShouldReturnTrueForCorrectPassword()
        {
            // Arrange
            const string plainTextPassword = "password123";

            var hashedPassword = _hasher.Hash(plainTextPassword);

            // Act
            var result = _hasher.Verify(
                plainTextPassword,
                hashedPassword);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyShouldReturnFalseForIncorrectPassword()
        {
            // Arrange
            const string hashedPassword =
                "$2a$12$abcdefghijklmnopqrstuu123456789012345678901234567890";

            // Act
            var result = _hasher.Verify(
                "wrong-password",
                hashedPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyShouldReturnFalseWhenPlainTextPasswordIsEmpty()
        {
            // Arrange
            var hashedPassword = _hasher.Hash("password123");

            // Act
            var result = _hasher.Verify(
                string.Empty,
                hashedPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyShouldReturnFalseWhenHashedPasswordIsEmpty()
        {
            // Act
            var result = _hasher.Verify(
                "password123",
                string.Empty);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyShouldReturnFalseWhenHashIsInvalid()
        {
            // Act
            var result = _hasher.Verify(
                "password123",
                "invalid-hash");

            // Assert
            Assert.False(result);
        }
    }
}
