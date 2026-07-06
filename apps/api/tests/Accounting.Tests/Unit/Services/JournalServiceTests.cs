using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class JournalServiceTests
{
    private readonly IJournalRepository            _journalRepo  = Substitute.For<IJournalRepository>();
    private readonly IAccountRepository            _accountRepo  = Substitute.For<IAccountRepository>();
    private readonly IAccountingPeriodRepository   _periods      = Substitute.For<IAccountingPeriodRepository>();
    private readonly IValidator<CreateJournalEntryDto> _validator       = Substitute.For<IValidator<CreateJournalEntryDto>>();
    private readonly IValidator<UpdateJournalEntryDto> _updateValidator = Substitute.For<IValidator<UpdateJournalEntryDto>>();
    private readonly IValidator<VoidJournalEntryDto>   _voidValidator   = Substitute.For<IValidator<VoidJournalEntryDto>>();
    private readonly JournalService _sut;

    private static readonly Guid OrgId  = Guid.NewGuid();
    private static readonly Guid BankId = Guid.NewGuid();
    private static readonly Guid IncId  = Guid.NewGuid();

    public JournalServiceTests()
    {
        _sut = new JournalService(_journalRepo, _accountRepo, _periods, _validator, _updateValidator, _voidValidator);

        // Default: all periods open
        _periods.IsClosedAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _updateValidator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _voidValidator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
    }

    [Fact]
    public async Task CreateAsync_ValidEntry_CreatesAndReturnsDto()
    {
        var bankAccount = MakeAccount(BankId, "1.1.02", "Banco",              AccountType.Asset,  postable: true);
        var incAccount  = MakeAccount(IncId,  "4.1",    "Ingresos por ventas", AccountType.Income, postable: true);

        _accountRepo.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { bankAccount, incAccount });

        JournalEntry? captured = null;
        await _journalRepo.AddAsync(Arg.Do<JournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        var dto = MakeDto(
            (BankId, 1000m, 0m),
            (IncId,  0m,    1000m));

        var result = await _sut.CreateAsync(OrgId, dto);

        result.TotalDebit.Should().Be(1000m);
        result.TotalCredit.Should().Be(1000m);
        result.Lines.Should().HaveCount(2);
        result.Status.Should().Be(JournalStatus.Posted);
        captured.Should().NotBeNull();
        captured!.OrganizationId.Should().Be(OrgId);
    }

    [Fact]
    public async Task CreateAsync_AccountNotInOrg_ThrowsInvalidOperation()
    {
        _accountRepo.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { MakeAccount(BankId, "1.1.02", "Banco", AccountType.Asset) });

        var dto = MakeDto((BankId, 500m, 0m), (Guid.NewGuid(), 0m, 500m));

        await _sut.Invoking(s => s.CreateAsync(OrgId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no existen en esta organización*");
    }

    [Fact]
    public async Task CreateAsync_InactiveAccount_ThrowsInvalidOperation()
    {
        var inactiveBank = MakeAccount(BankId, "1.1.02", "Banco", AccountType.Asset, postable: true);
        inactiveBank.IsActive = false;
        var incAccount   = MakeAccount(IncId,  "4.1",    "Ingresos", AccountType.Income, postable: true);

        _accountRepo.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { inactiveBank, incAccount });

        var dto = MakeDto((BankId, 500m, 0m), (IncId, 0m, 500m));

        await _sut.Invoking(s => s.CreateAsync(OrgId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*están inactivas*");
    }

    [Fact]
    public async Task CreateAsync_NonPostableAccount_ThrowsInvalidOperation()
    {
        var headerAccount = MakeAccount(BankId, "1",    "Activos", AccountType.Asset,  postable: false);
        var incAccount    = MakeAccount(IncId,  "4.1",  "Ingresos", AccountType.Income, postable: true);

        _accountRepo.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { headerAccount, incAccount });

        var dto = MakeDto((BankId, 500m, 0m), (IncId, 0m, 500m));

        await _sut.Invoking(s => s.CreateAsync(OrgId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no admiten movimientos*");
    }

    [Fact]
    public async Task CreateAsync_TrimsDescriptionAndReference()
    {
        var bank = MakeAccount(BankId, "1.1.02", "Banco",  AccountType.Asset,  postable: true);
        var inc  = MakeAccount(IncId,  "4.1",    "Ingreso", AccountType.Income, postable: true);

        _accountRepo.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { bank, inc });

        JournalEntry? captured = null;
        await _journalRepo.AddAsync(Arg.Do<JournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        var dto = new CreateJournalEntryDto(
            DateOnly.FromDateTime(DateTime.Today),
            "  Pago de servicios  ",
            "  REF-001  ",
            new List<CreateJournalLineDto>
            {
                new(BankId, 100m, 0m, null),
                new(IncId,  0m,  100m, null)
            });

        await _sut.CreateAsync(OrgId, dto);

        captured!.Description.Should().Be("Pago de servicios");
        captured.Reference.Should().Be("REF-001");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _journalRepo.GetByIdAsync(Arg.Any<Guid>(), OrgId, Arg.Any<CancellationToken>())
            .Returns((JournalEntry?)null);

        await _sut.Invoking(s => s.GetByIdAsync(Guid.NewGuid(), OrgId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedSummaries()
    {
        var bank = MakeAccount(BankId, "1.1.02", "Banco",  AccountType.Asset,  postable: true);
        var inc  = MakeAccount(IncId,  "4.1",    "Ingreso", AccountType.Income, postable: true);

        var entry = new JournalEntry
        {
            Id             = Guid.NewGuid(),
            OrganizationId = OrgId,
            Date           = DateOnly.FromDateTime(DateTime.Today),
            Description    = "Cobro",
            Status         = JournalStatus.Posted,
            Lines          = new List<JournalLine>
            {
                new() { AccountId = BankId, Account = bank, Debit = 500m, Credit = 0m },
                new() { AccountId = IncId,  Account = inc,  Debit = 0m,   Credit = 500m }
            }
        };

        _journalRepo.GetPagedAsync(
                OrgId, 1, 25,
                Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(),
                Arg.Any<JournalStatus?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((new List<JournalEntry> { entry }, 1));

        var result = await _sut.ListAsync(OrgId, 1, 25);

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
        result.Items[0].TotalDebit.Should().Be(500m);
    }

    [Fact]
    public async Task VoidAsync_PostedEntry_VoidsOriginalAndCreatesCounterEntry()
    {
        var entryId  = Guid.NewGuid();
        var original = MakePostedEntry(entryId);

        _journalRepo.GetByIdTrackedAsync(entryId, OrgId, Arg.Any<CancellationToken>())
            .Returns(original);

        JournalEntry? captured = null;
        await _journalRepo.AddAsync(Arg.Do<JournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        var result = await _sut.VoidAsync(OrgId, entryId, new VoidJournalEntryDto("Error en el monto", null));

        original.Status.Should().Be(JournalStatus.Voided);
        original.VoidReason.Should().Be("Error en el monto");
        original.VoidedAtUtc.Should().NotBeNull();
        original.VoidedByEntryId.Should().NotBeNull();

        captured.Should().NotBeNull();
        captured!.OrganizationId.Should().Be(OrgId);
        captured.Status.Should().Be(JournalStatus.Posted);
        captured.VoidsEntryId.Should().Be(entryId);
        captured.Description.Should().StartWith("ANULACIÓN:");

        var bankLine = captured.Lines.First(l => l.AccountId == BankId);
        var incLine  = captured.Lines.First(l => l.AccountId == IncId);
        bankLine.Debit.Should().Be(0m);
        bankLine.Credit.Should().Be(500m);
        incLine.Debit.Should().Be(500m);
        incLine.Credit.Should().Be(0m);

        original.VoidedByEntryId.Should().Be(captured.Id);

        await _journalRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        result.Id.Should().Be(entryId);
        result.Status.Should().Be(JournalStatus.Voided);
    }

    [Fact]
    public async Task VoidAsync_CustomVoidDate_CounterEntryUsesProvidedDate()
    {
        var entryId    = Guid.NewGuid();
        var customDate = new DateOnly(2026, 3, 15);

        _journalRepo.GetByIdTrackedAsync(entryId, OrgId, Arg.Any<CancellationToken>())
            .Returns(MakePostedEntry(entryId));

        JournalEntry? captured = null;
        await _journalRepo.AddAsync(Arg.Do<JournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.VoidAsync(OrgId, entryId, new VoidJournalEntryDto(null, customDate));

        captured!.Date.Should().Be(customDate);
    }

    [Fact]
    public async Task VoidAsync_NullVoidDate_CounterEntryUsesToday()
    {
        var entryId = Guid.NewGuid();

        _journalRepo.GetByIdTrackedAsync(entryId, OrgId, Arg.Any<CancellationToken>())
            .Returns(MakePostedEntry(entryId));

        JournalEntry? captured = null;
        await _journalRepo.AddAsync(Arg.Do<JournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.VoidAsync(OrgId, entryId, new VoidJournalEntryDto(null, null));

        captured!.Date.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }

    [Fact]
    public async Task VoidAsync_ReasonWithWhitespace_GetsStoredTrimmed()
    {
        var entryId  = Guid.NewGuid();
        var original = MakePostedEntry(entryId);

        _journalRepo.GetByIdTrackedAsync(entryId, OrgId, Arg.Any<CancellationToken>())
            .Returns(original);

        await _sut.VoidAsync(OrgId, entryId, new VoidJournalEntryDto("  Error tipográfico  ", null));

        original.VoidReason.Should().Be("Error tipográfico");
    }

    [Fact]
    public async Task VoidAsync_EntryNotFound_ThrowsKeyNotFoundException()
    {
        _journalRepo.GetByIdTrackedAsync(Arg.Any<Guid>(), OrgId, Arg.Any<CancellationToken>())
            .Returns((JournalEntry?)null);

        await _sut.Invoking(s => s.VoidAsync(OrgId, Guid.NewGuid(), new VoidJournalEntryDto(null, null)))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task VoidAsync_AlreadyVoidedEntry_ThrowsInvalidOperation()
    {
        var entryId  = Guid.NewGuid();
        var original = MakePostedEntry(entryId);
        original.Status = JournalStatus.Voided;

        _journalRepo.GetByIdTrackedAsync(entryId, OrgId, Arg.Any<CancellationToken>())
            .Returns(original);

        await _sut.Invoking(s => s.VoidAsync(OrgId, entryId, new VoidJournalEntryDto(null, null)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya fue anulado*");
    }

    [Fact]
    public async Task VoidAsync_DraftEntry_ThrowsInvalidOperation()
    {
        var entryId  = Guid.NewGuid();
        var original = MakePostedEntry(entryId);
        original.Status = JournalStatus.Draft;

        _journalRepo.GetByIdTrackedAsync(entryId, OrgId, Arg.Any<CancellationToken>())
            .Returns(original);

        await _sut.Invoking(s => s.VoidAsync(OrgId, entryId, new VoidJournalEntryDto(null, null)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Solo se pueden anular*");
    }

    private static Account MakeAccount(Guid id, string code, string name,
        AccountType type, bool postable = true) =>
        new() { Id = id, Code = code, Name = name, Type = type, IsPostable = postable };

    private static JournalEntry MakePostedEntry(Guid id)
    {
        var bank = MakeAccount(BankId, "1.1.02", "Banco",    AccountType.Asset);
        var inc  = MakeAccount(IncId,  "4.1",    "Ingresos", AccountType.Income);
        return new JournalEntry
        {
            Id             = id,
            OrganizationId = OrgId,
            Date           = new DateOnly(2026, 6, 1),
            Description    = "Venta de mercadería",
            Reference      = "REF-001",
            Status         = JournalStatus.Posted,
            Lines          = new List<JournalLine>
            {
                new() { AccountId = BankId, Account = bank, Debit = 500m, Credit = 0m   },
                new() { AccountId = IncId,  Account = inc,  Debit = 0m,   Credit = 500m }
            }
        };
    }

    private static CreateJournalEntryDto MakeDto(params (Guid accountId, decimal debit, decimal credit)[] lines) =>
        new(DateOnly.FromDateTime(DateTime.Today),
            "Test entry", null,
            lines.Select(l => new CreateJournalLineDto(l.accountId, l.debit, l.credit, null)).ToList());
}
