using Accounting.Domain.Common;
using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class OrganizationSettings : BaseEntity
{
    public Guid           OrganizationId  { get; set; }
    public Organization   Organization    { get; set; } = default!;
    public string         CompanyName     { get; set; } = default!;
    public string?        LogoUrl         { get; set; }
    public string?        Address         { get; set; }
    public string?        TaxId           { get; set; }
    public string?        Phone           { get; set; }
    public string?        Email           { get; set; }
    public string         CurrencySymbol  { get; set; } = "$";
    public ReportTheme    Theme           { get; set; } = ReportTheme.Professional;
}
