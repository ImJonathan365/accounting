export enum ReportTheme {
  Minimal      = "Minimal",
  Professional = "Professional",
  Corporate    = "Corporate",
}

export interface OrgSettings {
  organizationId: string;
  companyName:    string;
  logoUrl:        string | null;
  address:        string | null;
  taxId:          string | null;
  phone:          string | null;
  email:          string | null;
  currencySymbol: string;
  theme:          ReportTheme;
}

export interface UpdateOrgSettingsRequest {
  companyName:    string;
  logoUrl:        string | null;
  address:        string | null;
  taxId:          string | null;
  phone:          string | null;
  email:          string | null;
  currencySymbol: string;
  theme:          ReportTheme;
}
