using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;

namespace DeezerStats.Domain.UnitTests.ValueObjects
{
    public class DurationTests
    {
        [Fact]
        public void ConstructorWithNegativeSecondsShouldThrowDomainException()
        {
            // Act
            Action act = () => _ = new Duration(-10);

            // Assert
            act.Should().Throw<DomainException>()
               .WithMessage("Une durée ne peut pas être négative.");
        }

        [Fact]
        public void ToStringShouldFormatCorrectly()
        {
            // Arrange
            var duration = new Duration(125); // 2 minutes 5 secondes

            // Assert
            duration.ToString().Should().Be("02:05");
        }
    }
}
