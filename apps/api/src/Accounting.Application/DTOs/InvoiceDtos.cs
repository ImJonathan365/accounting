using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record ContactDto(
    Guid        Id,
    ContactType Type,
    string      Name,
    string?     Email,
    string?     Phone,
    string?     Address,
    string?     Notes,
    bool        IsActive,
    int         InvoiceCount);

public record CreateContactDto(
    ContactType Type,
    string      Name,
    string?     Email,
    string?     Phone,
    string?     Address,
    string?     Notes);

public record UpdateContactDto(
    ContactType? Type,
    string?      Name,
    string?      Email,
    string?      Phone,
    string?      Address,
    string?      Notes,
    bool?        IsActive);

public record InvoiceLineDto(
    Guid    Id,
    string  Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    Guid    AccountId,
    string  AccountCode,
    string  AccountName,
    Guid?   TaxRateId,
    string? TaxRateName,
    decimal TaxRatePercent,
    decimal TaxAmount,
    decimal LineTotal);

public record CreateInvoiceLineDto(
    string  Description,
    decimal Quantity,
    decimal UnitPrice,
    Guid    AccountId,
    Guid?   TaxRateId);

public record InvoicePaymentDto(
    Guid    Id,
    string  Date,
    decimal Amount,
    Guid    PaymentAccountId,
    string  PaymentAccountName,
    Guid?   JournalEntryId,
    string? Notes);

public record CreatePaymentDto(
    string  Date,
    decimal Amount,
    Guid    PaymentAccountId,
    string? Notes);

public record InvoiceDto(
    Guid          Id,
    InvoiceType   Type,
    Guid          ContactId,
    string        ContactName,
    string        Number,
    string        Date,
    string        DueDate,
    InvoiceStatus Status,
    string        StatusLabel,
    Guid          ArApAccountId,
    string        ArApAccountName,
    string?       Notes,
    Guid?         JournalEntryId,
    decimal       SubTotal,
    decimal       TaxTotal,
    decimal       Total,
    decimal       Paid,
    decimal       Balance,
    List<InvoiceLineDto>    Lines,
    List<InvoicePaymentDto> Payments);

public record CreateInvoiceDto(
    InvoiceType          Type,
    Guid                 ContactId,
    string               Number,
    string               Date,
    string               DueDate,
    Guid                 ArApAccountId,
    string?              Notes,
    List<CreateInvoiceLineDto> Lines);
