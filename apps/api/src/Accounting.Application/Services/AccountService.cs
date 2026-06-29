using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;

namespace Accounting.Application.Services;

public interface IAccountService
{
    Task<List<AccountDto>> ListAsync(Guid orgId, CancellationToken ct = default);
    Task<AccountDto> CreateAsync(Guid orgId, CreateAccountDto dto, CancellationToken ct = default);
}

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repo;
    public AccountService(IAccountRepository repo) => _repo = repo;

    public async Task<List<AccountDto>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        var accounts = await _repo.GetByOrganizationAsync(orgId, ct);
        return accounts.Select(Map).ToList();
    }

    public async Task<AccountDto> CreateAsync(Guid orgId, CreateAccountDto dto, CancellationToken ct = default)
    {
        if (await _repo.CodeExistsAsync(orgId, dto.Code, ct))
            throw new InvalidOperationException($"El código {dto.Code} ya existe.");

        var account = new Account
        {
            OrganizationId = orgId,
            Code = dto.Code,
            Name = dto.Name,
            Type = dto.Type,
            ParentId = dto.ParentId,
            IsPostable = dto.IsPostable
        };
        await _repo.AddAsync(account, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(account);
    }

    private static AccountDto Map(Account a) =>
        new(a.Id, a.Code, a.Name, a.Type, a.ParentId, a.IsPostable, a.IsActive);
}
