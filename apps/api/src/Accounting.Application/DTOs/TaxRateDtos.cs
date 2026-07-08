namespace Accounting.Application.DTOs;

public record TaxRateDto(
    Guid    Id,
    string  Name,
    decimal Rate,
    Guid    TaxAccountId,
    string  TaxAccountName,
    bool    IsActive);

public record CreateTaxRateDto(
    string  Name,
    decimal Rate,
    Guid    TaxAccountId);

public record UpdateTaxRateDto(
    string?  Name,
    decimal? Rate,
    Guid?    TaxAccountId,
    bool?    IsActive);
