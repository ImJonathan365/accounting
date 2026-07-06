using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class RecurringJournalEntry
{
    public Guid   Id             { get; set; } = Guid.NewGuid();
    public Guid   OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public string  Description { get; set; } = default!;
    public string? Reference   { get; set; }

    public RecurringFrequency Frequency { get; set; }
    public DateOnly NextDate            { get; set; }
    public DateOnly? EndDate            { get; set; }
    public bool IsActive                { get; set; } = true;
    public DateTime CreatedAtUtc        { get; set; } = DateTime.UtcNow;

    public ICollection<RecurringJournalLine> Lines { get; set; } = new List<RecurringJournalLine>();
}
