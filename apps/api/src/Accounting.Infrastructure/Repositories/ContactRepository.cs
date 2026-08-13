using Accounting.Application.DTOs;
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

    public Task<List<ContactDto>> GetByOrganizationAsync(Guid orgId, ContactType? type = null, CancellationToken ct = default)
    {
        var q = _db.Contacts.AsNoTracking()
            .Where(c => c.OrganizationId == orgId);

        if (type is not null)
            q = q.Where(c => c.Type == type || c.Type == ContactType.Both);

        return q.OrderBy(c => c.Name)
            .Select(c => new ContactDto(
                c.Id, c.Type, c.Name, c.Email, c.Phone, c.Address, c.Notes, c.IsActive,
                c.Invoices.Count()))
            .ToListAsync(ct);
    }

    public async Task<(List<ContactDto> Items, int Total)> GetPagedAsync(
        Guid orgId, ContactType? type, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Contacts.AsNoTracking()
            .Where(c => c.OrganizationId == orgId);

        if (type is not null)
            q = q.Where(c => c.Type == type || c.Type == ContactType.Both);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search.Trim().Replace("%", "\\%").Replace("_", "\\_");
            q = q.Where(c => EF.Functions.ILike(c.Name, $"%{escaped}%", "\\")
                           || (c.Email != null && EF.Functions.ILike(c.Email, $"%{escaped}%", "\\")));
        }

        var total = await q.CountAsync(ct);

        var items = await q.OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContactDto(
                c.Id, c.Type, c.Name, c.Email, c.Phone, c.Address, c.Notes, c.IsActive,
                c.Invoices.Count()))
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Contact?> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.Contacts
            .FirstOrDefaultAsync(c => c.OrganizationId == orgId && c.Id == id, ct);

    public Task<ContactDto?> GetDtoByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.Contacts.AsNoTracking()
            .Where(c => c.OrganizationId == orgId && c.Id == id)
            .Select(c => new ContactDto(
                c.Id, c.Type, c.Name, c.Email, c.Phone, c.Address, c.Notes, c.IsActive,
                c.Invoices.Count()))
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Contact contact, CancellationToken ct = default) =>
        await _db.Contacts.AddAsync(contact, ct);

    public Task<bool> HasInvoicesAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        _db.Invoices.AnyAsync(i => i.OrganizationId == orgId && i.ContactId == id, ct);

    public void Remove(Contact contact) => _db.Contacts.Remove(contact);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
