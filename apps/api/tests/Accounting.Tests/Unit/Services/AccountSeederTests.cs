using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class AccountSeederTests
{
    private readonly IAccountRepository _repo = Substitute.For<IAccountRepository>();
    private readonly AccountSeeder _sut;

    public AccountSeederTests()
    {
        _sut = new AccountSeeder(_repo);
    }

    [Fact]
    public async Task SeedAsync_Creates30Accounts()
    {
        var captured = await CaptureAsync();

        captured.Should().HaveCount(30);
    }

    [Fact]
    public async Task SeedAsync_CorrectAccountTypeDistribution()
    {
        var captured = await CaptureAsync();

        captured.Count(a => a.Type == AccountType.Asset).Should().Be(9);
        captured.Count(a => a.Type == AccountType.Liability).Should().Be(6);
        captured.Count(a => a.Type == AccountType.Equity).Should().Be(4);
        captured.Count(a => a.Type == AccountType.Income).Should().Be(3);
        captured.Count(a => a.Type == AccountType.Expense).Should().Be(8);
    }

    [Fact]
    public async Task SeedAsync_AllAccountsBelongToOrg()
    {
        var orgId = Guid.NewGuid();
        var captured = await CaptureAsync(orgId);

        captured.Should().AllSatisfy(a => a.OrganizationId.Should().Be(orgId));
    }

    [Fact]
    public async Task SeedAsync_HeaderAccountsAreNotPostable()
    {
        var captured = await CaptureAsync();

        // Top-level group headers (1-digit codes) must not be postable
        var headers = captured.Where(a => !a.Code.Contains('.')).ToList();
        headers.Should().NotBeEmpty();
        headers.Should().AllSatisfy(a => a.IsPostable.Should().BeFalse());
    }

    [Fact]
    public async Task SeedAsync_LeafAccountsArePostable()
    {
        var captured = await CaptureAsync();

        // Leaf accounts are those whose code appears as no other account's parent prefix
        var codes = captured.Select(a => a.Code).ToHashSet();
        var leafAccounts = captured.Where(a =>
            !codes.Any(c => c != a.Code && c.StartsWith(a.Code + ".")));

        leafAccounts.Should().AllSatisfy(a => a.IsPostable.Should().BeTrue());
    }

    [Fact]
    public async Task SeedAsync_CodesAreUnique()
    {
        var captured = await CaptureAsync();

        var codes = captured.Select(a => a.Code).ToList();
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SeedAsync_ParentChildHierarchyIsConsistent()
    {
        var captured = await CaptureAsync();

        var withParent = captured.Where(a => a.ParentId is not null).ToList();
        withParent.Should().NotBeEmpty();

        // Every child's ParentId must point to an account in the same list
        var ids = captured.Select(a => a.Id).ToHashSet();
        withParent.Should().AllSatisfy(a => ids.Should().Contain(a.ParentId!.Value));
    }

    private async Task<List<Account>> CaptureAsync(Guid? orgId = null)
    {
        var captured = new List<Account>();
        _repo.When(r => r.AddRangeAsync(Arg.Any<IEnumerable<Account>>(), Arg.Any<CancellationToken>()))
             .Do(ci => captured.AddRange(ci.Arg<IEnumerable<Account>>()));

        await _sut.SeedAsync(orgId ?? Guid.NewGuid());
        return captured;
    }
}
