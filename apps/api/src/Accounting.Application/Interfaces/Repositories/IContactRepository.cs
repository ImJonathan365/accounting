using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Interfaces.Repositories;

public interface IContactRepository
{
    Task<List<Contact>> GetByOrganizationAsync(Guid orgId, ContactType? type = null, CancellationToken ct = default);
    Task<Contact?>      GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task                AddAsync(Contact contact, CancellationToken ct = default);
    Task                SaveChangesAsync(CancellationToken ct = default);
}
