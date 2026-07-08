export interface OpeningBalanceLine { accountId: string; debit: number; credit: number; }

export interface SetOpeningBalancesRequest {
  date:        string;
  description?: string;
  lines:       OpeningBalanceLine[];
}

export interface OpeningBalanceResult {
  journalEntryId: string;
  date:           string;
  totalDebit:     number;
  totalCredit:    number;
}
