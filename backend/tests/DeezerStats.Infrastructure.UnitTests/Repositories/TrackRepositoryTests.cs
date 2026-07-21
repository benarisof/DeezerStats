using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.UnitTests.Repositories
{
    public class TrackRepositoryTests
    {
        [Fact]
        public async Task AddAsyncAndGetByIsrcAsyncShouldPersistAndRetrieveTrackCorrectly()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new TrackRepository(context);

            var isrc = new Isrc("FR6V81100021");
            var track = new Track(
                id: Guid.NewGuid(),
                isrc: isrc,
                title: "Midnight City",
                artistId: Guid.NewGuid(),
                albumId: Guid.NewGuid());

            // Act
            await repository.AddAsync(track);
            Track? retrievedTrack = await repository.GetByIsrcAsync(isrc);

            // Assert
            retrievedTrack.Should().NotBeNull();
            retrievedTrack!.Title.Should().Be("Midnight City");
            retrievedTrack.Isrc.Should().Be(isrc); // Vérifie la reconversion du Value Object Isrc
        }

        [Fact]
        public async Task GetByIsrcsAsyncShouldReturnOnlyMatchingTracks()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new TrackRepository(context);

            var isrcA = new Isrc("FR6V81100021");
            var isrcB = new Isrc("USUM71607007");
            var isrcC = new Isrc("GBUM71029601");

            await repository.AddAsync(new Track(Guid.NewGuid(), isrcA, "Track A", Guid.NewGuid(), Guid.NewGuid()));
            await repository.AddAsync(new Track(Guid.NewGuid(), isrcB, "Track B", Guid.NewGuid(), Guid.NewGuid()));
            await repository.AddAsync(new Track(Guid.NewGuid(), isrcC, "Track C", Guid.NewGuid(), Guid.NewGuid()));

            // Act : on ne demande que deux des trois ISRC connus.
            IReadOnlyList<Track> retrieved = await repository.GetByIsrcsAsync([isrcA, isrcB]);

            // Assert
            retrieved.Should().HaveCount(2);
            retrieved.Select(t => t.Isrc).Should().BeEquivalentTo([isrcA, isrcB]);
        }

        [Fact]
        public async Task GetByIsrcsAsyncWithNoMatchShouldReturnEmptyList()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new TrackRepository(context);

            // Act
            IReadOnlyList<Track> retrieved = await repository.GetByIsrcsAsync([new Isrc("FR6V81100021")]);

            // Assert
            retrieved.Should().BeEmpty();
        }

        [Fact]
        public async Task AddRangeAsyncShouldTrackEntitiesWithoutSavingUntilSaveChangesIsCalled()
        {
            // Arrange : AddRangeAsync ne doit PAS persister à lui seul (voir ITrackRepository.AddRangeAsync) —
            // c'est ce qui permet à un cas d'usage comme l'import de committer plusieurs types
            // d'entités en une seule transaction, via IUnitOfWork.
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new TrackRepository(context);
            var isrc = new Isrc("FR6V81100021");
            var track = new Track(Guid.NewGuid(), isrc, "Track A", Guid.NewGuid(), Guid.NewGuid());

            // Act
            await repository.AddRangeAsync([track]);
            Track? beforeSave = await repository.GetByIsrcAsync(isrc);

            await context.SaveChangesAsync();
            Track? afterSave = await repository.GetByIsrcAsync(isrc);

            // Assert
            beforeSave.Should().BeNull();
            afterSave.Should().NotBeNull();
        }

        private static ApplicationDbContext CreateInMemoryDbContext()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
