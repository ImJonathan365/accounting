using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IBankService
{
    Task<List<BankAccountDto>>  GetAccountsAsync(Guid orgId, CancellationToken ct = default);
    Task<BankAccountDto>        CreateAccountAsync(Guid orgId, CreateBankAccountDto dto, CancellationToken ct = default);
    Task<BankReconciliationDto> GetReconciliationAsync(Guid orgId, Guid bankAccountId, CancellationToken ct = default);
    Task<List<BankTransactionDto>> ImportTransactionsAsync(Guid orgId, Guid bankAccountId, List<ImportBankTransactionDto> rows, CancellationToken ct = default);
    Task<BankTransactionDto>    MatchTransactionAsync(Guid orgId, Guid transactionId, MatchTransactionDto dto, CancellationToken ct = default);
    Task<BankTransactionDto>    ExcludeTransactionAsync(Guid orgId, Guid transactionId, CancellationToken ct = default);
    Task<BankTransactionDto>    UnmatchTransactionAsync(Guid orgId, Guid transactionId, CancellationToken ct = default);
}

public class BankService : IBankService
{
    private readonly IBankRepository    _repo;
    private readonly IAccountRepository _accounts;

    public BankService(IBankRepository repo, IAccountRepository accounts)
    {
        _repo     = repo;
        _accounts = accounts;
    }

    public async Task<List<BankAccountDto>> GetAccountsAsync(Guid orgId, CancellationToken ct = default)
    {
        var list = await _repo.GetAccountsAsync(orgId, ct);
        return list.Select(MapAccount).ToList();
    }

    public async Task<BankAccountDto> CreateAccountAsync(Guid orgId, CreateBankAccountDto dto, CancellationToken ct = default)
    {
        var linkedAccount = await _accounts.GetByIdAsync(dto.LinkedAccountId, orgId, ct)
            ?? throw new KeyNotFoundException("La cuenta contable no existe.");

        var bankAccount = new BankAccount
        {
            OrganizationId  = orgId,
            Name            = dto.Name.Trim(),
            BankName        = dto.BankName?.Trim(),
            AccountNumber   = dto.AccountNumber?.Trim(),
            LinkedAccountId = dto.LinkedAccountId,
        };

        await _repo.AddAccountAsync(bankAccount, ct);
        await _repo.SaveChangesAsync(ct);

        bankAccount.LinkedAccount = linkedAccount;
        return MapAccount(bankAccount);
    }

    public async Task<BankReconciliationDto> GetReconciliationAsync(Guid orgId, Guid bankAccountId, CancellationToken ct = default)
    {
        var account = await _repo.GetAccountAsync(orgId, bankAccountId, ct)
            ?? throw new KeyNotFoundException("Cuenta bancaria no encontrada.");

        var transactions = await _repo.GetTransactionsAsync(bankAccountId, ct);
        var unmatched    = transactions.Where(t => t.Status == BankTransactionStatus.Pending).ToList();

        var unmatchedJournal = await _repo.GetUnmatchedJournalEntriesAsync(orgId, account.LinkedAccountId, ct);

        var bankBalance = transactions
            .Where(t => t.Status != BankTransactionStatus.Excluded)
            .Sum(t => t.Type == BankTransactionType.Credit ? t.Amount : -t.Amount);

        var bookBalance = unmatchedJournal.Count == 0 ? bankBalance : 0;

        return new BankReconciliationDto(
            MapAccount(account),
            unmatched.Select(MapTransaction).ToList(),
            unmatchedJournal.Select(j => new UnmatchedJournalDto(
                j.Id, j.Date.ToString("yyyy-MM-dd"), j.Reference, j.Description, j.Amount)).ToList(),
            bankBalance,
            bookBalance,
            bankBalance - bookBalance);
    }

    public async Task<List<BankTransactionDto>> ImportTransactionsAsync(Guid orgId, Guid bankAccountId, List<ImportBankTransactionDto> rows, CancellationToken ct = default)
    {
        var account = await _repo.GetAccountAsync(orgId, bankAccountId, ct)
            ?? throw new KeyNotFoundException("Cuenta bancaria no encontrada.");

        var txs = rows.Select(r => new BankTransaction
        {
            BankAccountId = bankAccountId,
            Date          = DateOnly.Parse(r.Date),
            Description   = r.Description.Trim(),
            Amount        = Math.Abs(r.Amount),
            Type          = r.Type.Equals("credit", StringComparison.OrdinalIgnoreCase)
                            || r.Amount > 0
                            ? BankTransactionType.Credit
                            : BankTransactionType.Debit,
            Status        = BankTransactionStatus.Pending,
        }).ToList();

        await _repo.AddTransactionsAsync(txs, ct);
        await _repo.SaveChangesAsync(ct);
        return txs.Select(MapTransaction).ToList();
    }

    public async Task<BankTransactionDto> MatchTransactionAsync(Guid orgId, Guid transactionId, MatchTransactionDto dto, CancellationToken ct = default)
    {
        var tx = await _repo.GetTransactionAsync(transactionId, ct)
            ?? throw new KeyNotFoundException("Transacción no encontrada.");

        tx.JournalEntryId = dto.JournalEntryId;
        tx.Status         = BankTransactionStatus.Matched;
        await _repo.SaveChangesAsync(ct);
        return MapTransaction(tx);
    }

    public async Task<BankTransactionDto> ExcludeTransactionAsync(Guid orgId, Guid transactionId, CancellationToken ct = default)
    {
        var tx = await _repo.GetTransactionAsync(transactionId, ct)
            ?? throw new KeyNotFoundException("Transacción no encontrada.");

        tx.Status         = BankTransactionStatus.Excluded;
        tx.JournalEntryId = null;
        await _repo.SaveChangesAsync(ct);
        return MapTransaction(tx);
    }

    public async Task<BankTransactionDto> UnmatchTransactionAsync(Guid orgId, Guid transactionId, CancellationToken ct = default)
    {
        var tx = await _repo.GetTransactionAsync(transactionId, ct)
            ?? throw new KeyNotFoundException("Transacción no encontrada.");

        tx.Status         = BankTransactionStatus.Pending;
        tx.JournalEntryId = null;
        await _repo.SaveChangesAsync(ct);
        return MapTransaction(tx);
    }

    private static BankAccountDto MapAccount(BankAccount a) => new(
        a.Id, a.Name, a.BankName, a.AccountNumber,
        a.LinkedAccountId,
        a.LinkedAccount?.Code ?? "",
        a.LinkedAccount?.Name ?? "",
        a.IsActive,
        a.Transactions.Count(t => t.Status == BankTransactionStatus.Pending));

    private static BankTransactionDto MapTransaction(BankTransaction t) => new(
        t.Id, t.BankAccountId,
        t.Date.ToString("yyyy-MM-dd"),
        t.Description, t.Amount, t.Type, t.Status,
        t.JournalEntryId, t.Notes);
}
