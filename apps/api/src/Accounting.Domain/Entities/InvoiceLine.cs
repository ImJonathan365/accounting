namespace Accounting.Domain.Entities;

public class InvoiceLine
{
    public Guid    Id          { get; set; } = Guid.NewGuid();
    public Guid    InvoiceId   { get; set; }
    public string  Description { get; set; } = string.Empty;
    public decimal Quantity    { get; set; } = 1;
    public decimal UnitPrice   { get; set; }
    public Guid    AccountId   { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
