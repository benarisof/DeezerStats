using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.IntegrationTests.Repositories
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

        private static ApplicationDbContext CreateInMemoryDbContext()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
