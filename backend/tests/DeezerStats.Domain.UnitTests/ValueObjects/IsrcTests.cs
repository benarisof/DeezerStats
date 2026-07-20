using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;

namespace DeezerStats.Domain.UnitTests.ValueObjects
{
    public class IsrcTests
    {
        [Theory]
        [InlineData("USCM51300736")]
        [InlineData("FRY689310074")]
        [InlineData("QZCE61803002")]
        public void ConstructorWithValidIsrcShouldCreateInstanceAndNormalizeToUppercase(string rawIsrc)
        {
            // Act
            var isrc = new Isrc(rawIsrc.ToLowerInvariant());

            // Assert
            isrc.Value.Should().Be(rawIsrc.ToUpperInvariant());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("INVALID_ISRC")]
        [InlineData("USCM5130073")] // Trop court (11 chars)
        [InlineData("USCM5130073699")] // Trop long
        public void ConstructorWithInvalidIsrcShouldThrowDomainException(string invalidIsrc)
        {
            // Act
            Action act = () => _ = new Isrc(invalidIsrc);

            // Assert
            act.Should().Throw<DomainException>()
               .WithMessage("*ISRC*invalide*");
        }

        [Fact]
        public void EqualityTwoIsrcsWithSameValueShouldBeEqual()
        {
            // Arrange
            var isrc1 = new Isrc("USCM51300736");
            var isrc2 = new Isrc("uscm51300736");

            // Assert
            isrc1.Should().Be(isrc2);
            (isrc1 == isrc2).Should().BeTrue();
        }
    }
}
