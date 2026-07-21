using DeezerStats.Domain.Entities;
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

        [Fact]
        public async Task GetByTitleAndArtistAsyncShouldFindAlbumRegardlessOfCaseAndWhitespace()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);
            var artistId = Guid.NewGuid();
            var album = new Album(Guid.NewGuid(), "Discovery", artistId);
            await repository.AddAsync(album);

            // Act
            Album? retrieved = await repository.GetByTitleAndArtistAsync("  discovery ", artistId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(album.Id);
        }

        [Fact]
        public async Task GetByTitleAndArtistAsyncWithDifferentArtistShouldReturnNull()
        {
            // Arrange : même titre d'album, mais pour un artiste différent -> ne doit pas matcher.
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);
            await repository.AddAsync(new Album(Guid.NewGuid(), "Discovery", Guid.NewGuid()));

            // Act
            Album? retrieved = await repository.GetByTitleAndArtistAsync("Discovery", Guid.NewGuid());

            // Assert
            retrieved.Should().BeNull();
        }

        [Fact]
        public async Task AddAsyncWithDuplicateNormalizedTitleForSameArtistShouldThrow()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);
            var artistId = Guid.NewGuid();
            await repository.AddAsync(new Album(Guid.NewGuid(), "Discovery", artistId));

            // Act
            Func<Task> act = () => repository.AddAsync(new Album(Guid.NewGuid(), " discovery ", artistId));

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GetByArtistIdsAsyncShouldReturnOnlyAlbumsOfRequestedArtists()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);
            var artistId = Guid.NewGuid();
            var otherArtistId = Guid.NewGuid();

            var albumA = new Album(Guid.NewGuid(), "Discovery", artistId);
            var albumB = new Album(Guid.NewGuid(), "Random Access Memories", artistId);
            await repository.AddAsync(albumA);
            await repository.AddAsync(albumB);
            await repository.AddAsync(new Album(Guid.NewGuid(), "After Hours", otherArtistId));

            // Act
            IReadOnlyList<Album> retrieved = await repository.GetByArtistIdsAsync([artistId]);

            // Assert
            retrieved.Should().HaveCount(2);
            retrieved.Select(a => a.Id).Should().BeEquivalentTo([albumA.Id, albumB.Id]);
        }

        [Fact]
        public async Task AddRangeAsyncShouldTrackEntitiesWithoutSavingUntilSaveChangesIsCalled()
        {
            // Arrange : AddRangeAsync ne doit PAS persister à lui seul (voir IAlbumRepository.AddRangeAsync) —
            // c'est ce qui permet à un cas d'usage comme l'import de committer plusieurs types
            // d'entités en une seule transaction, via IUnitOfWork.
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);
            var album = new Album(Guid.NewGuid(), "Discovery", Guid.NewGuid());

            // Act
            await repository.AddRangeAsync([album]);
            Album? beforeSave = await repository.GetByIdAsync(album.Id);

            await context.SaveChangesAsync();
            Album? afterSave = await repository.GetByIdAsync(album.Id);

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
