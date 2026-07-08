using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Accounting.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<JournalEntry>         JournalEntries         => Set<JournalEntry>();
    public DbSet<JournalLine>          JournalLines           => Set<JournalLine>();
    public DbSet<OrganizationSettings> OrganizationSettings   => Set<OrganizationSettings>();
    public DbSet<AuditLog>             AuditLogs              => Set<AuditLog>();
    public DbSet<RefreshToken>              RefreshTokens              => Set<RefreshToken>();
    public DbSet<PasswordResetToken>        PasswordResetTokens        => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken>    EmailVerificationTokens    => Set<EmailVerificationToken>();
    public DbSet<AccountingPeriod>       AccountingPeriods       => Set<AccountingPeriod>();
    public DbSet<YearEndClosing>         YearEndClosings         => Set<YearEndClosing>();
    public DbSet<RecurringJournalEntry>  RecurringJournalEntries => Set<RecurringJournalEntry>();
    public DbSet<RecurringJournalLine>   RecurringJournalLines   => Set<RecurringJournalLine>();
    public DbSet<BankAccount>     BankAccounts     => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<Contact>         Contacts         => Set<Contact>();
    public DbSet<Invoice>         Invoices         => Set<Invoice>();
    public DbSet<InvoiceLine>     InvoiceLines     => Set<InvoiceLine>();
    public DbSet<InvoicePayment>  InvoicePayments  => Set<InvoicePayment>();
    public DbSet<Budget>          Budgets          => Set<Budget>();
    public DbSet<BudgetLine>      BudgetLines      => Set<BudgetLine>();
    public DbSet<TaxRate>          TaxRates          => Set<TaxRate>();
    public DbSet<Product>          Products          => Set<Product>();
    public DbSet<MemberInvitation> MemberInvitations => Set<MemberInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
