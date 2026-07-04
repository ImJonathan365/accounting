using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class JournalLine : BaseEntity
{
    public Guid JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = default!;

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = default!;

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Note { get; set; }
}
