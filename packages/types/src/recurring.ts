export type RecurringFrequency = "Weekly" | "Biweekly" | "Monthly" | "Quarterly" | "Annually";

export interface RecurringLine {
  id:          string;
  accountId:   string;
  accountCode: string;
  accountName: string;
  debit:       number;
  credit:      number;
  note:        string | null;
}

export interface RecurringEntry {
  id:             string;
  description:    string;
  reference:      string | null;
  frequency:      RecurringFrequency;
  frequencyLabel: string;
  nextDate:       string;
  endDate:        string | null;
  isActive:       boolean;
  lines:          RecurringLine[];
}

export interface CreateRecurringLine {
  accountId: string;
  debit:     number;
  credit:    number;
  note?:     string;
}

export interface CreateRecurringEntryRequest {
  description: string;
  reference?:  string;
  frequency:   RecurringFrequency;
  startDate:   string;
  endDate?:    string;
  lines:       CreateRecurringLine[];
}

export interface UpdateRecurringEntryRequest {
  description?: string;
  reference?:   string | null;
  frequency?:   RecurringFrequency;
  nextDate?:    string;
  endDate?:     string | null;
  isActive?:    boolean;
}

export interface GeneratePendingResult {
  generated: number;
  entryIds:  string[];
}
