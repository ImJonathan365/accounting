export interface BudgetLine {
  id:          string;
  accountId:   string;
  accountCode: string;
  accountName: string;
  accountType: string;
  month:       number;
  amount:      number;
}

export interface Budget {
  id:       string;
  year:     number;
  name:     string;
  isActive: boolean;
  lines:    BudgetLine[];
}

export interface BudgetVsActualLine {
  accountId:   string;
  accountCode: string;
  accountName: string;
  accountType: string;
  budget:      number[];
  actual:      number[];
  totalBudget: number;
  totalActual: number;
  variance:    number;
}

export interface BudgetVsActual {
  year:       number;
  budgetName: string;
  lines:      BudgetVsActualLine[];
}

export interface CreateBudgetRequest { name: string; year: number; }
export interface UpsertBudgetLineRequest { accountId: string; month: number; amount: number; }
