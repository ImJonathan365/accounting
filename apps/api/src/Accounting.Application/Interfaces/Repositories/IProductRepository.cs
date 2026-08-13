using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default);
    Task<Product?>      GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<bool>          NameExistsAsync(Guid orgId, string name, CancellationToken ct = default);
    Task                AddAsync(Product product, CancellationToken ct = default);
    void                Remove(Product product);
    Task                SaveChangesAsync(CancellationToken ct = default);
}
