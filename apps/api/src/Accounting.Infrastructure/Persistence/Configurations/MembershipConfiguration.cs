using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> b)
    {
        b.ToTable("memberships");
        b.HasKey(m => m.Id);
        b.Property(m => m.Role).HasMaxLength(20).IsRequired();
        b.HasIndex(m => new { m.UserId, m.OrganizationId }).IsUnique();
        b.HasOne(m => m.User).WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(m => m.Organization).WithMany(o => o.Memberships)
            .HasForeignKey(m => m.OrganizationId).OnDelete(DeleteBehavior.Cascade);
    }
}
