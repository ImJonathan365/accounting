using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class BankServiceTests
{
    private readonly IBankRepository    _repo     = Substitute.For<IBankRepository>();
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IJournalRepository _journal  = Substitute.For<IJournalRepository>();
    private readonly BankService        _sut;

    private static readonly Guid OrgId         = Guid.NewGuid();
    private static readonly Guid BankAccountId = Guid.NewGuid();
    private static readonly Guid LinkedAccId   = Guid.NewGuid();
    private static readonly Guid TxId          = Guid.NewGuid();
    private static readonly Guid JournalId     = Guid.NewGuid();

    public BankServiceTests()
    {
        _sut = new BankService(_repo, _accounts, _journal);

        _repo.GetAccountAsync(OrgId, BankAccountId, Arg.Any<CancellationToken>())
            .Returns(MakeBankAccount());
        _repo.GetTransactionAsync(OrgId, TxId, Arg.Any<CancellationToken>())
            .Returns(MakeTransaction());
        _accounts.GetByIdAsync(LinkedAccId, OrgId, Arg.Any<CancellationToken>())
            .Returns(MakeAccount());
        _repo.GetPendingCountsAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());
    }

    [Fact]
    public async Task CreateAccountAsync_LinkedAccountNotFound_ThrowsKeyNotFoundException()
    {
        _accounts.GetByIdAsync(Arg.Any<Guid>(), OrgId, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        await _sut.Invoking(s => s.CreateAccountAsync(OrgId, new("Main Bank", "BankA", "001", Guid.NewGuid())))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*cuenta contable no existe*");
    }

    [Fact]
    public async Task CreateAccountAsync_Valid_CreatesAndReturns()
    {
        BankAccount? captured = null;
        await _repo.AddAccountAsync(Arg.Do<BankAccount>(a => captured = a), Arg.Any<CancellationToken>());

        await _sut.CreateAccountAsync(OrgId, new("Main Bank", "BankA", "001", LinkedAccId));

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Main Bank");
        captured.OrganizationId.Should().Be(OrgId);
        captured.LinkedAccountId.Should().Be(LinkedAccId);
    }

    [Fact]
    public async Task ImportTransactionsAsync_BankAccountNotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetAccountAsync(OrgId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((BankAccount?)null);

        await _sut.Invoking(s => s.ImportTransactionsAsync(OrgId, Guid.NewGuid(),
                new List<ImportBankTransactionDto> { new("2025-01-01", "Test", 100m, "credit") }))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ImportTransactionsAsync_PositiveAmountNoType_IsCredit()
    {
        IEnumerable<BankTransaction>? captured = null;
        await _repo.AddTransactionsAsync(
            Arg.Do<IEnumerable<BankTransaction>>(txs => captured = txs),
            Arg.Any<CancellationToken>());

        await _sut.ImportTransactionsAsync(OrgId, BankAccountId,
            new List<ImportBankTransactionDto> { new("2025-01-15", "Pago recibido", 500m, "") });

        captured.Should().NotBeNull();
        var tx = captured!.First();
        tx.Type.Should().Be(BankTransactionType.Credit);
        tx.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task ImportTransactionsAsync_NegativeAmount_IsDebit()
    {
        IEnumerable<BankTransaction>? captured = null;
        await _repo.AddTransactionsAsync(
            Arg.Do<IEnumerable<BankTransaction>>(txs => captured = txs),
            Arg.Any<CancellationToken>());

        await _sut.ImportTransactionsAsync(OrgId, BankAccountId,
            new List<ImportBankTransactionDto> { new("2025-01-16", "Pago servicios", -250m, "") });

        captured!.First().Type.Should().Be(BankTransactionType.Debit);
        captured.First().Amount.Should().Be(250m); // stored as absolute value
    }

    [Fact]
    public async Task ImportTransactionsAsync_ExplicitType_OverridesAmountSign()
    {
        IEnumerable<BankTransaction>? captured = null;
        await _repo.AddTransactionsAsync(
            Arg.Do<IEnumerable<BankTransaction>>(txs => captured = txs),
            Arg.Any<CancellationToken>());

        await _sut.ImportTransactionsAsync(OrgId, BankAccountId,
            new List<ImportBankTransactionDto> { new("2025-01-17", "Cargo", 100m, "debit") });

        captured!.First().Type.Should().Be(BankTransactionType.Debit);
    }

    [Fact]
    public async Task MatchTransactionAsync_JournalNotInOrg_ThrowsArgumentException()
    {
        _journal.GetByIdAsync(JournalId, OrgId, Arg.Any<CancellationToken>())
            .Returns((JournalEntry?)null);

        await _sut.Invoking(s => s.MatchTransactionAsync(OrgId, TxId, new MatchTransactionDto(JournalId)))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*asiento no pertenece*");
    }

    [Fact]
    public async Task MatchTransactionAsync_Valid_SetsStatusMatched()
    {
        var tx      = MakeTransaction();
        var entry   = new JournalEntry { Id = JournalId, OrganizationId = OrgId };
        _repo.GetTransactionAsync(OrgId, TxId, Arg.Any<CancellationToken>()).Returns(tx);
        _journal.GetByIdAsync(JournalId, OrgId, Arg.Any<CancellationToken>()).Returns(entry);

        var result = await _sut.MatchTransactionAsync(OrgId, TxId, new MatchTransactionDto(JournalId));

        tx.Status.Should().Be(BankTransactionStatus.Matched);
        tx.JournalEntryId.Should().Be(JournalId);
    }

    [Fact]
    public async Task ExcludeTransactionAsync_SetsStatusExcluded()
    {
        var tx = MakeTransaction();
        _repo.GetTransactionAsync(OrgId, TxId, Arg.Any<CancellationToken>()).Returns(tx);

        await _sut.ExcludeTransactionAsync(OrgId, TxId);

        tx.Status.Should().Be(BankTransactionStatus.Excluded);
        tx.JournalEntryId.Should().BeNull();
    }

    [Fact]
    public async Task UnmatchTransactionAsync_ResetsToPending()
    {
        var tx = MakeTransaction();
        tx.Status         = BankTransactionStatus.Matched;
        tx.JournalEntryId = JournalId;
        _repo.GetTransactionAsync(OrgId, TxId, Arg.Any<CancellationToken>()).Returns(tx);

        await _sut.UnmatchTransactionAsync(OrgId, TxId);

        tx.Status.Should().Be(BankTransactionStatus.Pending);
        tx.JournalEntryId.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountsAsync_ReturnsMappedAccounts()
    {
        _repo.GetAccountsAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new List<BankAccount> { MakeBankAccount() });
        _repo.GetPendingCountsAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int> { [BankAccountId] = 3 });

        var result = await _sut.GetAccountsAsync(OrgId);

        result.Should().HaveCount(1);
        result[0].PendingCount.Should().Be(3);
    }

    private static Account MakeAccount() =>
        new() { Id = LinkedAccId, Code = "1.1.01", Name = "Banco", Type = AccountType.Asset, IsPostable = true };

    private static BankAccount MakeBankAccount() =>
        new() { Id = BankAccountId, OrganizationId = OrgId, Name = "Cuenta Principal", LinkedAccountId = LinkedAccId, LinkedAccount = MakeAccount() };

    private static BankTransaction MakeTransaction() =>
        new() { Id = TxId, BankAccountId = BankAccountId, Date = new DateOnly(2025, 1, 15), Description = "Test", Amount = 100m, Type = BankTransactionType.Credit, Status = BankTransactionStatus.Pending };
}
