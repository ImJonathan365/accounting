using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task AddAsync(EmailVerificationToken token, CancellationToken ct = default);
    Task<EmailVerificationToken?> GetByHashAsync(string hash, CancellationToken ct = default);
    Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
