using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;

namespace DeezerStats.Domain.UnitTests.ValueObjects
{
    public class DateRangeTests
    {
        [Fact]
        public void ConstructorWhenFromIsAfterToShouldThrowDomainException()
        {
            // Arrange
            var from = new DateOnly(2025, 12, 31);
            var to = new DateOnly(2025, 01, 01);

            // Act
            Action act = () => _ = new DateRange(from, to);

            // Assert
            act.Should().Throw<DomainException>()
               .WithMessage("La date de début ne peut pas être postérieure à la date de fin.");
        }

        [Fact]
        public void ContainsWhenDateInBetweenShouldReturnTrue()
        {
            // Arrange
            var range = new DateRange(new DateOnly(2025, 01, 01), new DateOnly(2025, 12, 31));
            var testDate = new DateTime(2025, 06, 15);

            // Act & Assert
            range.Contains(testDate).Should().BeTrue();
        }
    }
}
