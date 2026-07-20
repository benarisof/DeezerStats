using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.UnitTests.Repositories
{
    public class ArtistRepositoryTests
    {
        [Fact]
        public async Task AddAsyncAndGetByIdAsyncShouldPersistAndRetrieveArtist()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ArtistRepository(context);
            var artistId = Guid.NewGuid();
            var artist = new Artist(artistId, "Daft Punk");

            // Act
            await repository.AddAsync(artist);
            Artist? retrieved = await repository.GetByIdAsync(artistId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be("Daft Punk");
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
