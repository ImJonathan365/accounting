using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AppDbContext _db;
    public EmailVerificationTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(EmailVerificationToken token, CancellationToken ct = default) =>
        await _db.EmailVerificationTokens.AddAsync(token, ct);

    public async Task<EmailVerificationToken?> GetByHashAsync(string hash, CancellationToken ct = default) =>
        await _db.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

    public async Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.EmailVerificationTokens
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
