using Accounting.Domain.Entities;

namespace Accounting.Domain.Entities;

public class BankAccount
{
    public Guid   Id             { get; set; } = Guid.NewGuid();
    public Guid   OrganizationId { get; set; }
    public string Name           { get; set; } = string.Empty;
    public string? BankName      { get; set; }
    public string? AccountNumber { get; set; }
    public Guid   LinkedAccountId { get; set; }
    public bool   IsActive       { get; set; } = true;

    public Account              LinkedAccount { get; set; } = null!;
    public List<BankTransaction> Transactions { get; set; } = [];
}
