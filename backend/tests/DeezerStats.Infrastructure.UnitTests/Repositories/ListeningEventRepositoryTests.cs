using DeezerStats.Domain.Entities;
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
            var isrc = new Isrc("FR6V81100021");
            DateTime listenedAt = DateTime.UtcNow;

            var listeningEvent = new ListeningEvent(
                id: Guid.NewGuid(),
                userId: userId,
                trackId: Guid.NewGuid(),
                isrc: isrc,
                listeningDuration: new Duration(200),
                listenedAt: listenedAt);

            // AddRangeAsync ne déclenche plus SaveChangesAsync lui-même (voir
            // IListeningEventRepository.AddRangeAsync) : il faut committer explicitement, comme le
            // ferait le Unit of Work dans le cas d'usage appelant.
            await repository.AddRangeAsync([listeningEvent]);
            await context.SaveChangesAsync();

            // Act
            var exists = await repository.ExistsAsync(userId, isrc, listenedAt);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task GetExistingListenedAtsAsyncShouldReturnOnlyMatchingUserAndIsrcs()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ListeningEventRepository(context);

            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var isrcA = new Isrc("FR6V81100021");
            var isrcB = new Isrc("USUM71607007");
            DateTime firstListen = DateTime.UtcNow.AddDays(-2);
            DateTime secondListen = DateTime.UtcNow.AddDays(-1);

            await repository.AddRangeAsync([
                new ListeningEvent(Guid.NewGuid(), userId, Guid.NewGuid(), isrcA, new Duration(200), firstListen),
                new ListeningEvent(Guid.NewGuid(), userId, Guid.NewGuid(), isrcA, new Duration(200), secondListen),
                new ListeningEvent(Guid.NewGuid(), userId, Guid.NewGuid(), isrcB, new Duration(180), firstListen),

                // Ne doit pas être pris en compte : appartient à un autre utilisateur.
                new ListeningEvent(Guid.NewGuid(), otherUserId, Guid.NewGuid(), isrcA, new Duration(200), firstListen),
            ]);
            await context.SaveChangesAsync();

            // Act
            IReadOnlyDictionary<Isrc, HashSet<DateTime>> result =
                await repository.GetExistingListenedAtsAsync(userId, [isrcA, isrcB]);

            // Assert
            result.Should().ContainKey(isrcA);
            result[isrcA].Should().BeEquivalentTo([firstListen, secondListen]);
            result.Should().ContainKey(isrcB);
            result[isrcB].Should().BeEquivalentTo([firstListen]);
        }

        [Fact]
        public async Task GetExistingListenedAtsAsyncWithNoIsrcsShouldReturnEmptyDictionary()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new ListeningEventRepository(context);

            // Act
            IReadOnlyDictionary<Isrc, HashSet<DateTime>> result =
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
