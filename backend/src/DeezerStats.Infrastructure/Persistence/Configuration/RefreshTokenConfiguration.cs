using DeezerStats.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeezerStats.Infrastructure.Persistence.Configuration
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            // Recherche par hash lors d'un refresh (POST /auth/refresh) : doit être unique et
            // indexée, comme ITrackRepository.GetByIsrcAsync sur Track.Isrc.
            builder.HasIndex(t => t.TokenHash).IsUnique();

            builder.HasIndex(t => t.UserId);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
