using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IAccountRepository
{
    Task<List<Account>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(Guid orgId, string code, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
