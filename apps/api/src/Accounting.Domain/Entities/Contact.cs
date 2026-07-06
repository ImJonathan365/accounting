using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class Contact
{
    public Guid        Id             { get; set; } = Guid.NewGuid();
    public Guid        OrganizationId { get; set; }
    public ContactType Type           { get; set; }
    public string      Name           { get; set; } = string.Empty;
    public string?     Email          { get; set; }
    public string?     Phone          { get; set; }
    public string?     Address        { get; set; }
    public string?     Notes          { get; set; }
    public bool        IsActive       { get; set; } = true;

    public List<Invoice> Invoices { get; set; } = [];
}
