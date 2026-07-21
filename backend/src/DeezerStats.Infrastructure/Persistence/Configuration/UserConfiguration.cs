using DeezerStats.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeezerStats.Infrastructure.Persistence.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Conversion du Value Object Email -> string
            builder.Property(u => u.Email)
                .HasConversion(DomainValueConverters.EmailConverter);

            builder.HasAlternateKey(u => u.Email);
        }
    }
}
