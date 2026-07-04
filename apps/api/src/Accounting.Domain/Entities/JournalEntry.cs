using Accounting.Domain.Common;
using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class JournalEntry : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public DateOnly Date { get; set; }
    public string Description { get; set; } = default!;
    public string? Reference { get; set; }
    public JournalStatus Status { get; set; } = JournalStatus.Posted;

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();

    // Void support
    /// <summary>If this entry reverses another, points to the original entry.</summary>
    public Guid? VoidsEntryId    { get; set; }

    /// <summary>If this entry has been voided, points to the counter-entry that reversed it.</summary>
    public Guid? VoidedByEntryId { get; set; }

    public string?   VoidReason   { get; set; }
    public DateTime? VoidedAtUtc  { get; set; }
}
