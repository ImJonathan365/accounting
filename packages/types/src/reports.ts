import type { AccountType } from "./account";
import type { JournalStatus } from "./journal";

// ── Dashboard ─────────────────────────────────────────────────────────────────

export interface MonthlyTrend {
  year:    number;
  month:   number;
  label:   string;
  income:  number;
  expense: number;
}

export interface FinancialRatios {
  liquidityRatio:   number | null;
  debtRatio:        number;
  netProfitMargin:  number | null;
}

export interface RecentEntry {
  id: string;
  date: string;
  description: string;
  reference?: string | null;
  totalDebit: number;
  status: JournalStatus;
}

export interface OverdueInvoice {
  id:          string;
  number:      string;
  contactName: string;
  dueDate:     string;
  type:        string;
  balance:     number;
}

export interface DashboardSummary {
  totalAssets:        number;
  totalLiabilities:   number;
  totalEquity:        number;
  netIncome:          number;
  isProfit:           boolean;
  isBalanced:         boolean;
  recentEntries:      RecentEntry[];
  currencySymbol:     string;
  periodLabel:        string;
  monthlyTrend:       MonthlyTrend[];
  ratios:             FinancialRatios;
  overdueCount:       number;
  overdueAmount:      number;
  pendingReceivable:  number;
  pendingPayable:     number;
  overdueInvoices:    OverdueInvoice[];
}

// ── Cash Flow Statement ───────────────────────────────────────────────────────

export interface CashFlowLine {
  accountCode: string;
  accountName: string;
  amount:      number;
}

export interface CashFlowActivitySection {
  title:     string;
  lines:     CashFlowLine[];
  netIncome: number;
  total:     number;
}

export interface CashFlow {
  from:          string;
  to:            string;
  beginningCash: number;
  operating:     CashFlowActivitySection;
  investing:     CashFlowActivitySection;
  financing:     CashFlowActivitySection;
  netChange:     number;
  endingCash:    number;
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
  accountId:      string;
  accountCode:    string;
  accountName:    string;
  accountType:    AccountType;
  from:           string;
  to:             string;
  openingBalance: number;
  lines:          LedgerLine[];
  closingBalance: number;
  totalLines:     number;
  page:           number;
  pageSize:       number;
  totalPages:     number;
}
