using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;

namespace Accounting.Application.Services;

public interface IAuditService
{
    Task LogAsync(Guid orgId, Guid userId, string action, string entityType,
        Guid entityId, string? details = null, CancellationToken ct = default);

    Task<PagedResult<AuditLogDto>> ListAsync(
        Guid orgId, int page, int pageSize, CancellationToken ct = default);
}

public class AuditService : IAuditService
{
    private readonly IAuditRepository _repo;
    public AuditService(IAuditRepository repo) => _repo = repo;

    public async Task LogAsync(Guid orgId, Guid userId, string action, string entityType,
        Guid entityId, string? details = null, CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            OrganizationId = orgId,
            UserId         = userId,
            Action         = action,
            EntityType     = entityType,
            EntityId       = entityId,
            Details        = details
        };
        await _repo.AddAsync(log, ct);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditLogDto>> ListAsync(
        Guid orgId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _repo.GetPagedAsync(orgId, page, pageSize, ct);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new PagedResult<AuditLogDto>(
            items.Select(a => new AuditLogDto(
                a.Id, a.UserId,
                a.User is not null ? $"{a.User.FirstName} {a.User.LastName}" : "Usuario desconocido",
                a.Action, a.EntityType, a.EntityId, a.Details, a.CreatedAtUtc
            )).ToList(),
            total, page, pageSize, totalPages);
    }
}
