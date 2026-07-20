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

            await repository.AddRangeAsync([listeningEvent]);

            // Act
            var exists = await repository.ExistsAsync(userId, isrc, listenedAt);

            // Assert
            exists.Should().BeTrue();
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
