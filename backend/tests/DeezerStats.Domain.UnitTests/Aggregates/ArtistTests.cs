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

        [Fact]
        public void NewArtistShouldNotBeEnriched()
        {
            // Arrange
            var artist = new Artist(Guid.NewGuid(), "Daft Punk");

            // Assert
            artist.IsEnriched.Should().BeFalse();
            artist.CoverUrl.Should().BeNull();
        }

        [Fact]
        public void EnrichCoverWithValidUrlShouldSetCoverUrlAndMarkAsEnriched()
        {
            // Arrange
            var artist = new Artist(Guid.NewGuid(), "Daft Punk");

            // Act
            artist.EnrichCover("https://cdn-images.deezer.com/artist-cover.jpg");

            // Assert
            artist.IsEnriched.Should().BeTrue();
            artist.CoverUrl.Should().Be("https://cdn-images.deezer.com/artist-cover.jpg");
        }

        [Fact]
        public void EnrichCoverWithNullOrWhitespaceShouldLeaveArtistUnenriched()
        {
            // Arrange : Deezer peut ne renvoyer aucune photo pour un artiste (voir
            // DeezerArtistMetadata.CoverUrl, nullable) — ne doit jamais faire planter
            // l'enrichissement, juste laisser l'artiste non enrichi pour une prochaine tentative.
            var artist = new Artist(Guid.NewGuid(), "Daft Punk");

            // Act
            artist.EnrichCover(null);
            artist.EnrichCover("   ");

            // Assert
            artist.IsEnriched.Should().BeFalse();
            artist.CoverUrl.Should().BeNull();
        }
    }
}
