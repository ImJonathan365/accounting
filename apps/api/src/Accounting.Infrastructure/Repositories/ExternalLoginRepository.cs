using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly AppDbContext _db;
    public ExternalLoginRepository(AppDbContext db) => _db = db;

    public Task<ExternalLogin?> GetByProviderAsync(AuthProvider provider, string providerUserId, CancellationToken ct = default) =>
        _db.ExternalLogins
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderUserId == providerUserId, ct);

    public async Task AddAsync(ExternalLogin login, CancellationToken ct = default) =>
        await _db.ExternalLogins.AddAsync(login, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
