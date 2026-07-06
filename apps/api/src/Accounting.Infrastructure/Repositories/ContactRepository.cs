using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly AppDbContext _db;
    public ContactRepository(AppDbContext db) => _db = db;

    public Task<List<Contact>> GetByOrganizationAsync(Guid orgId, ContactType? type = null, CancellationToken ct = default)
    {
        var q = _db.Contacts
            .Include(c => c.Invoices)
            .Where(c => c.OrganizationId == orgId);

        if (type is not null)
            q = q.Where(c => c.Type == type || c.Type == ContactType.Both);

        return q.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public Task<Contact?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.Contacts
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.OrganizationId == orgId && c.Id == id, ct);

    public async Task AddAsync(Contact contact, CancellationToken ct = default) =>
        await _db.Contacts.AddAsync(contact, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
