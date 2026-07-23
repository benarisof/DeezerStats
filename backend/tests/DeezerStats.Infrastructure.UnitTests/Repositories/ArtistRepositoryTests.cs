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
            await context.SaveChangesAsync();
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
            await context.SaveChangesAsync();

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
            await context.SaveChangesAsync();

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

        [Fact]
        public async Task UpdateAsyncShouldPersistEnrichedCoverUrl()
        {
            // Arrange : reproduit le scénario de GetOrEnrichArtistUseCase — l'artiste est d'abord
            // persisté sans photo, puis mis à jour après l'appel à Deezer. Un second DbContext
            // (même base nommée) est utilisé pour la relecture, afin de vérifier une réelle
            // persistance plutôt que le simple suivi en mémoire du ChangeTracker de l'instance
            // ayant fait la mise à jour.
            var databaseName = Guid.NewGuid().ToString();
            var artistId = Guid.NewGuid();

            using (ApplicationDbContext writeContext = CreateInMemoryDbContext(databaseName))
            {
                var writeRepository = new ArtistRepository(writeContext);
                var artist = new Artist(artistId, "Daft Punk");
                await writeRepository.AddAsync(artist);
                await writeContext.SaveChangesAsync();

                // Act
                artist.EnrichCover("https://cdn-images.deezer.com/artist-cover.jpg");
                await writeRepository.UpdateAsync(artist);
                await writeContext.SaveChangesAsync();
            }

            // Assert
            using ApplicationDbContext readContext = CreateInMemoryDbContext(databaseName);
            Artist? retrieved = await new ArtistRepository(readContext).GetByIdAsync(artistId);

            retrieved.Should().NotBeNull();
            retrieved!.CoverUrl.Should().Be("https://cdn-images.deezer.com/artist-cover.jpg");
            retrieved.IsEnriched.Should().BeTrue();
        }

        private static ApplicationDbContext CreateInMemoryDbContext(string? databaseName = null)
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
