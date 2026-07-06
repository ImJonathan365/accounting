namespace Accounting.Domain.Entities;

public class InvoicePayment
{
    public Guid     Id               { get; set; } = Guid.NewGuid();
    public Guid     InvoiceId        { get; set; }
    public DateOnly Date             { get; set; }
    public decimal  Amount           { get; set; }
    public Guid     PaymentAccountId { get; set; }
    public Guid?    JournalEntryId   { get; set; }
    public string?  Notes            { get; set; }

    public Invoice       Invoice        { get; set; } = null!;
    public Account       PaymentAccount { get; set; } = null!;
    public JournalEntry? JournalEntry   { get; set; }
}
