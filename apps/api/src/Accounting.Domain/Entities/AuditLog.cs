using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId         { get; set; }
    public User? User          { get; set; }

    public string  Action     { get; set; } = null!;
    public string  EntityType { get; set; } = null!;
    public Guid    EntityId   { get; set; }
    public string? Details    { get; set; }
}

public static class AuditActions
{
    public const string JournalDraftCreated = "journal.draft_created";
    public const string JournalPostedCreated = "journal.posted_created";
    public const string JournalUpdated      = "journal.updated";
    public const string JournalPosted       = "journal.posted";
    public const string JournalVoided       = "journal.voided";
    public const string JournalDeleted      = "journal.deleted";
    public const string AccountCreated      = "account.created";
    public const string AccountUpdated      = "account.updated";
    public const string AccountToggled      = "account.toggled";
    public const string MemberInvited       = "member.invited";
    public const string MemberRoleChanged   = "member.role_changed";
    public const string MemberRemoved       = "member.removed";
}
