using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("accounts");
        b.HasKey(a => a.Id);
        b.Property(a => a.Code).HasMaxLength(30).IsRequired();
        b.Property(a => a.Name).HasMaxLength(150).IsRequired();
        b.Property(a => a.Type).HasConversion<int>();
        b.HasIndex(a => new { a.OrganizationId, a.Code }).IsUnique();
        b.HasOne(a => a.Parent).WithMany(a => a.Children)
            .HasForeignKey(a => a.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Organization).WithMany(o => o.Accounts)
            .HasForeignKey(a => a.OrganizationId).OnDelete(DeleteBehavior.Cascade);
    }
}
