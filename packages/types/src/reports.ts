import type { AccountType } from "./account";
import type { JournalStatus } from "./journal";

// ── Dashboard ─────────────────────────────────────────────────────────────────

export interface RecentEntry {
  id: string;
  date: string;
  description: string;
  reference?: string | null;
  totalDebit: number;
  status: JournalStatus;
}

export interface DashboardSummary {
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  netIncome: number;
  isProfit: boolean;
  isBalanced: boolean;
  recentEntries: RecentEntry[];
  currencySymbol: string;
  periodLabel: string;
}

export interface TrialBalanceLine {
  accountId: string;
  code: string;
  name: string;
  type: AccountType;
  totalDebit: number;
  totalCredit: number;
  debitBalance: number;
  creditBalance: number;
}

export interface TrialBalance {
  from: string;
  to: string;
  lines: TrialBalanceLine[];
  totalDebit: number;
  totalCredit: number;
  totalDebitBalance: number;
  totalCreditBalance: number;
  isBalanced: boolean;
}

// ── Balance Sheet ─────────────────────────────────────────────────────────────

export interface BalanceSheetLine {
  accountId: string;
  code: string;
  name: string;
  balance: number;
}

export interface BalanceSheetSection {
  sectionCode: string;
  sectionName: string;
  lines: BalanceSheetLine[];
  subtotal: number;
}

export interface BalanceSheetGroup {
  title: string;
  sections: BalanceSheetSection[];
  total: number;
}

export interface BalanceSheet {
  asOf: string;
  assets: BalanceSheetGroup;
  liabilities: BalanceSheetGroup;
  equity: BalanceSheetGroup;
  netIncome: number;
  totalEquity: number;
  totalLiabilitiesAndEquity: number;
  isBalanced: boolean;
}

// ── Income Statement ──────────────────────────────────────────────────────────

export interface IncomeStatementLine {
  accountId: string;
  code: string;
  name: string;
  parentCode?: string;
  parentName?: string;
  amount: number;
}

export interface IncomeStatementSection {
  title: string;
  lines: IncomeStatementLine[];
  total: number;
}

export interface IncomeStatement {
  from: string;
  to: string;
  income: IncomeStatementSection;
  expenses: IncomeStatementSection;
  netIncome: number;
  isProfit: boolean;
}

// ── Ledger ────────────────────────────────────────────────────────────────────

export interface LedgerLine {
  entryId: string;
  date: string;
  description: string;
  reference?: string | null;
  debit: number;
  credit: number;
  runningBalance: number;
}

export interface Ledger {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountType: AccountType;
  from: string;
  to: string;
  openingBalance: number;
  lines: LedgerLine[];
  closingBalance: number;
}
