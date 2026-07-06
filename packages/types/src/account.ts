export type AccountType = "Asset" | "Liability" | "Equity" | "Income" | "Expense";

export type CashFlowSection = "None" | "Cash" | "Operating" | "Investing" | "Financing";

export interface Account {
  id:              string;
  code:            string;
  name:            string;
  type:            AccountType;
  parentId:        string | null;
  isPostable:      boolean;
  isActive:        boolean;
  cashFlowSection: CashFlowSection;
}

export interface CreateAccountRequest {
  code:             string;
  name:             string;
  type:             AccountType;
  parentId?:        string;
  isPostable?:      boolean;
  cashFlowSection?: CashFlowSection;
}

export interface UpdateAccountRequest {
  name:            string;
  isPostable:      boolean;
  cashFlowSection: CashFlowSection;
}
