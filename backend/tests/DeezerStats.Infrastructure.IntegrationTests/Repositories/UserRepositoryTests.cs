using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.UnitTests.Repositories
{
    public class UserRepositoryTests
    {
        [Fact]
        public async Task AddAsyncAndGetByIdAsyncShouldPersistAndRetrieveUser()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new UserRepository(context);
            var userId = Guid.NewGuid();
            var user = new User(userId, "alex@example.com", "hashed_password_123", "Alex");

            // Act
            await repository.AddAsync(user);
            User? retrieved = await repository.GetByIdAsync(userId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Email.Should().Be("alex@example.com");
            retrieved.DisplayName.Should().Be("Alex");
        }

        [Fact]
        public async Task GetByEmailAsyncShouldFindUserInsensitiveToCaseAndSpaces()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new UserRepository(context);
            var user = new User(Guid.NewGuid(), "alex@example.com", "hashed_password_123", "Alex");
            await repository.AddAsync(user);

            // Act : On cherche avec des majuscules et des espaces autour
            User? retrieved = await repository.GetByEmailAsync("  ALEX@EXAMPLE.COM  ");

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByEmailAsyncWhenUserDoesNotExistShouldReturnNull()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new UserRepository(context);

            // Act
            User? retrieved = await repository.GetByEmailAsync("unknown@example.com");

            // Assert
            retrieved.Should().BeNull();
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
