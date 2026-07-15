using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class InvoiceServiceTests
{
    private readonly IInvoiceRepository          _repo        = Substitute.For<IInvoiceRepository>();
    private readonly IContactRepository          _contacts    = Substitute.For<IContactRepository>();
    private readonly IAccountRepository          _accounts    = Substitute.For<IAccountRepository>();
    private readonly IJournalRepository          _journal     = Substitute.For<IJournalRepository>();
    private readonly ITaxRateRepository          _taxRates    = Substitute.For<ITaxRateRepository>();
    private readonly IJournalService             _journalSvc  = Substitute.For<IJournalService>();
    private readonly IAccountingPeriodRepository _periods     = Substitute.For<IAccountingPeriodRepository>();
    private readonly InvoiceService _sut;

    private static readonly Guid OrgId      = Guid.NewGuid();
    private static readonly Guid InvoiceId  = Guid.NewGuid();
    private static readonly Guid ArApId     = Guid.NewGuid();
    private static readonly Guid IncomeId   = Guid.NewGuid();
    private static readonly Guid ContactId  = Guid.NewGuid();
    private static readonly Guid BankAccId  = Guid.NewGuid();

    public InvoiceServiceTests()
    {
        _sut = new InvoiceService(_repo, _contacts, _accounts, _journal, _taxRates, _journalSvc, _periods);

        // Default: contact belongs to org
        _contacts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new Contact { Id = ContactId, Name = "Cliente Test", Type = ContactType.Customer });

        // Default: any account belongs to org (covers PaymentAccountId validation in RecordPaymentAsync)
        _accounts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(MakeAccount(BankAccId));

        // Default: GetByIdsAsync returns active/postable accounts for each requested id (covers M-8 in IssueAsync)
        _accounts.GetByIdsAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ((IEnumerable<Guid>)ci[1]).Select(MakeAccount).ToList());

        // Default: all periods open
        _periods.IsClosedAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Default: no tax rates
        _taxRates.GetByIdsAsync(Arg.Any<IList<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<TaxRate>());

        // Default: GetByIdReadOnlyAsync returns a fully navigated invoice for response mapping
        _repo.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(MakeReadonlyInvoice());
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NoLines_ThrowsArgumentException()
    {
        var dto = MakeCreateDto(lines: Array.Empty<CreateInvoiceLineDto>());

        await _sut.Invoking(s => s.CreateAsync(OrgId, dto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*al menos una línea*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateNumber_ThrowsInvalidOperation()
    {
        _repo.NumberExistsAsync(OrgId, "FAC-001", null, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.Invoking(s => s.CreateAsync(OrgId, MakeCreateDto()))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*FAC-001*");
    }

    [Fact]
    public async Task CreateAsync_AccountNotInOrg_ThrowsArgumentException()
    {
        _repo.NumberExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Returns only 1 account but we need 2 (line + arAp)
        _accounts.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { MakeAccount(ArApId) });

        await _sut.Invoking(s => s.CreateAsync(OrgId, MakeCreateDto()))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*no pertenecen*");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_PersistsInvoiceAndReturnsDto()
    {
        _repo.NumberExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _accounts.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { MakeAccount(ArApId), MakeAccount(IncomeId) });

        Invoice? saved = null;
        await _repo.AddAsync(Arg.Do<Invoice>(i => saved = i), Arg.Any<CancellationToken>());

        var result = await _sut.CreateAsync(OrgId, MakeCreateDto());

        saved.Should().NotBeNull();
        saved!.OrganizationId.Should().Be(OrgId);
        saved.Number.Should().Be("FAC-001");
        saved.Lines.Should().HaveCount(1);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
    }

    // ── IssueAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task IssueAsync_NotDraft_ThrowsInvalidOperation()
    {
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>())
            .Returns(MakeInvoice(InvoiceStatus.Issued));

        await _sut.Invoking(s => s.IssueAsync(OrgId, InvoiceId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*borrador*");
    }

    [Fact]
    public async Task IssueAsync_ClosedPeriod_ThrowsInvalidOperation()
    {
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>())
            .Returns(MakeInvoice(InvoiceStatus.Draft));

        _periods.IsClosedAsync(OrgId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.Invoking(s => s.IssueAsync(OrgId, InvoiceId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cerrado*");
    }

    [Fact]
    public async Task IssueAsync_ValidReceivable_CreatesDebitArApCreditIncomeEntry()
    {
        var inv = MakeInvoice(InvoiceStatus.Draft, InvoiceType.Receivable);
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>()).Returns(inv);

        JournalEntry? captured = null;
        await _journal.AddAsync(Arg.Do<JournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.IssueAsync(OrgId, InvoiceId);

        captured.Should().NotBeNull();
        var arLine  = captured!.Lines.First(l => l.AccountId == ArApId);
        var incLine = captured.Lines.First(l => l.AccountId == IncomeId);
        arLine.Debit.Should().Be(500m);
        arLine.Credit.Should().Be(0m);
        incLine.Debit.Should().Be(0m);
        incLine.Credit.Should().Be(500m);
        inv.Status.Should().Be(InvoiceStatus.Issued);
        inv.JournalEntryId.Should().Be(captured.Id);
    }

    [Fact]
    public async Task IssueAsync_ValidPayable_CreditArApDebitExpenseEntry()
    {
        var inv = MakeInvoice(InvoiceStatus.Draft, InvoiceType.Payable);
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>()).Returns(inv);

        JournalEntry? captured = null;
        await _journal.AddAsync(Arg.Do<JournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.IssueAsync(OrgId, InvoiceId);

        var arLine  = captured!.Lines.First(l => l.AccountId == ArApId);
        var expLine = captured.Lines.First(l => l.AccountId == IncomeId);
        arLine.Debit.Should().Be(0m);
        arLine.Credit.Should().Be(500m);
        expLine.Debit.Should().Be(500m);
        expLine.Credit.Should().Be(0m);
    }

    // ── RecordPaymentAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RecordPaymentAsync_DraftInvoice_ThrowsInvalidOperation()
    {
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>())
            .Returns(MakeInvoice(InvoiceStatus.Draft));

        await _sut.Invoking(s => s.RecordPaymentAsync(OrgId, InvoiceId, MakePaymentDto(500m)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*emitida*");
    }

    [Fact]
    public async Task RecordPaymentAsync_ClosedPeriod_ThrowsInvalidOperation()
    {
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>())
            .Returns(MakeInvoice(InvoiceStatus.Issued));

        _periods.IsClosedAsync(OrgId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.Invoking(s => s.RecordPaymentAsync(OrgId, InvoiceId, MakePaymentDto(500m)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cerrado*");
    }

    [Fact]
    public async Task RecordPaymentAsync_AmountExceedsBalance_ThrowsArgumentException()
    {
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>())
            .Returns(MakeInvoice(InvoiceStatus.Issued));

        await _sut.Invoking(s => s.RecordPaymentAsync(OrgId, InvoiceId, MakePaymentDto(9999m)))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*monto*");
    }

    [Fact]
    public async Task RecordPaymentAsync_FullPayment_SetsStatusPaid()
    {
        var inv = MakeInvoice(InvoiceStatus.Issued); // total = 500, balance = 500
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>()).Returns(inv);

        await _sut.RecordPaymentAsync(OrgId, InvoiceId, MakePaymentDto(500m));

        inv.Status.Should().Be(InvoiceStatus.Paid);
        await _repo.Received(1).AddPaymentAsync(Arg.Any<InvoicePayment>(), Arg.Any<CancellationToken>());
        await _journal.Received(1).AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordPaymentAsync_PartialPayment_SetsStatusPartiallyPaid()
    {
        var inv = MakeInvoice(InvoiceStatus.Issued); // total = 500
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>()).Returns(inv);

        await _sut.RecordPaymentAsync(OrgId, InvoiceId, MakePaymentDto(200m));

        inv.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public async Task RecordPaymentAsync_Receivable_DebitsBankCreditsArAp()
    {
        var inv = MakeInvoice(InvoiceStatus.Issued, InvoiceType.Receivable);
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>()).Returns(inv);

        JournalEntry? captured = null;
        await _journal.AddAsync(Arg.Do<JournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.RecordPaymentAsync(OrgId, InvoiceId, MakePaymentDto(500m));

        var bankLine = captured!.Lines.First(l => l.AccountId == BankAccId);
        var arLine   = captured.Lines.First(l => l.AccountId == ArApId);
        bankLine.Debit.Should().Be(500m);
        bankLine.Credit.Should().Be(0m);
        arLine.Debit.Should().Be(0m);
        arLine.Credit.Should().Be(500m);
    }

    // ── VoidAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task VoidAsync_AlreadyVoid_ThrowsInvalidOperation()
    {
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>())
            .Returns(MakeInvoice(InvoiceStatus.Void));

        await _sut.Invoking(s => s.VoidAsync(OrgId, InvoiceId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya está anulada*");
    }

    [Fact]
    public async Task VoidAsync_HasPayments_ThrowsInvalidOperation()
    {
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>())
            .Returns(MakeInvoice(InvoiceStatus.PartiallyPaid, hasPayment: true));

        await _sut.Invoking(s => s.VoidAsync(OrgId, InvoiceId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pagos registrados*");
    }

    [Fact]
    public async Task VoidAsync_DraftWithNoJournalEntry_SavesDirectly()
    {
        var inv = MakeInvoice(InvoiceStatus.Draft);
        inv.JournalEntryId = null;
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>()).Returns(inv);

        await _sut.VoidAsync(OrgId, InvoiceId);

        inv.Status.Should().Be(InvoiceStatus.Void);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _journalSvc.DidNotReceive().VoidAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<VoidJournalEntryDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VoidAsync_IssuedWithJournalEntry_DelegatesToJournalService()
    {
        var journalEntryId = Guid.NewGuid();
        var inv = MakeInvoice(InvoiceStatus.Issued);
        inv.JournalEntryId = journalEntryId;
        _repo.GetByIdAsync(OrgId, InvoiceId, Arg.Any<CancellationToken>()).Returns(inv);

        _journalSvc.VoidAsync(OrgId, journalEntryId, Arg.Any<VoidJournalEntryDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new JournalEntryDto(
                journalEntryId, new DateOnly(2026, 7, 1), "", null, JournalStatus.Voided, 0m, 0m,
                new List<JournalLineDto>(), DateTime.UtcNow, null, null, null, null)));

        await _sut.VoidAsync(OrgId, InvoiceId);

        inv.Status.Should().Be(InvoiceStatus.Void);
        await _journalSvc.Received(1).VoidAsync(
            OrgId, journalEntryId,
            Arg.Is<VoidJournalEntryDto>(d => d.Reason!.Contains("FAC-001")),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Invoice MakeInvoice(
        InvoiceStatus status = InvoiceStatus.Draft,
        InvoiceType   type   = InvoiceType.Receivable,
        bool          hasPayment = false)
    {
        var contact  = new Contact { Id = ContactId, Name = "Cliente Test", Type = ContactType.Customer };
        var arAp     = new Account { Id = ArApId,   Code = "1.1.03", Name = "Cuentas por cobrar", Type = AccountType.Asset };
        var income   = new Account { Id = IncomeId, Code = "4.1",    Name = "Ingresos",            Type = AccountType.Income };
        var bankAcc  = new Account { Id = BankAccId, Code = "1.1.01", Name = "Banco",              Type = AccountType.Asset };

        var payments = hasPayment
            ? new List<InvoicePayment> { new() { Amount = 200m, Date = new DateOnly(2026, 7, 1), PaymentAccount = bankAcc, PaymentAccountId = BankAccId } }
            : new List<InvoicePayment>();

        return new Invoice
        {
            Id             = InvoiceId,
            OrganizationId = OrgId,
            Type           = type,
            Status         = status,
            ContactId      = ContactId,
            Contact        = contact,
            Number         = "FAC-001",
            Date           = new DateOnly(2026, 7, 1),
            DueDate        = new DateOnly(2026, 7, 31),
            ArApAccountId  = ArApId,
            ArApAccount    = arAp,
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Id          = Guid.NewGuid(),
                    Description = "Servicio",
                    Quantity    = 1m,
                    UnitPrice   = 500m,
                    AccountId   = IncomeId,
                    Account     = income,
                }
            },
            Payments = payments,
        };
    }

    // Fully navigated invoice for read-only responses (Map requires Account/TaxRate/PaymentAccount)
    private static Invoice MakeReadonlyInvoice() => MakeInvoice();

    private static Account MakeAccount(Guid id) =>
        new() { Id = id, Code = "1.1.01", Name = "Cuenta Test", Type = AccountType.Asset };

    private static CreateInvoiceDto MakeCreateDto(
        IEnumerable<CreateInvoiceLineDto>? lines = null) =>
        new(
            InvoiceType.Receivable,
            ContactId,
            "FAC-001",
            "2026-07-01",
            "2026-07-31",
            ArApId,
            null,
            (lines ?? new[] { new CreateInvoiceLineDto("Servicio", 1m, 500m, IncomeId, null) }).ToList());

    private static CreatePaymentDto MakePaymentDto(decimal amount) =>
        new("2026-07-10", amount, BankAccId, null);
}
