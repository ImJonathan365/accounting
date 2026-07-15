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

    public const string InvoiceCreated      = "invoice.created";
    public const string InvoiceIssued       = "invoice.issued";
    public const string InvoicePaymentAdded = "invoice.payment_added";
    public const string InvoiceVoided       = "invoice.voided";

    public const string BankTxMatched   = "bank.tx_matched";
    public const string BankTxExcluded  = "bank.tx_excluded";
    public const string BankTxUnmatched = "bank.tx_unmatched";
    public const string BankImported    = "bank.imported";

    public const string PeriodClosed   = "period.closed";
    public const string PeriodReopened = "period.reopened";

    public const string YearEndClosed = "yearend.closed";

    public const string RecurringGenerated = "recurring.generated";

    public const string BudgetCreated      = "budget.created";
    public const string BudgetLineUpserted = "budget.line_upserted";

    public const string ContactCreated     = "contact.created";
    public const string ContactUpdated     = "contact.updated";

    public const string TaxRateCreated     = "taxrate.created";
    public const string TaxRateUpdated     = "taxrate.updated";

    public const string ProductCreated     = "product.created";
    public const string ProductUpdated     = "product.updated";

    public const string OrgSettingsUpdated = "orgsettings.updated";
    public const string OpeningBalancesSet = "openingbalances.set";
}
