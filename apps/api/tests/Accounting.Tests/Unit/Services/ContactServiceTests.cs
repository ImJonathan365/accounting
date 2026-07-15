using Accounting.Application.DTOs;
using Accounting.Application.Interfaces.Repositories;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Accounting.Tests.Unit.Services;

public class ContactServiceTests
{
    private readonly IContactRepository _repo = Substitute.For<IContactRepository>();
    private readonly ContactService     _sut;

    private static readonly Guid OrgId     = Guid.NewGuid();
    private static readonly Guid ContactId = Guid.NewGuid();

    public ContactServiceTests() =>
        _sut = new ContactService(_repo);

    [Fact]
    public async Task CreateAsync_TrimsName_AndStoresCorrectType()
    {
        Contact? captured = null;
        await _repo.AddAsync(Arg.Do<Contact>(c => captured = c), Arg.Any<CancellationToken>());

        await _sut.CreateAsync(OrgId, new CreateContactDto(ContactType.Customer, "  Acme Corp  ", null, null, null, null));

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Acme Corp");
        captured.Type.Should().Be(ContactType.Customer);
        captured.OrganizationId.Should().Be(OrgId);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetDtoByIdAsync(OrgId, ContactId, Arg.Any<CancellationToken>())
            .Returns((ContactDto?)null);

        await _sut.Invoking(s => s.GetByIdAsync(OrgId, ContactId))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Contacto no encontrado*");
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _repo.GetByIdAsync(OrgId, ContactId, Arg.Any<CancellationToken>())
            .Returns((Contact?)null);

        await _sut.Invoking(s => s.UpdateAsync(OrgId, ContactId, new UpdateContactDto(null, null, null, null, null, null, null)))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Contacto no encontrado*");
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdate_OnlyChangesProvidedFields()
    {
        var contact = new Contact
        {
            Id   = ContactId,
            OrganizationId = OrgId,
            Type = ContactType.Customer,
            Name = "Old Name",
        };

        _repo.GetByIdAsync(OrgId, ContactId, Arg.Any<CancellationToken>()).Returns(contact);
        _repo.GetDtoByIdAsync(OrgId, ContactId, Arg.Any<CancellationToken>())
            .Returns(new ContactDto(ContactId, ContactType.Customer, "New Name", null, null, null, null, true, 0));

        await _sut.UpdateAsync(OrgId, ContactId, new UpdateContactDto(null, "New Name", null, null, null, null, null));

        contact.Name.Should().Be("New Name");
        contact.Type.Should().Be(ContactType.Customer); // unchanged
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var items = new List<ContactDto>
        {
            new(Guid.NewGuid(), ContactType.Vendor, "Supplier A", null, null, null, null, true, 2),
        };
        _repo.GetPagedAsync(OrgId, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((items, 1));

        var result = await _sut.GetPagedAsync(OrgId, null, null, 1, 10);

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
        result.TotalPages.Should().Be(1);
    }
}
