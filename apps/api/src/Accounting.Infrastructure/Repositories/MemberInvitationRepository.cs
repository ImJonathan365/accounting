using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class MemberInvitationRepository : IMemberInvitationRepository
{
    private readonly AppDbContext _db;
    public MemberInvitationRepository(AppDbContext db) => _db = db;

    public async Task<MemberInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        await _db.MemberInvitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public async Task<MemberInvitation?> GetPendingAsync(Guid orgId, string email, CancellationToken ct = default) =>
        await _db.MemberInvitations.FirstOrDefaultAsync(
            i => i.OrganizationId == orgId
              && i.InvitedEmail    == email
              && i.AcceptedAtUtc   == null
              && i.DeclinedAtUtc   == null
              && i.ExpiresAtUtc    > DateTime.UtcNow,
            ct);

    public async Task AddAsync(MemberInvitation invitation, CancellationToken ct = default) =>
        await _db.MemberInvitations.AddAsync(invitation, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
