using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class OrgSettingsServiceTests
{
    private readonly IOrganizationSettingsRepository _repo      = Substitute.For<IOrganizationSettingsRepository>();
    private readonly IOrganizationRepository         _orgs      = Substitute.For<IOrganizationRepository>();
    private readonly IValidator<UpdateOrgSettingsDto> _validator = Substitute.For<IValidator<UpdateOrgSettingsDto>>();
    private readonly OrgSettingsService _sut;

    private static readonly Guid OrgId = Guid.NewGuid();

    public OrgSettingsServiceTests()
    {
        _sut = new OrgSettingsService(_repo, _orgs, _validator);

        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
    }

    // ── GetAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_SettingsExist_ReturnsMappedDto()
    {
        var settings = new OrganizationSettings
        {
            OrganizationId = OrgId,
            CompanyName    = "Mi Empresa S.A.",
            CurrencySymbol = "Q",
            Theme          = ReportTheme.Corporate,
        };
        _repo.GetByOrgIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns(settings);

        var result = await _sut.GetAsync(OrgId);

        result.CompanyName.Should().Be("Mi Empresa S.A.");
        result.CurrencySymbol.Should().Be("Q");
        result.Theme.Should().Be(ReportTheme.Corporate);
    }

    [Fact]
    public async Task GetAsync_NoSettings_ReturnsDefaultsWithOrgName()
    {
        _repo.GetByOrgIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns((OrganizationSettings?)null);
        _orgs.GetByIdAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(new Organization { Id = OrgId, Name = "Comercial Nuevo" });

        var result = await _sut.GetAsync(OrgId);

        result.CompanyName.Should().Be("Comercial Nuevo");
        result.CurrencySymbol.Should().Be("$");
        result.Theme.Should().Be(ReportTheme.Professional);
        result.LogoUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_NoSettingsAndOrgNotFound_ThrowsKeyNotFound()
    {
        _repo.GetByOrgIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns((OrganizationSettings?)null);
        _orgs.GetByIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns((Organization?)null);

        await _sut.Invoking(s => s.GetAsync(OrgId))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Organización no encontrada*");
    }

    // ── UpsertAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_NewSettings_AddsAndSaves()
    {
        _repo.GetByOrgIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns((OrganizationSettings?)null);

        var dto = MakeDto("Empresa Nueva", "€");

        var result = await _sut.UpsertAsync(OrgId, dto);

        result.CompanyName.Should().Be("Empresa Nueva");
        result.CurrencySymbol.Should().Be("€");
        await _repo.Received(1).AddAsync(Arg.Any<OrganizationSettings>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertAsync_ExistingSettings_UpdatesInPlace()
    {
        var existing = new OrganizationSettings
        {
            OrganizationId = OrgId,
            CompanyName    = "Nombre Viejo",
            CurrencySymbol = "$",
            Theme          = ReportTheme.Professional,
        };
        _repo.GetByOrgIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns(existing);

        var dto = MakeDto("Nombre Nuevo", "Q", ReportTheme.Corporate);

        var result = await _sut.UpsertAsync(OrgId, dto);

        result.CompanyName.Should().Be("Nombre Nuevo");
        result.CurrencySymbol.Should().Be("Q");
        result.Theme.Should().Be(ReportTheme.Corporate);
        await _repo.DidNotReceive().AddAsync(Arg.Any<OrganizationSettings>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertAsync_TrimsWhitespace_StoresTrimmedValues()
    {
        _repo.GetByOrgIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns((OrganizationSettings?)null);

        var dto = MakeDto("  Empresa Con Espacios  ", "  $  ");

        var result = await _sut.UpsertAsync(OrgId, dto);

        result.CompanyName.Should().Be("Empresa Con Espacios");
        result.CurrencySymbol.Should().Be("$");
    }

    [Fact]
    public async Task UpsertAsync_OptionalFieldsNull_StoredAsNull()
    {
        _repo.GetByOrgIdAsync(OrgId, Arg.Any<CancellationToken>()).Returns((OrganizationSettings?)null);

        var dto = new UpdateOrgSettingsDto("Solo Nombre", null, null, null, null, null, "$", ReportTheme.Minimal);

        var result = await _sut.UpsertAsync(OrgId, dto);

        result.LogoUrl.Should().BeNull();
        result.Address.Should().BeNull();
        result.TaxId.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UpdateOrgSettingsDto MakeDto(
        string name, string currency, ReportTheme theme = ReportTheme.Professional) =>
        new(name, null, null, null, null, null, currency, theme);
}
