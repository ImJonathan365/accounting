namespace Accounting.Domain.Entities;

public class AccountingPeriod
{
    public Guid   OrganizationId { get; set; }
    public int    Year           { get; set; }
    public int    Month          { get; set; }   // 1-12
    public DateTime ClosedAtUtc  { get; set; } = DateTime.UtcNow;
    public Guid   ClosedByUserId { get; set; }

    public User ClosedBy { get; set; } = default!;
}
