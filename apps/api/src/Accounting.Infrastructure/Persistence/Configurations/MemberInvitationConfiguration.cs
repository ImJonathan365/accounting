using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class MemberInvitationConfiguration : IEntityTypeConfiguration<MemberInvitation>
{
    public void Configure(EntityTypeBuilder<MemberInvitation> b)
    {
        b.ToTable("member_invitations");
        b.HasKey(i => i.Id);
        b.Property(i => i.InvitedEmail).HasMaxLength(256).IsRequired();
        b.Property(i => i.Role).HasMaxLength(50).IsRequired();
        b.Property(i => i.TokenHash).HasMaxLength(128).IsRequired();
        b.HasIndex(i => i.TokenHash).IsUnique();
        b.HasIndex(i => new { i.OrganizationId, i.InvitedEmail });
    }
}
