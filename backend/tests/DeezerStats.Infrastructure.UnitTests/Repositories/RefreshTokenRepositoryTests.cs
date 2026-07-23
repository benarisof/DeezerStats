using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.UnitTests.Repositories
{
    public class RefreshTokenRepositoryTests
    {
        [Fact]
        public async Task AddAsyncAndGetByTokenHashAsyncShouldPersistAndRetrieveToken()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new RefreshTokenRepository(context);
            var token = new RefreshToken(Guid.NewGuid(), Guid.NewGuid(), "hashed-token", DateTime.UtcNow.AddDays(30));

            // Act
            await repository.AddAsync(token);
            await context.SaveChangesAsync();
            RefreshToken? retrieved = await repository.GetByTokenHashAsync("hashed-token");

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(token.Id);
            retrieved.UserId.Should().Be(token.UserId);
        }

        [Fact]
        public async Task GetByTokenHashAsyncWhenTokenDoesNotExistShouldReturnNull()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new RefreshTokenRepository(context);

            // Act
            RefreshToken? retrieved = await repository.GetByTokenHashAsync("unknown-hash");

            // Assert
            retrieved.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsyncShouldPersistRevocation()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new RefreshTokenRepository(context);
            var token = new RefreshToken(Guid.NewGuid(), Guid.NewGuid(), "hashed-token", DateTime.UtcNow.AddDays(30));
            await repository.AddAsync(token);
            await context.SaveChangesAsync();

            // Act
            token.Revoke();
            await repository.UpdateAsync(token);
            await context.SaveChangesAsync();

            RefreshToken? retrieved = await repository.GetByTokenHashAsync("hashed-token");

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.IsRevoked.Should().BeTrue();
        }

        [Fact]
        public async Task RevokeAllActiveForUserAsyncShouldRevokeOnlyActiveTokensOfThatUser()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new RefreshTokenRepository(context);
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var activeToken = new RefreshToken(Guid.NewGuid(), userId, "hash-active", DateTime.UtcNow.AddDays(30));
            var alreadyRevokedToken = new RefreshToken(Guid.NewGuid(), userId, "hash-revoked", DateTime.UtcNow.AddDays(30));
            alreadyRevokedToken.Revoke();
            var otherUserToken = new RefreshToken(Guid.NewGuid(), otherUserId, "hash-other-user", DateTime.UtcNow.AddDays(30));

            await repository.AddAsync(activeToken);
            await repository.AddAsync(alreadyRevokedToken);
            await repository.AddAsync(otherUserToken);
            await context.SaveChangesAsync();

            // Act
            await repository.RevokeAllActiveForUserAsync(userId);
            await context.SaveChangesAsync();

            // Assert
            (await repository.GetByTokenHashAsync("hash-active"))!.IsRevoked.Should().BeTrue();
            (await repository.GetByTokenHashAsync("hash-other-user"))!.IsRevoked.Should().BeFalse();
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
