using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class TaxRateServiceTests
{
    private readonly ITaxRateRepository  _repo     = Substitute.For<ITaxRateRepository>();
    private readonly IAccountRepository  _accounts = Substitute.For<IAccountRepository>();
    private readonly TaxRateService      _sut;

    private static readonly Guid OrgId     = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid RateId    = Guid.NewGuid();

    public TaxRateServiceTests()
    {
        _sut = new TaxRateService(_repo, _accounts);

        _repo.NameExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _accounts.GetByIdAsync(AccountId, OrgId, Arg.Any<CancellationToken>())
            .Returns(MakeAccount());

        _repo.GetByIdAsync(OrgId, RateId, Arg.Any<CancellationToken>())
            .Returns(MakeTaxRate());
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperation()
    {
        _repo.NameExistsAsync(OrgId, "IVA 16%", Arg.Any<CancellationToken>()).Returns(true);

        await _sut.Invoking(s => s.CreateAsync(OrgId, new("IVA 16%", 0.16m, AccountId)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ya existe una tasa*");
    }

    [Fact]
    public async Task CreateAsync_AccountNotInOrg_ThrowsArgumentException()
    {
        _accounts.GetByIdAsync(Arg.Any<Guid>(), OrgId, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        await _sut.Invoking(s => s.CreateAsync(OrgId, new("IVA 16%", 0.16m, Guid.NewGuid())))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cuenta de impuesto*");
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesAndReturns()
    {
        TaxRate? captured = null;
        await _repo.AddAsync(Arg.Do<TaxRate>(r => captured = r), Arg.Any<CancellationToken>());

        var fullRate = MakeTaxRate();
        _repo.GetByIdAsync(OrgId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(fullRate);

        var result = await _sut.CreateAsync(OrgId, new("IVA 16%", 0.16m, AccountId));

        captured.Should().NotBeNull();
        captured!.OrganizationId.Should().Be(OrgId);
        captured.Rate.Should().Be(0.16m);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetByIdAsync(OrgId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TaxRate?)null);

        await _sut.Invoking(s => s.GetByIdAsync(OrgId, Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Tasa de impuesto no encontrada*");
    }

    [Fact]
    public async Task UpdateAsync_ChangesRate_UpdatesEntity()
    {
        var rate = MakeTaxRate();
        _repo.GetByIdAsync(OrgId, RateId, Arg.Any<CancellationToken>()).Returns(rate);

        var result = await _sut.UpdateAsync(OrgId, RateId, new(null, 0.19m, null, null));

        rate.Rate.Should().Be(0.19m);
    }

    [Fact]
    public async Task UpdateAsync_NewAccountId_UpdatesNavProperty()
    {
        var rate       = MakeTaxRate();
        var newAccount = MakeAccount(Guid.NewGuid());
        _repo.GetByIdAsync(OrgId, RateId, Arg.Any<CancellationToken>()).Returns(rate);
        _accounts.GetByIdAsync(newAccount.Id, OrgId, Arg.Any<CancellationToken>()).Returns(newAccount);

        await _sut.UpdateAsync(OrgId, RateId, new(null, null, newAccount.Id, null));

        rate.TaxAccountId.Should().Be(newAccount.Id);
        rate.TaxAccount.Should().Be(newAccount);
    }

    private static Account MakeAccount(Guid? id = null) =>
        new() { Id = id ?? AccountId, Code = "2.1.01", Name = "IVA por Pagar", Type = AccountType.Liability, IsPostable = true };

    private static TaxRate MakeTaxRate() =>
        new() { Id = RateId, OrganizationId = OrgId, Name = "IVA 16%", Rate = 0.16m, TaxAccountId = AccountId, TaxAccount = MakeAccount(), IsActive = true };
}
