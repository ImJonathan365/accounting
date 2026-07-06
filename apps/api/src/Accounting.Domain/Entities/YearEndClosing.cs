namespace Accounting.Domain.Entities;

public class YearEndClosing
{
    public Guid   OrganizationId { get; set; }
    public int    Year           { get; set; }
    public Guid   JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = default!;
    public Guid   ClosedByUserId { get; set; }
    public User   ClosedBy       { get; set; } = default!;
    public DateTime ClosedAtUtc  { get; set; } = DateTime.UtcNow;
}
