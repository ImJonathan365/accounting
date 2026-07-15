using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public Guid?   LastOrgId { get; set; }
    public Guid    SecurityStamp { get; set; } = Guid.NewGuid();
    public DateTime? EmailVerifiedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
}
