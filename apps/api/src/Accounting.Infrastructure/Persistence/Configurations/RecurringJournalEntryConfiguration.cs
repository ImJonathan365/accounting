using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class RecurringJournalEntryConfiguration : IEntityTypeConfiguration<RecurringJournalEntry>
{
    public void Configure(EntityTypeBuilder<RecurringJournalEntry> b)
    {
        b.ToTable("recurring_journal_entries");
        b.HasKey(r => r.Id);
        b.Property(r => r.Description).HasMaxLength(300).IsRequired();
        b.Property(r => r.Reference).HasMaxLength(100);
        b.Property(r => r.Frequency).HasConversion<int>();
        b.HasOne(r => r.Organization).WithMany()
            .HasForeignKey(r => r.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(r => r.Lines).WithOne(l => l.Entry)
            .HasForeignKey(l => l.RecurringEntryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RecurringJournalLineConfiguration : IEntityTypeConfiguration<RecurringJournalLine>
{
    public void Configure(EntityTypeBuilder<RecurringJournalLine> b)
    {
        b.ToTable("recurring_journal_lines");
        b.HasKey(l => l.Id);
        b.Property(l => l.Debit).HasPrecision(18, 4);
        b.Property(l => l.Credit).HasPrecision(18, 4);
        b.Property(l => l.Note).HasMaxLength(300);
        b.HasOne(l => l.Account).WithMany()
            .HasForeignKey(l => l.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
