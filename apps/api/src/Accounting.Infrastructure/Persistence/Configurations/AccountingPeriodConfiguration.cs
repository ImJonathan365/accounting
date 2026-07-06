using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> b)
    {
        b.ToTable("accounting_periods");
        b.HasKey(p => new { p.OrganizationId, p.Year, p.Month });

        b.HasOne(p => p.ClosedBy).WithMany()
            .HasForeignKey(p => p.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Organization>().WithMany()
            .HasForeignKey(p => p.OrganizationId).OnDelete(DeleteBehavior.Cascade);
    }
}
