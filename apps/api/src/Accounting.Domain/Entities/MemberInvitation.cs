namespace Accounting.Domain.Entities;

public class MemberInvitation
{
    public Guid     Id              { get; set; } = Guid.NewGuid();
    public Guid     OrganizationId  { get; set; }
    public Guid     InvitedByUserId { get; set; }
    public string   InvitedEmail    { get; set; } = "";
    public string   Role            { get; set; } = "member";
    public string   TokenHash       { get; set; } = "";
    public DateTime ExpiresAtUtc    { get; set; }
    public DateTime CreatedAtUtc    { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAtUtc  { get; set; }
    public DateTime? DeclinedAtUtc  { get; set; }

    public bool IsExpired => ExpiresAtUtc < DateTime.UtcNow;
    public bool IsPending => AcceptedAtUtc is null && DeclinedAtUtc is null && !IsExpired;
}
