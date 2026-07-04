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
    private readonly IJournalRepository  _journalRepo  = Substitute.For<IJournalRepository>();
    private readonly IAccountRepository  _accountRepo  = Substitute.For<IAccountRepository>();
    private readonly IValidator<CreateJournalEntryDto> _validator = Substitute.For<IValidator<CreateJournalEntryDto>>();
    private readonly JournalService _sut;

    private static readonly Guid OrgId  = Guid.NewGuid();
    private static readonly Guid BankId = Guid.NewGuid();
    private static readonly Guid IncId  = Guid.NewGuid();

    public JournalServiceTests()
    {
        _sut = new JournalService(_journalRepo, _accountRepo, _validator);

        // Validator passes by default
        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
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

        _journalRepo.GetByOrganizationAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<JournalEntry> { entry });

        var result = await _sut.ListAsync(OrgId);

        result.Should().HaveCount(1);
        result[0].TotalDebit.Should().Be(500m);
    }

    private static Account MakeAccount(Guid id, string code, string name,
        AccountType type, bool postable = true) =>
        new() { Id = id, Code = code, Name = name, Type = type, IsPostable = postable };

    private static CreateJournalEntryDto MakeDto(params (Guid accountId, decimal debit, decimal credit)[] lines) =>
        new(DateOnly.FromDateTime(DateTime.Today),
            "Test entry", null,
            lines.Select(l => new CreateJournalLineDto(l.accountId, l.debit, l.credit, null)).ToList());
}
