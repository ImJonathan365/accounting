namespace Accounting.Domain.Entities;

public class TaxRate
{
    public Guid    Id             { get; set; } = Guid.NewGuid();
    public Guid    OrganizationId { get; set; }
    public string  Name           { get; set; } = string.Empty;
    public decimal Rate           { get; set; }
    public Guid    TaxAccountId   { get; set; }
    public bool    IsActive       { get; set; } = true;

    public Organization      Organization { get; set; } = null!;
    public Account           TaxAccount   { get; set; } = null!;
    public List<InvoiceLine> Lines        { get; set; } = new();
    public List<Product>     Products     { get; set; } = new();
}
