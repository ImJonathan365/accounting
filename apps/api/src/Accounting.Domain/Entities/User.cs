using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
