using Accounting.Domain.Entities;

namespace Accounting.Application.Interfaces.Repositories;

public interface IMemberInvitationRepository
{
    Task<MemberInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<MemberInvitation?> GetPendingAsync(Guid orgId, string email, CancellationToken ct = default);
    Task AddAsync(MemberInvitation invitation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
