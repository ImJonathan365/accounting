using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> b)
    {
        b.ToTable("journal_entries");
        b.HasKey(e => e.Id);
        b.Property(e => e.Description).HasMaxLength(500).IsRequired();
        b.Property(e => e.Reference).HasMaxLength(100).IsRequired(false);
        b.Property(e => e.Status).HasConversion<int>();

        // Void fields
        b.Property(e => e.VoidReason).HasMaxLength(500).IsRequired(false);
        b.Property(e => e.VoidedAtUtc).IsRequired(false);
        b.Property(e => e.VoidsEntryId).IsRequired(false);
        b.Property(e => e.VoidedByEntryId).IsRequired(false);

        // Self-referential FKs for void relationships (Restrict to avoid cascade cycles)
        b.HasOne<JournalEntry>().WithMany()
            .HasForeignKey(e => e.VoidsEntryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        b.HasOne<JournalEntry>().WithMany()
            .HasForeignKey(e => e.VoidedByEntryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        b.HasOne(e => e.Organization).WithMany(o => o.JournalEntries)
            .HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Cascade);

        b.HasMany(e => e.Lines).WithOne(l => l.JournalEntry)
            .HasForeignKey(l => l.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
    }
}
