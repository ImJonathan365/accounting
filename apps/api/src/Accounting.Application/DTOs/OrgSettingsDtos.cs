using Accounting.Domain.Enums;

namespace Accounting.Application.DTOs;

public record OrgSettingsDto(
    Guid        OrganizationId,
    string      CompanyName,
    string?     LogoUrl,
    string?     Address,
    string?     TaxId,
    string?     Phone,
    string?     Email,
    string      CurrencySymbol,
    ReportTheme Theme);

public record UpdateOrgSettingsDto(
    string      CompanyName,
    string?     LogoUrl,
    string?     Address,
    string?     TaxId,
    string?     Phone,
    string?     Email,
    string      CurrencySymbol,
    ReportTheme Theme);

public record ExportResult(byte[] Data, string ContentType, string FileName);
