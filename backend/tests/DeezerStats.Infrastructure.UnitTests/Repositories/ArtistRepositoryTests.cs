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

        [Fact]
        public async Task GetByNameAsyncShouldFindArtistRegardlessOfCaseAndWhitespace()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ArtistRepository(context);
            var artist = new Artist(Guid.NewGuid(), "The Weeknd");
            await repository.AddAsync(artist);

            // Act
            Artist? retrieved = await repository.GetByNameAsync("  the weeknd ");

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(artist.Id);
        }

        [Fact]
        public async Task GetByNameAsyncWithUnknownNameShouldReturnNull()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ArtistRepository(context);

            // Act
            Artist? retrieved = await repository.GetByNameAsync("Inconnu");

            // Assert
            retrieved.Should().BeNull();
        }

        [Fact]
        public async Task AddAsyncWithDuplicateNormalizedNameShouldThrow()
        {
            // Arrange : NormalizedName est configurée comme clé alternative (voir ArtistConfiguration),
            // le filet de sécurité en base contre les doublons d'artiste même si la recherche
            // applicative est contournée. Ici, les deux entités étant suivies par le même DbContext,
            // EF Core détecte le conflit dès l'ajout (identity map du ChangeTracker), avant même
            // SaveChanges : c'est une InvalidOperationException. Dans un scénario réel de deux imports
            // concurrents utilisant chacun leur propre DbContext, c'est la contrainte d'unicité en base
            // qui interviendrait, avec une DbUpdateException levée par SaveChangesAsync.
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ArtistRepository(context);
            await repository.AddAsync(new Artist(Guid.NewGuid(), "The Weeknd"));

            // Act
            Func<Task> act = () => repository.AddAsync(new Artist(Guid.NewGuid(), " the weeknd "));

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GetByNamesAsyncShouldReturnOnlyMatchingArtistsRegardlessOfCaseAndWhitespace()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ArtistRepository(context);
            var weeknd = new Artist(Guid.NewGuid(), "The Weeknd");
            var daftPunk = new Artist(Guid.NewGuid(), "Daft Punk");
            await repository.AddAsync(weeknd);
            await repository.AddAsync(daftPunk);

            // Act : une seule des deux entrées demandées existe, avec une casse/espaces différents.
            IReadOnlyList<Artist> retrieved = await repository.GetByNamesAsync(["  the weeknd ", "Inconnu"]);

            // Assert
            retrieved.Should().ContainSingle(a => a.Id == weeknd.Id);
        }

        [Fact]
        public async Task AddRangeAsyncShouldTrackEntitiesWithoutSavingUntilSaveChangesIsCalled()
        {
            // Arrange : AddRangeAsync ne doit PAS persister à lui seul (voir IArtistRepository.AddRangeAsync) —
            // c'est ce qui permet à un cas d'usage comme l'import de committer plusieurs types
            // d'entités en une seule transaction, via IUnitOfWork.
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ArtistRepository(context);
            var artist = new Artist(Guid.NewGuid(), "Daft Punk");

            // Act
            await repository.AddRangeAsync([artist]);
            Artist? beforeSave = await repository.GetByIdAsync(artist.Id);

            await context.SaveChangesAsync();
            Artist? afterSave = await repository.GetByIdAsync(artist.Id);

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
