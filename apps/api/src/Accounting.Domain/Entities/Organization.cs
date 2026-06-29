using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = default!;
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}
