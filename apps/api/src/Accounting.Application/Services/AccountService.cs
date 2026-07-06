using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using FluentValidation;

namespace Accounting.Application.Services;

public interface IAccountService
{
    Task<List<AccountDto>> ListAsync(Guid orgId, CancellationToken ct = default);
    Task<AccountDto> CreateAsync(Guid orgId, CreateAccountDto dto, CancellationToken ct = default);
    Task<AccountDto> UpdateAsync(Guid orgId, Guid id, UpdateAccountDto dto, CancellationToken ct = default);
    Task<AccountDto> ToggleActiveAsync(Guid orgId, Guid id, CancellationToken ct = default);
}

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repo;
    private readonly IValidator<CreateAccountDto> _createValidator;
    private readonly IValidator<UpdateAccountDto> _updateValidator;

    public AccountService(
        IAccountRepository repo,
        IValidator<CreateAccountDto> createValidator,
        IValidator<UpdateAccountDto> updateValidator)
    {
        _repo = repo;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<AccountDto>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        var accounts = await _repo.GetByOrganizationAsync(orgId, ct);
        return accounts.Select(Map).ToList();
    }

    public async Task<AccountDto> CreateAsync(Guid orgId, CreateAccountDto dto, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(dto, ct);

        if (await _repo.CodeExistsAsync(orgId, dto.Code, ct))
            throw new InvalidOperationException($"El código '{dto.Code}' ya existe en esta organización.");

        var account = new Account
        {
            OrganizationId  = orgId,
            Code            = dto.Code.Trim(),
            Name            = dto.Name.Trim(),
            Type            = dto.Type,
            ParentId        = dto.ParentId,
            IsPostable      = dto.IsPostable,
            CashFlowSection = dto.CashFlowSection,
        };
        await _repo.AddAsync(account, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(account);
    }

    public async Task<AccountDto> UpdateAsync(Guid orgId, Guid id, UpdateAccountDto dto, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(dto, ct);
        var account = await _repo.GetByIdTrackedAsync(id, orgId, ct)
            ?? throw new KeyNotFoundException($"Cuenta {id} no encontrada.");
        account.Name            = dto.Name.Trim();
        account.IsPostable      = dto.IsPostable;
        account.CashFlowSection = dto.CashFlowSection;
        await _repo.SaveChangesAsync(ct);
        return Map(account);
    }

    public async Task<AccountDto> ToggleActiveAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var account = await _repo.GetByIdTrackedAsync(id, orgId, ct)
            ?? throw new KeyNotFoundException($"Cuenta {id} no encontrada.");
        account.IsActive = !account.IsActive;
        await _repo.SaveChangesAsync(ct);
        return Map(account);
    }

    private static AccountDto Map(Account a) =>
        new(a.Id, a.Code, a.Name, a.Type, a.ParentId, a.IsPostable, a.IsActive, a.CashFlowSection);
}
