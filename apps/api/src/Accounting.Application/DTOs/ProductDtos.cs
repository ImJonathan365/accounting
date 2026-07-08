namespace Accounting.Application.DTOs;

public record ProductDto(
    Guid    Id,
    string  Name,
    string? Description,
    decimal DefaultPrice,
    Guid    AccountId,
    string  AccountCode,
    string  AccountName,
    Guid?   TaxRateId,
    string? TaxRateName,
    decimal TaxRate,
    bool    IsActive);

public record CreateProductDto(
    string  Name,
    string? Description,
    decimal DefaultPrice,
    Guid    AccountId,
    Guid?   TaxRateId);

public record UpdateProductDto(
    string?  Name,
    string?  Description,
    decimal? DefaultPrice,
    Guid?    AccountId,
    Guid?    TaxRateId,
    bool?    IsActive);
