namespace Accounting.Domain.Entities;

public class Budget
{
    public Guid   Id             { get; set; } = Guid.NewGuid();
    public Guid   OrganizationId { get; set; }
    public int    Year           { get; set; }
    public string Name           { get; set; } = string.Empty;
    public bool   IsActive       { get; set; } = true;

    public List<BudgetLine> Lines { get; set; } = [];
}
