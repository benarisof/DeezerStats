using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.UnitTests.Repositories
{
    public class ListeningEventRepositoryTests
    {
        [Fact]
        public async Task ExistsAsyncWhenEventExistsShouldReturnTrue()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ListeningEventRepository(context);

            var userId = Guid.NewGuid();
            var trackId = Guid.NewGuid();
            DateTime listenedAt = DateTime.UtcNow;

            var listeningEvent = new ListeningEvent(
                id: Guid.NewGuid(),
                userId: userId,
                trackId: trackId,
                listeningDuration: new Duration(200),
                listenedAt: listenedAt);

            // AddRangeAsync ne déclenche plus SaveChangesAsync lui-même (voir
            // IListeningEventRepository.AddRangeAsync) : il faut committer explicitement, comme le
            // ferait le Unit of Work dans le cas d'usage appelant.
            await repository.AddRangeAsync([listeningEvent]);
            await context.SaveChangesAsync();

            // Act
            var exists = await repository.ExistsAsync(userId, trackId, listenedAt);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task GetExistingListenedAtsAsyncShouldReturnOnlyMatchingUserAndTrackIds()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ListeningEventRepository(context);

            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var trackIdA = Guid.NewGuid();
            var trackIdB = Guid.NewGuid();
            DateTime firstListen = DateTime.UtcNow.AddDays(-2);
            DateTime secondListen = DateTime.UtcNow.AddDays(-1);

            await repository.AddRangeAsync([
                new ListeningEvent(Guid.NewGuid(), userId, trackIdA, new Duration(200), firstListen),
                new ListeningEvent(Guid.NewGuid(), userId, trackIdA, new Duration(200), secondListen),
                new ListeningEvent(Guid.NewGuid(), userId, trackIdB, new Duration(180), firstListen),

                // Ne doit pas être pris en compte : appartient à un autre utilisateur.
                new ListeningEvent(Guid.NewGuid(), otherUserId, trackIdA, new Duration(200), firstListen),
            ]);
            await context.SaveChangesAsync();

            // Act
            IReadOnlyDictionary<Guid, HashSet<DateTime>> result =
                await repository.GetExistingListenedAtsAsync(userId, [trackIdA, trackIdB]);

            // Assert
            result.Should().ContainKey(trackIdA);
            result[trackIdA].Should().BeEquivalentTo([firstListen, secondListen]);
            result.Should().ContainKey(trackIdB);
            result[trackIdB].Should().BeEquivalentTo([firstListen]);
        }

        [Fact]
        public async Task AddRangeAsyncWithDuplicateUserTrackListenedAtShouldThrow()
        {
            // Arrange : (UserId, TrackId, ListenedAt) est configuré comme clé alternative (voir
            // ListeningEventConfiguration), le filet de sécurité en base contre les doublons même si
            // la vérification applicative d'ImportListeningHistoryUseCase est contournée. Ici, les
            // deux entités étant suivies par le même DbContext, EF Core détecte le conflit dès
            // l'ajout (identity map du ChangeTracker), avant même SaveChanges : une
            // InvalidOperationException. Dans un scénario réel de deux imports concurrents utilisant
            // chacun leur propre DbContext, c'est la contrainte d'unicité en base qui interviendrait,
            // avec une DbUpdateException levée par SaveChangesAsync (même comportement que pour
            // Artist/Album/User).
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ListeningEventRepository(context);

            var userId = Guid.NewGuid();
            var trackId = Guid.NewGuid();
            DateTime listenedAt = DateTime.UtcNow;

            await repository.AddRangeAsync([
                new ListeningEvent(Guid.NewGuid(), userId, trackId, new Duration(200), listenedAt),
            ]);
            await context.SaveChangesAsync();

            // Act
            Func<Task> act = () => repository.AddRangeAsync([
                new ListeningEvent(Guid.NewGuid(), userId, trackId, new Duration(180), listenedAt),
            ]);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GetExistingListenedAtsAsyncWithNoTrackIdsShouldReturnEmptyDictionary()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ListeningEventRepository(context);

            // Act
            IReadOnlyDictionary<Guid, HashSet<DateTime>> result =
                await repository.GetExistingListenedAtsAsync(Guid.NewGuid(), []);

            // Assert
            result.Should().BeEmpty();
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
