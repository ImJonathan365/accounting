using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default);
    Task<Product?>      GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task                AddAsync(Product product, CancellationToken ct = default);
    Task                SaveChangesAsync(CancellationToken ct = default);
}
