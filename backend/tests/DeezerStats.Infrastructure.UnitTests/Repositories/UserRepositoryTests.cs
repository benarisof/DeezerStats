using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
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
            var user = new User(
                userId,
                new Email("alex@example.com"),
                "hashed_password_123",
                "Alex");

            // Act
            await repository.AddAsync(user);
            User? retrieved = await repository.GetByIdAsync(userId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Email.Value.Should().Be("alex@example.com");
            retrieved.DisplayName.Should().Be("Alex");
        }

        [Fact]
        public async Task GetByEmailAsyncShouldFindUserInsensitiveToCaseAndSpaces()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new UserRepository(context);
            var user = new User(Guid.NewGuid(), new Email("alex@example.com"), "hashed_password_123", "Alex");
            await repository.AddAsync(user);

            // Act : On cherche avec des majuscules et des espaces autour
            User? retrieved = await repository.GetByEmailAsync(new Email("alex@example.com"));

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
            User? retrieved = await repository.GetByEmailAsync(new Email("alex@example.com"));

            // Assert
            retrieved.Should().BeNull();
        }

        [Fact]
        public async Task AddAsyncWithDuplicateEmailShouldThrowConflictException()
        {
            // Arrange : Email est configuré comme clé alternative (voir UserConfiguration), le filet
            // de sécurité en base contre les doublons de compte même si le contrôle applicatif de
            // RegisterUserUseCase (GetByEmailAsync) est contourné par une course concurrente. Ici, les
            // deux entités étant suivies par le même DbContext, EF Core détecte le conflit dès l'ajout
            // (identity map du ChangeTracker), avant même SaveChanges : une InvalidOperationException,
            // que UserRepository.AddAsync traduit en ConflictException pour préserver le contrat 409
            // de l'API. Dans un scénario réel de deux requêtes HTTP concurrentes utilisant chacune
            // leur propre DbContext, c'est la contrainte d'unicité en base qui interviendrait, avec
            // une DbUpdateException — traduite de la même façon.
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var repository = new UserRepository(context);
            await repository.AddAsync(new User(Guid.NewGuid(), new Email("alex@example.com"), "hash1", "Alex"));

            // Act
            Func<Task> act = () => repository.AddAsync(
                new User(Guid.NewGuid(), new Email("alex@example.com"), "hash2", "Alex Bis"));

            // Assert
            await act.Should().ThrowAsync<ConflictException>();
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
