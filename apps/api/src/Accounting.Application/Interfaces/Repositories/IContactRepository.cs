using Accounting.Application.DTOs;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Interfaces.Repositories;

public interface IContactRepository
{
    Task<List<ContactDto>> GetByOrganizationAsync(Guid orgId, ContactType? type = null, CancellationToken ct = default);
    Task<(List<ContactDto> Items, int Total)> GetPagedAsync(Guid orgId, ContactType? type, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<Contact?>      GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<ContactDto?>   GetDtoByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task                AddAsync(Contact contact, CancellationToken ct = default);
    Task<bool>          HasInvoicesAsync(Guid orgId, Guid id, CancellationToken ct = default);
    void                Remove(Contact contact);
    Task                SaveChangesAsync(CancellationToken ct = default);
}
