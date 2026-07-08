export type ContactType   = "Customer" | "Vendor" | "Both";
export type InvoiceType   = "Receivable" | "Payable";
export type InvoiceStatus = "Draft" | "Issued" | "PartiallyPaid" | "Paid" | "Void";

export interface Contact {
  id:           string;
  type:         ContactType;
  name:         string;
  email:        string | null;
  phone:        string | null;
  address:      string | null;
  notes:        string | null;
  isActive:     boolean;
  invoiceCount: number;
}

export interface CreateContactRequest {
  type:     ContactType;
  name:     string;
  email?:   string;
  phone?:   string;
  address?: string;
  notes?:   string;
}

export interface UpdateContactRequest {
  type?:     ContactType;
  name?:     string;
  email?:    string;
  phone?:    string;
  address?:  string;
  notes?:    string;
  isActive?: boolean;
}

export interface InvoiceLine {
  id:              string;
  description:     string;
  quantity:        number;
  unitPrice:       number;
  subtotal:        number;
  accountId:       string;
  accountCode:     string;
  accountName:     string;
  taxRateId:       string | null;
  taxRateName:     string | null;
  taxRatePercent:  number;
  taxAmount:       number;
  lineTotal:       number;
}

export interface InvoicePayment {
  id:                 string;
  date:               string;
  amount:             number;
  paymentAccountId:   string;
  paymentAccountName: string;
  journalEntryId:     string | null;
  notes:              string | null;
}

export interface Invoice {
  id:              string;
  type:            InvoiceType;
  contactId:       string;
  contactName:     string;
  number:          string;
  date:            string;
  dueDate:         string;
  status:          InvoiceStatus;
  statusLabel:     string;
  arApAccountId:   string;
  arApAccountName: string;
  notes:           string | null;
  journalEntryId:  string | null;
  subTotal:        number;
  taxTotal:        number;
  total:           number;
  paid:            number;
  balance:         number;
  lines:           InvoiceLine[];
  payments:        InvoicePayment[];
}

export interface CreateInvoiceLineRequest {
  description: string;
  quantity:    number;
  unitPrice:   number;
  accountId:   string;
  taxRateId?:  string;
}

export interface CreateInvoiceRequest {
  type:          InvoiceType;
  contactId:     string;
  number:        string;
  date:          string;
  dueDate:       string;
  arApAccountId: string;
  notes?:        string;
  lines:         CreateInvoiceLineRequest[];
}

export interface CreatePaymentRequest {
  date:             string;
  amount:           number;
  paymentAccountId: string;
  notes?:           string;
}
