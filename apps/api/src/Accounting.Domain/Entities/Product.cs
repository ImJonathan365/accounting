namespace Accounting.Domain.Entities;

public class Product
{
    public Guid    Id             { get; set; } = Guid.NewGuid();
    public Guid    OrganizationId { get; set; }
    public string  Name          { get; set; } = string.Empty;
    public string? Description   { get; set; }
    public decimal DefaultPrice  { get; set; }
    public Guid    AccountId     { get; set; }
    public Guid?   TaxRateId     { get; set; }
    public bool    IsActive      { get; set; } = true;

    public Organization Organization { get; set; } = null!;
    public Account      Account      { get; set; } = null!;
    public TaxRate?     TaxRate      { get; set; }
}
