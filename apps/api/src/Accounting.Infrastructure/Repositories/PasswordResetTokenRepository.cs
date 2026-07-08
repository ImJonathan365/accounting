using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _db;
    public PasswordResetTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default) =>
        await _db.PasswordResetTokens.AddAsync(token, ct);

    public async Task<PasswordResetToken?> GetByHashAsync(string hash, CancellationToken ct = default) =>
        await _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.PasswordResetTokens
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
