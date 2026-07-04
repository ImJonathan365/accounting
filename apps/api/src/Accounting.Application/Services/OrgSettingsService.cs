using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentValidation;

namespace Accounting.Application.Services;

public interface IOrgSettingsService
{
    Task<OrgSettingsDto> GetAsync(Guid orgId, CancellationToken ct = default);
    Task<OrgSettingsDto> UpsertAsync(Guid orgId, UpdateOrgSettingsDto dto, CancellationToken ct = default);
}

public class OrgSettingsService : IOrgSettingsService
{
    private readonly IOrganizationSettingsRepository _repo;
    private readonly IOrganizationRepository         _orgs;
    private readonly IValidator<UpdateOrgSettingsDto> _validator;

    public OrgSettingsService(
        IOrganizationSettingsRepository repo,
        IOrganizationRepository orgs,
        IValidator<UpdateOrgSettingsDto> validator)
    {
        _repo      = repo;
        _orgs      = orgs;
        _validator = validator;
    }

    public async Task<OrgSettingsDto> GetAsync(Guid orgId, CancellationToken ct = default)
    {
        var settings = await _repo.GetByOrgIdAsync(orgId, ct);
        if (settings is not null)
            return ToDto(settings);

        // Return defaults using org name when not yet configured
        var org = await _orgs.GetByIdAsync(orgId, ct)
            ?? throw new KeyNotFoundException("Organización no encontrada.");

        return new OrgSettingsDto(orgId, org.Name, null, null, null, null, null, "$", ReportTheme.Professional);
    }

    public async Task<OrgSettingsDto> UpsertAsync(Guid orgId, UpdateOrgSettingsDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct);

        var settings = await _repo.GetByOrgIdAsync(orgId, ct);

        if (settings is null)
        {
            settings = new OrganizationSettings { OrganizationId = orgId };
            await _repo.AddAsync(settings, ct);
        }

        settings.CompanyName    = dto.CompanyName.Trim();
        settings.LogoUrl        = dto.LogoUrl?.Trim();
        settings.Address        = dto.Address?.Trim();
        settings.TaxId          = dto.TaxId?.Trim();
        settings.Phone          = dto.Phone?.Trim();
        settings.Email          = dto.Email?.Trim();
        settings.CurrencySymbol = dto.CurrencySymbol.Trim();
        settings.Theme          = dto.Theme;
        settings.UpdatedAtUtc   = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
        return ToDto(settings);
    }

    private static OrgSettingsDto ToDto(OrganizationSettings s) =>
        new(s.OrganizationId, s.CompanyName, s.LogoUrl, s.Address,
            s.TaxId, s.Phone, s.Email, s.CurrencySymbol, s.Theme);
}
