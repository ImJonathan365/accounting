namespace Accounting.Domain.Entities;

public class RecurringJournalLine
{
    public Guid   Id                  { get; set; } = Guid.NewGuid();
    public Guid   RecurringEntryId    { get; set; }
    public RecurringJournalEntry Entry { get; set; } = default!;
    public Guid   AccountId           { get; set; }
    public Account Account            { get; set; } = default!;
    public decimal Debit              { get; set; }
    public decimal Credit             { get; set; }
    public string? Note               { get; set; }
}
