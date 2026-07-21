using DeezerStats.Domain.Entities;
using DeezerStats.Domain.SeedWork;
using FluentAssertions;

namespace DeezerStats.Domain.UnitTests.Entities
{
    public class AlbumTests
    {
        [Fact]
        public void ConstructorShouldTrimTitleAndComputeNormalizedTitle()
        {
            // Act
            var album = new Album(Guid.NewGuid(), "  After Hours  ", Guid.NewGuid());

            // Assert
            album.Title.Should().Be("After Hours");
            album.NormalizedTitle.Should().Be("after hours");
        }

        [Fact]
        public void NormalizeShouldBeCaseAndWhitespaceInsensitive()
        {
            Album.Normalize("After Hours").Should().Be("after hours");
            Album.Normalize("  after hours ").Should().Be("after hours");
            Album.Normalize("AFTER HOURS").Should().Be("after hours");
        }

        [Fact]
        public void ConstructorWithEmptyTitleShouldThrowDomainException()
        {
            // Act
            Action act = () =>
            {
                var album = new Album(Guid.NewGuid(), "   ", Guid.NewGuid());
                _ = album;
            };

            // Assert
            act.Should().Throw<DomainException>()
               .WithMessage("Le titre de l'album est obligatoire.");
        }

        [Fact]
        public void ConstructorWithEmptyArtistIdShouldThrowDomainException()
        {
            // Act
            Action act = () =>
            {
                var album = new Album(Guid.NewGuid(), "After Hours", Guid.Empty);
                _ = album;
            };

            // Assert
            act.Should().Throw<DomainException>()
               .WithMessage("Un album doit être rattaché à un artiste.");
        }
    }
}
