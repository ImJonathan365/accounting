using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class BankTransaction
{
    public Guid                  Id              { get; set; } = Guid.NewGuid();
    public Guid                  BankAccountId   { get; set; }
    public DateOnly              Date            { get; set; }
    public string                Description     { get; set; } = string.Empty;
    public decimal               Amount          { get; set; }
    public BankTransactionType   Type            { get; set; }
    public BankTransactionStatus Status          { get; set; } = BankTransactionStatus.Pending;
    public Guid?                 JournalEntryId  { get; set; }
    public string?               Notes           { get; set; }

    public BankAccount   BankAccount   { get; set; } = null!;
    public JournalEntry? JournalEntry  { get; set; }
}
