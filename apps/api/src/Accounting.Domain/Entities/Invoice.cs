using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class Invoice
{
    public Guid          Id             { get; set; } = Guid.NewGuid();
    public Guid          OrganizationId { get; set; }
    public InvoiceType   Type           { get; set; }
    public Guid          ContactId      { get; set; }
    public string        Number         { get; set; } = string.Empty;
    public DateOnly      Date           { get; set; }
    public DateOnly      DueDate        { get; set; }
    public InvoiceStatus Status         { get; set; } = InvoiceStatus.Draft;
    public Guid          ArApAccountId  { get; set; }
    public string?       Notes          { get; set; }
    public Guid?         JournalEntryId { get; set; }

    public Contact             Contact      { get; set; } = null!;
    public Account             ArApAccount  { get; set; } = null!;
    public JournalEntry?       JournalEntry { get; set; }
    public List<InvoiceLine>   Lines        { get; set; } = [];
    public List<InvoicePayment> Payments    { get; set; } = [];
}
