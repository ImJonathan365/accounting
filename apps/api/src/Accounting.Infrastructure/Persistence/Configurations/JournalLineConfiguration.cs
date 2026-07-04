using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> b)
    {
        b.ToTable("journal_lines");
        b.HasKey(l => l.Id);
        b.Property(l => l.Debit).HasPrecision(18, 2);
        b.Property(l => l.Credit).HasPrecision(18, 2);
        b.Property(l => l.Note).HasMaxLength(250).IsRequired(false);

        b.HasOne(l => l.Account).WithMany()
            .HasForeignKey(l => l.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
