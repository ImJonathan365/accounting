using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class RecurringEntryServiceTests
{
    private readonly IRecurringEntryRepository _repo     = Substitute.For<IRecurringEntryRepository>();
    private readonly IAccountRepository        _accounts = Substitute.For<IAccountRepository>();
    private readonly IJournalRepository        _journal  = Substitute.For<IJournalRepository>();
    private readonly RecurringEntryService     _sut;

    private static readonly Guid OrgId    = Guid.NewGuid();
    private static readonly Guid EntryId  = Guid.NewGuid();
    private static readonly Guid AccIdA   = Guid.NewGuid();
    private static readonly Guid AccIdB   = Guid.NewGuid();

    public RecurringEntryServiceTests()
    {
        _sut = new RecurringEntryService(_repo, _accounts, _journal);

        _accounts.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account> { MakeAccount(AccIdA), MakeAccount(AccIdB) });

        _repo.GetByIdAsync(OrgId, EntryId, Arg.Any<CancellationToken>())
            .Returns(MakeTemplate());
    }

    [Fact]
    public async Task CreateAsync_NoLines_ThrowsArgumentException()
    {
        var dto = new CreateRecurringEntryDto("Renta", null, RecurringFrequency.Monthly,
            DateOnly.FromDateTime(DateTime.Today), null, new List<CreateRecurringLineDto>());

        await _sut.Invoking(s => s.CreateAsync(OrgId, dto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*al menos una línea*");
    }

    [Fact]
    public async Task CreateAsync_UnbalancedLines_ThrowsArgumentException()
    {
        var dto = MakeDto(lines: new[] { (AccIdA, 100m, 0m), (AccIdB, 0m, 90m) });

        await _sut.Invoking(s => s.CreateAsync(OrgId, dto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*débitos debe ser igual*");
    }

    [Fact]
    public async Task CreateAsync_AccountNotPostable_ThrowsArgumentException()
    {
        _accounts.GetByIdsAsync(OrgId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Account>
            {
                new() { Id = AccIdA, IsPostable = false },
                MakeAccount(AccIdB),
            });

        var dto = MakeDto(lines: new[] { (AccIdA, 500m, 0m), (AccIdB, 0m, 500m) });

        await _sut.Invoking(s => s.CreateAsync(OrgId, dto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*no encontrada o no es postable*");
    }

    [Fact]
    public async Task CreateAsync_Valid_CreatesEntry()
    {
        RecurringJournalEntry? captured = null;
        await _repo.AddAsync(Arg.Do<RecurringJournalEntry>(e => captured = e), Arg.Any<CancellationToken>());

        var savedTemplate = MakeTemplate();
        _repo.GetByIdAsync(OrgId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(savedTemplate);

        var dto = MakeDto();
        await _sut.CreateAsync(OrgId, dto);

        captured.Should().NotBeNull();
        captured!.OrganizationId.Should().Be(OrgId);
        captured.Description.Should().Be("Renta mensual");
        captured.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetByIdAsync(OrgId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((RecurringJournalEntry?)null);

        await _sut.Invoking(s => s.DeleteAsync(OrgId, Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*recurrente no encontrado*");
    }

    [Fact]
    public async Task DeleteAsync_Found_RemovesEntry()
    {
        var template = MakeTemplate();
        _repo.GetByIdAsync(OrgId, EntryId, Arg.Any<CancellationToken>()).Returns(template);

        await _sut.DeleteAsync(OrgId, EntryId);

        _repo.Received(1).Remove(template);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GeneratePendingAsync_NoPendingTemplates_ReturnsZeroCount()
    {
        _repo.GetPendingAsync(OrgId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<RecurringJournalEntry>());

        var result = await _sut.GeneratePendingAsync(OrgId);

        result.Generated.Should().Be(0);
        result.EntryIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GeneratePendingAsync_OnePendingTemplate_CreatesJournalEntryAndAdvances()
    {
        var today    = DateOnly.FromDateTime(DateTime.Today);
        var template = MakeTemplate(nextDate: today);

        _repo.GetPendingAsync(OrgId, today, Arg.Any<CancellationToken>())
            .Returns(new List<RecurringJournalEntry> { template });

        _repo.TryAdvanceAsync(template.Id, today, today.AddMonths(1), false, Arg.Any<CancellationToken>())
            .Returns(true);

        JournalEntry? capturedEntry = null;
        await _journal.AddAsync(Arg.Do<JournalEntry>(e => capturedEntry = e), Arg.Any<CancellationToken>());

        var result = await _sut.GeneratePendingAsync(OrgId);

        result.Generated.Should().Be(1);
        capturedEntry.Should().NotBeNull();
        capturedEntry!.Status.Should().Be(JournalStatus.Draft);
        capturedEntry.Date.Should().Be(today);
        await _journal.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GeneratePendingAsync_TemplateWithEndDate_DeactivatesWhenPast()
    {
        var today     = DateOnly.FromDateTime(DateTime.Today);
        var endDate   = today; // next date would exceed end
        var template  = MakeTemplate(nextDate: today, endDate: endDate);

        _repo.GetPendingAsync(OrgId, today, Arg.Any<CancellationToken>())
            .Returns(new List<RecurringJournalEntry> { template });
        _repo.TryAdvanceAsync(template.Id, today, today.AddMonths(1), true, Arg.Any<CancellationToken>())
            .Returns(true);
        await _journal.AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());

        var result = await _sut.GeneratePendingAsync(OrgId);

        result.Generated.Should().Be(1);
        await _repo.Received(1).TryAdvanceAsync(template.Id, today, today.AddMonths(1), true, Arg.Any<CancellationToken>());
    }

    private static Account MakeAccount(Guid id) =>
        new() { Id = id, Code = "1.1", Name = "Cuenta", Type = AccountType.Asset, IsPostable = true };

    private static RecurringJournalEntry MakeTemplate(DateOnly? nextDate = null, DateOnly? endDate = null) =>
        new()
        {
            Id             = EntryId,
            OrganizationId = OrgId,
            Description    = "Renta mensual",
            Frequency      = RecurringFrequency.Monthly,
            NextDate       = nextDate ?? DateOnly.FromDateTime(DateTime.Today),
            EndDate        = endDate,
            IsActive       = true,
            Lines          = new List<RecurringJournalLine>
            {
                new() { AccountId = AccIdA, Account = MakeAccount(AccIdA), Debit = 500m, Credit = 0m },
                new() { AccountId = AccIdB, Account = MakeAccount(AccIdB), Debit = 0m,   Credit = 500m },
            }
        };

    private static CreateRecurringEntryDto MakeDto(
        (Guid, decimal, decimal)[]? lines = null) =>
        new("Renta mensual", null, RecurringFrequency.Monthly,
            DateOnly.FromDateTime(DateTime.Today), null,
            (lines ?? new[] { (AccIdA, 500m, 0m), (AccIdB, 0m, 500m) })
                .Select(l => new CreateRecurringLineDto(l.Item1, l.Item2, l.Item3))
                .ToList());
}
