using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public Task<List<Product>> GetByOrganizationAsync(Guid orgId, CancellationToken ct = default) =>
        _db.Products.AsNoTracking()
            .Include(p => p.Account)
            .Include(p => p.TaxRate)
            .Where(p => p.OrganizationId == orgId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public Task<Product?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.Products
            .Include(p => p.Account)
            .Include(p => p.TaxRate)
            .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.Id == id, ct);

    public Task<bool> NameExistsAsync(Guid orgId, string name, CancellationToken ct = default) =>
        _db.Products.AnyAsync(p => p.OrganizationId == orgId && p.Name == name, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await _db.Products.AddAsync(product, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
