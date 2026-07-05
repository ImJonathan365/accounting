export type JournalStatus = "Draft" | "Posted" | "Voided";

export interface JournalLine {
  id: string;
  accountId: string;
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
  note?: string;
}

export interface JournalEntry {
  id: string;
  date: string;
  description: string;
  reference?: string;
  status: JournalStatus;
  totalDebit: number;
  totalCredit: number;
  lines: JournalLine[];
  createdAtUtc: string;
  voidsEntryId?:    string | null;
  voidedByEntryId?: string | null;
  voidReason?:      string | null;
  voidedAtUtc?:     string | null;
}

export interface JournalEntrySummary {
  id: string;
  date: string;
  description: string;
  reference?: string;
  status: JournalStatus;
  totalDebit: number;
  createdAtUtc: string;
  voidsEntryId?:    string | null;
  voidedByEntryId?: string | null;
}

export interface CreateJournalLineRequest {
  accountId: string;
  debit: number;
  credit: number;
  note?: string;
}

export interface CreateJournalEntryRequest {
  date: string;
  description: string;
  reference?: string;
  lines: CreateJournalLineRequest[];
  isDraft?: boolean;
}

export interface UpdateJournalEntryRequest {
  date: string;
  description: string;
  reference?: string;
  lines: CreateJournalLineRequest[];
}

export interface VoidJournalEntryRequest {
  reason:   string | null;
  voidDate: string | null;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
