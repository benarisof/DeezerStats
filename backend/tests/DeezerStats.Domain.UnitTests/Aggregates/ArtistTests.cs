using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.SeedWork;
using FluentAssertions;

namespace DeezerStats.Domain.UnitTests.Aggregates
{
    public class ArtistTests
    {
        [Fact]
        public void ConstructorShouldTrimNameAndComputeNormalizedName()
        {
            // Act
            var artist = new Artist(Guid.NewGuid(), "  The Weeknd  ");

            // Assert
            artist.Name.Should().Be("The Weeknd");
            artist.NormalizedName.Should().Be("the weeknd");
        }

        [Fact]
        public void NormalizeShouldBeCaseAndWhitespaceInsensitive()
        {
            Artist.Normalize("The Weeknd").Should().Be("the weeknd");
            Artist.Normalize("  the weeknd ").Should().Be("the weeknd");
            Artist.Normalize("THE WEEKND").Should().Be("the weeknd");
        }

        [Fact]
        public void ConstructorWithEmptyNameShouldThrowDomainException()
        {
            // Act
            Action act = () =>
            {
                var artist = new Artist(Guid.NewGuid(), "   ");
                _ = artist;
            };

            // Assert
            act.Should().Throw<DomainException>()
               .WithMessage("Le nom de l'artiste est obligatoire.");
        }
    }
}
