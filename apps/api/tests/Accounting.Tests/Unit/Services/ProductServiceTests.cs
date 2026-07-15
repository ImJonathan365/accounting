using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class ProductServiceTests
{
    private readonly IProductRepository  _repo     = Substitute.For<IProductRepository>();
    private readonly IAccountRepository  _accounts = Substitute.For<IAccountRepository>();
    private readonly ITaxRateRepository  _taxRates = Substitute.For<ITaxRateRepository>();
    private readonly ProductService      _sut;

    private static readonly Guid OrgId     = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid TaxRateId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    public ProductServiceTests()
    {
        _sut = new ProductService(_repo, _accounts, _taxRates);

        _repo.NameExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _accounts.GetByIdAsync(AccountId, OrgId, Arg.Any<CancellationToken>())
            .Returns(MakeAccount());
        _taxRates.GetByIdAsync(OrgId, TaxRateId, Arg.Any<CancellationToken>())
            .Returns(MakeTaxRate());
        _repo.GetByIdAsync(OrgId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(MakeProduct());
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperation()
    {
        _repo.NameExistsAsync(OrgId, "Consultoría", Arg.Any<CancellationToken>()).Returns(true);

        await _sut.Invoking(s => s.CreateAsync(OrgId, new("Consultoría", null, 100m, AccountId, null)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ya existe un producto*");
    }

    [Fact]
    public async Task CreateAsync_AccountNotInOrg_ThrowsArgumentException()
    {
        _accounts.GetByIdAsync(Arg.Any<Guid>(), OrgId, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        await _sut.Invoking(s => s.CreateAsync(OrgId, new("X", null, 10m, Guid.NewGuid(), null)))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cuenta de ingreso/gasto*");
    }

    [Fact]
    public async Task CreateAsync_InvalidTaxRate_ThrowsArgumentException()
    {
        _taxRates.GetByIdAsync(OrgId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TaxRate?)null);

        await _sut.Invoking(s => s.CreateAsync(OrgId, new("X", null, 10m, AccountId, Guid.NewGuid())))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tasa de impuesto*");
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesProduct()
    {
        Product? captured = null;
        await _repo.AddAsync(Arg.Do<Product>(p => captured = p), Arg.Any<CancellationToken>());

        await _sut.CreateAsync(OrgId, new("Consultoría", "Servicio mensual", 500m, AccountId, TaxRateId));

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Consultoría");
        captured.DefaultPrice.Should().Be(500m);
        captured.OrganizationId.Should().Be(OrgId);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetByIdAsync(OrgId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        await _sut.Invoking(s => s.GetByIdAsync(OrgId, Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Producto no encontrado*");
    }

    [Fact]
    public async Task UpdateAsync_GuidEmpty_ClearsTaxRate()
    {
        var product = MakeProduct();
        product.TaxRateId = TaxRateId;
        _repo.GetByIdAsync(OrgId, ProductId, Arg.Any<CancellationToken>()).Returns(product);

        await _sut.UpdateAsync(OrgId, ProductId, new(null, null, null, null, Guid.Empty, null));

        product.TaxRateId.Should().BeNull();
    }

    private static Account MakeAccount() =>
        new() { Id = AccountId, Code = "4.1", Name = "Ingresos", Type = AccountType.Income, IsPostable = true };

    private static TaxRate MakeTaxRate() =>
        new() { Id = TaxRateId, OrganizationId = OrgId, Name = "IVA", Rate = 0.16m, TaxAccountId = Guid.NewGuid() };

    private static Product MakeProduct() =>
        new() { Id = ProductId, OrganizationId = OrgId, Name = "Consultoría", DefaultPrice = 500m, AccountId = AccountId, Account = MakeAccount() };
}
