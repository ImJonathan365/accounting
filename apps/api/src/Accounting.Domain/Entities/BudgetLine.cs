namespace Accounting.Domain.Entities;

public class BudgetLine
{
    public Guid    Id        { get; set; } = Guid.NewGuid();
    public Guid    BudgetId  { get; set; }
    public Guid    AccountId { get; set; }
    public int     Month     { get; set; }
    public decimal Amount    { get; set; }

    public Budget  Budget  { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
