using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class Membership : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public string Role { get; set; } = "member"; // owner | admin | member
}
