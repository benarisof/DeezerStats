using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;

namespace DeezerStats.Domain.UnitTests.Aggregates
{
    public class TrackTests
    {
        [Fact]
        public void NewTrackShouldNotBeEnriched()
        {
            // Arrange
            var track = new Track(
                id: Guid.NewGuid(),
                isrc: new Isrc("USCM51300736"),
                title: "Furthest Thing",
                artistId: Guid.NewGuid(),
                albumId: Guid.NewGuid());

            // Assert
            track.IsEnriched.Should().BeFalse();
            track.Duration.Should().BeNull();
            track.CoverUrl.Should().BeNull();
        }

        [Fact]
        public void EnrichWithValidDataShouldSetPropertiesAndMarkAsEnriched()
        {
            // Arrange
            var track = new Track(
                id: Guid.NewGuid(),
                isrc: new Isrc("USCM51300736"),
                title: "Furthest Thing",
                artistId: Guid.NewGuid(),
                albumId: Guid.NewGuid());

            var duration = new Duration(267);
            var coverUrl = "https://cdn-images.deezer.com/cover.jpg";

            // Act
            track.Enrich(duration, coverUrl);

            // Assert
            track.IsEnriched.Should().BeTrue();
            track.Duration.Should().Be(duration);
            track.CoverUrl.Should().Be(coverUrl);
        }

        [Fact]
        public void ConstructorWithEmptyTitleShouldThrowDomainException()
        {
            // Act
            Action act = () =>
            {
                var track = new Track(
                    id: Guid.NewGuid(),
                    isrc: new Isrc("USCM51300736"),
                    title: "   ",
                    artistId: Guid.NewGuid(),
                    albumId: Guid.NewGuid());

                _ = track;
            };

            // Assert
            act.Should().Throw<DomainException>()
               .WithMessage("Le titre du morceau est obligatoire.");
        }
    }
}
