namespace Accounting.Domain.Entities;

public class EmailVerificationToken
{
    public Guid     Id           { get; set; } = Guid.NewGuid();
    public Guid     UserId       { get; set; }
    public string   TokenHash    { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
