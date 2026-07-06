using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class YearEndClosingConfiguration : IEntityTypeConfiguration<YearEndClosing>
{
    public void Configure(EntityTypeBuilder<YearEndClosing> b)
    {
        b.ToTable("year_end_closings");
        b.HasKey(y => new { y.OrganizationId, y.Year });
        b.HasOne(y => y.ClosedBy).WithMany()
            .HasForeignKey(y => y.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(y => y.JournalEntry).WithMany()
            .HasForeignKey(y => y.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
    }
}
