using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> b)
    {
        b.ToTable("external_logins");
        b.HasKey(e => e.Id);
        b.Property(e => e.Provider).HasConversion<int>().IsRequired();
        b.Property(e => e.ProviderUserId).HasMaxLength(256).IsRequired();
        b.Property(e => e.PasswordHash).IsRequired(false);

        // Each (provider, provider_user_id) pair must be unique across all users
        b.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();

        b.HasOne(e => e.User).WithMany(u => u.ExternalLogins)
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
