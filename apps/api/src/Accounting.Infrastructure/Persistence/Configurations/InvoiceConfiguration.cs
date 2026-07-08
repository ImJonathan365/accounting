using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("invoices");
        b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(50).IsRequired();
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasOne(x => x.Contact)
            .WithMany(c => c.Invoices)
            .HasForeignKey(x => x.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ArApAccount)
            .WithMany()
            .HasForeignKey(x => x.ArApAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.JournalEntry)
            .WithMany()
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> b)
    {
        b.ToTable("invoice_lines");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.UnitPrice).HasPrecision(18, 4);

        b.HasOne(x => x.Invoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TaxRate)
            .WithMany(t => t.Lines)
            .HasForeignKey(x => x.TaxRateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> b)
    {
        b.ToTable("invoice_payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasOne(x => x.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.PaymentAccount)
            .WithMany()
            .HasForeignKey(x => x.PaymentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.JournalEntry)
            .WithMany()
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
