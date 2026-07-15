using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;

namespace Accounting.Application.Services;

public interface IContactService
{
    Task<PagedResult<ContactDto>> GetPagedAsync(Guid orgId, ContactType? type, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<ContactDto>              GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default);
    Task<ContactDto>              CreateAsync(Guid orgId, CreateContactDto dto, CancellationToken ct = default);
    Task<ContactDto>              UpdateAsync(Guid orgId, Guid id, UpdateContactDto dto, CancellationToken ct = default);
}

public class ContactService : IContactService
{
    private readonly IContactRepository _repo;
    public ContactService(IContactRepository repo) => _repo = repo;

    public async Task<PagedResult<ContactDto>> GetPagedAsync(Guid orgId, ContactType? type, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _repo.GetPagedAsync(orgId, type, search, page, pageSize, ct);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<ContactDto>(items, total, page, pageSize, totalPages);
    }

    public async Task<ContactDto> GetByIdAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        await _repo.GetDtoByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Contacto no encontrado.");

    public async Task<ContactDto> CreateAsync(Guid orgId, CreateContactDto dto, CancellationToken ct = default)
    {
        var contact = new Contact
        {
            OrganizationId = orgId,
            Type    = dto.Type,
            Name    = dto.Name.Trim(),
            Email   = dto.Email?.Trim(),
            Phone   = dto.Phone?.Trim(),
            Address = dto.Address?.Trim(),
            Notes   = dto.Notes?.Trim(),
        };
        await _repo.AddAsync(contact, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(contact);
    }

    public async Task<ContactDto> UpdateAsync(Guid orgId, Guid id, UpdateContactDto dto, CancellationToken ct = default)
    {
        var contact = await _repo.GetByIdAsync(orgId, id, ct)
            ?? throw new KeyNotFoundException("Contacto no encontrado.");

        if (dto.Type     is not null) contact.Type     = dto.Type.Value;
        if (dto.Name     is not null) contact.Name     = dto.Name.Trim();
        if (dto.Email    is not null) contact.Email    = dto.Email.Trim();
        if (dto.Phone    is not null) contact.Phone    = dto.Phone.Trim();
        if (dto.Address  is not null) contact.Address  = dto.Address.Trim();
        if (dto.Notes    is not null) contact.Notes    = dto.Notes.Trim();
        if (dto.IsActive is not null) contact.IsActive = dto.IsActive.Value;

        await _repo.SaveChangesAsync(ct);
        return await _repo.GetDtoByIdAsync(orgId, id, ct) ?? throw new KeyNotFoundException("Contacto no encontrado.");
    }

    private static ContactDto Map(Contact c) => new(
        c.Id, c.Type, c.Name, c.Email, c.Phone, c.Address, c.Notes, c.IsActive,
        InvoiceCount: 0);
}
