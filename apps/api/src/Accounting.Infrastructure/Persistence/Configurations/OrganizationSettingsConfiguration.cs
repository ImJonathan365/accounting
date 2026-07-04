using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationSettings> b)
    {
        b.ToTable("organization_settings");
        b.HasKey(s => s.Id);
        b.HasIndex(s => s.OrganizationId).IsUnique();

        b.Property(s => s.CompanyName).HasMaxLength(200).IsRequired();
        b.Property(s => s.LogoUrl).HasMaxLength(500);
        b.Property(s => s.Address).HasMaxLength(300);
        b.Property(s => s.TaxId).HasMaxLength(50);
        b.Property(s => s.Phone).HasMaxLength(50);
        b.Property(s => s.Email).HasMaxLength(200);
        b.Property(s => s.CurrencySymbol).HasMaxLength(10).IsRequired();
        b.Property(s => s.Theme).HasConversion<int>();

        b.HasOne(s => s.Organization).WithOne()
            .HasForeignKey<OrganizationSettings>(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
