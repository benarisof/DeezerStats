using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.UnitTests.Repositories
{
    public class AlbumRepositoryTests
    {
        [Fact]
        public async Task AddAsyncAndGetByIdAsyncShouldPersistAndRetrieveAlbum()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);
            var albumId = Guid.NewGuid();
            var album = new Album(albumId, "Random Access Memories", Guid.NewGuid());

            // Act
            await repository.AddAsync(album);
            Album? retrieved = await repository.GetByIdAsync(albumId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be("Random Access Memories");
            retrieved.IsEnriched.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsyncShouldUpdateEnrichedPropertiesInDatabase()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);
            var album = new Album(Guid.NewGuid(), "Discovery", Guid.NewGuid());
            await repository.AddAsync(album);

            // Act
            album.Enrich(
                coverUrl: "https://cdn-images.deezer.com/discovery.jpg",
                releaseDate: new DateOnly(2001, 3, 12),
                duration: new Duration(3650));
            await repository.UpdateAsync(album);

            Album? updatedAlbum = await repository.GetByIdAsync(album.Id);

            // Assert
            updatedAlbum.Should().NotBeNull();
            updatedAlbum!.IsEnriched.Should().BeTrue();
            updatedAlbum.CoverUrl.Should().Be("https://cdn-images.deezer.com/discovery.jpg");
            updatedAlbum.ReleaseDate.Should().Be(new DateOnly(2001, 3, 12));
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
