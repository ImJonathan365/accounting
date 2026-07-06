export type BankTransactionType   = "Credit" | "Debit";
export type BankTransactionStatus = "Pending" | "Matched" | "Excluded";

export interface BankAccount {
  id:                string;
  name:              string;
  bankName:          string | null;
  accountNumber:     string | null;
  linkedAccountId:   string;
  linkedAccountCode: string;
  linkedAccountName: string;
  isActive:          boolean;
  pendingCount:      number;
}

export interface BankTransaction {
  id:             string;
  bankAccountId:  string;
  date:           string;
  description:    string;
  amount:         number;
  type:           BankTransactionType;
  status:         BankTransactionStatus;
  journalEntryId: string | null;
  notes:          string | null;
}

export interface UnmatchedJournal {
  journalEntryId: string;
  date:           string;
  reference:      string;
  description:    string;
  amount:         number;
}

export interface BankReconciliation {
  bankAccount:            BankAccount;
  unmatchedTransactions:  BankTransaction[];
  unmatchedJournalEntries: UnmatchedJournal[];
  bankBalance:            number;
  bookBalance:            number;
  difference:             number;
}

export interface CreateBankAccountRequest {
  name:            string;
  bankName?:       string;
  accountNumber?:  string;
  linkedAccountId: string;
}

export interface ImportBankTransactionRow {
  date:        string;
  description: string;
  amount:      number;
  type:        BankTransactionType;
}
