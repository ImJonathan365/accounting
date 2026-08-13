using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IBudgetService
{
    Task<List<BudgetDto>>      GetAllAsync(Guid orgId, CancellationToken ct = default);
    Task<BudgetDto>            GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<BudgetDto>            CreateAsync(Guid orgId, CreateBudgetDto dto, CancellationToken ct = default);
    Task<BudgetDto>            UpsertLineAsync(Guid orgId, Guid budgetId, UpsertBudgetLineDto dto, CancellationToken ct = default);
    Task<BudgetVsActualDto>    GetVsActualAsync(Guid orgId, Guid budgetId, CancellationToken ct = default);
    Task                       DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default);
}

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository  _repo;
    private readonly IAccountRepository _accounts;
    private readonly IReportRepository  _reports;

    public BudgetService(IBudgetRepository repo, IAccountRepository accounts, IReportRepository reports)
    {
        _repo     = repo;
        _accounts = accounts;
        _reports  = reports;
    }

    public async Task<List<BudgetDto>> GetAllAsync(Guid orgId, CancellationToken ct = default)
    {
        var list = await _repo.GetByOrganizationAsync(orgId, ct);
        return list.Select(Map).ToList();
    }

    public async Task<BudgetDto> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var budget = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Presupuesto no encontrado.");
        return Map(budget);
    }

    public async Task<BudgetDto> CreateAsync(Guid orgId, CreateBudgetDto dto, CancellationToken ct = default)
    {
        var budget = new Budget
        {
            OrganizationId = orgId,
            Year           = dto.Year,
            Name           = dto.Name.Trim(),
        };
        await _repo.AddAsync(budget, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(budget);
    }

    public async Task<BudgetDto> UpsertLineAsync(Guid orgId, Guid budgetId, UpsertBudgetLineDto dto, CancellationToken ct = default)
    {
        var budget = await _repo.GetByIdAsync(orgId, budgetId, ct)
            ?? throw new KeyNotFoundException("Presupuesto no encontrado.");

        var accountExists = await _accounts.GetByIdAsync(dto.AccountId, orgId, ct) is not null;
        if (!accountExists)
            throw new KeyNotFoundException("Cuenta no encontrada.");

        var existing = budget.Lines.FirstOrDefault(l => l.AccountId == dto.AccountId && l.Month == dto.Month);
        if (existing is not null)
        {
            if (dto.Amount == 0)
                _repo.RemoveLine(existing);
            else
                existing.Amount = dto.Amount;
        }
        else if (dto.Amount != 0)
        {
            budget.Lines.Add(new BudgetLine
            {
                BudgetId  = budgetId,
                AccountId = dto.AccountId,
                Month     = dto.Month,
                Amount    = dto.Amount,
            });
        }

        await _repo.SaveChangesAsync(ct);

        var updated = await _repo.GetByIdAsync(orgId, budgetId, ct);
        return Map(updated!);
    }

    public async Task<BudgetVsActualDto> GetVsActualAsync(Guid orgId, Guid budgetId, CancellationToken ct = default)
    {
        var budget = await _repo.GetByIdAsync(orgId, budgetId, ct)
            ?? throw new KeyNotFoundException("Presupuesto no encontrado.");

        var from   = new DateOnly(budget.Year, 1, 1);
        var to     = new DateOnly(budget.Year, 12, 31);
        var actual = await _reports.GetMonthlyTrendsAsync(orgId, from, to, ct);

        var accountIds = budget.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts   = await _accounts.GetByIdsAsync(orgId, accountIds, ct);
        var accountMap = accounts.ToDictionary(a => a.Id);

        var grouped = budget.Lines.GroupBy(l => l.AccountId);
        var lines = grouped.Select(g =>
        {
            var acc         = accountMap.TryGetValue(g.Key, out var a) ? a : null;
            var budgetByMonth = new decimal[12];
            var actualByMonth = new decimal[12];

            foreach (var line in g)
                budgetByMonth[line.Month - 1] = line.Amount;

            foreach (var row in actual.Where(r => r.AccountId == g.Key))
            {
                var idx = row.Month - 1;
                // Credit-normal accounts (Income, Liability, Equity): Credit - Debit
                // Debit-normal accounts (Asset, Expense): Debit - Credit
                actualByMonth[idx] = acc?.Type is AccountType.Income or AccountType.Liability or AccountType.Equity
                    ? row.Credit - row.Debit
                    : row.Debit - row.Credit;
            }

            return new BudgetVsActualLineDto(
                g.Key,
                acc?.Code ?? "",
                acc?.Name ?? "",
                acc?.Type.ToString() ?? "",
                budgetByMonth,
                actualByMonth,
                budgetByMonth.Sum(),
                actualByMonth.Sum(),
                actualByMonth.Sum() - budgetByMonth.Sum());
        }).OrderBy(l => l.AccountCode).ToList();

        return new BudgetVsActualDto(budget.Year, budget.Name, lines);
    }

    public async Task DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var budget = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Presupuesto no encontrado.");
        _repo.Remove(budget);
        await _repo.SaveChangesAsync(ct);
    }

    private static BudgetDto Map(Budget b) => new(
        b.Id, b.Year, b.Name, b.IsActive,
        b.Lines.Select(l => new BudgetLineDto(
            l.Id, l.AccountId,
            l.Account?.Code ?? "", l.Account?.Name ?? "", l.Account?.Type.ToString() ?? "",
            l.Month, l.Amount)).ToList());
}
