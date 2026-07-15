using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> b)
    {
        b.ToTable("tax_rates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Rate).HasPrecision(8, 4);

        b.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();

        b.HasOne(x => x.TaxAccount)
            .WithMany()
            .HasForeignKey(x => x.TaxAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("products");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.DefaultPrice).HasPrecision(18, 4);

        b.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();

        b.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TaxRate)
            .WithMany(t => t.Products)
            .HasForeignKey(x => x.TaxRateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
