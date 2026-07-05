using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(a => a.Id);
        b.Property(a => a.Action).HasMaxLength(50).IsRequired();
        b.Property(a => a.EntityType).HasMaxLength(50).IsRequired();
        b.Property(a => a.Details).HasMaxLength(500).IsRequired(false);

        b.HasIndex(a => new { a.OrganizationId, a.CreatedAtUtc });

        b.HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Organization>().WithMany()
            .HasForeignKey(a => a.OrganizationId).OnDelete(DeleteBehavior.Cascade);
    }
}
