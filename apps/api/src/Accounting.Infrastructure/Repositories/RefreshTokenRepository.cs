using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await _db.RefreshTokens.AddAsync(token, ct);

    public async Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct = default) =>
        await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

    public async Task RevokeByHashAsync(string hash, CancellationToken ct = default) =>
        await _db.RefreshTokens
            .Where(t => t.TokenHash == hash && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), ct);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
