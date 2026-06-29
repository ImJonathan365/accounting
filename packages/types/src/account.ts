export type AccountType = "Asset" | "Liability" | "Equity" | "Income" | "Expense";

export interface Account {
  id: string;
  code: string;
  name: string;
  type: AccountType;
  parentId: string | null;
  isPostable: boolean;
  isActive: boolean;
}

export interface CreateAccountRequest {
  code: string;
  name: string;
  type: AccountType;
  parentId?: string;
  isPostable?: boolean;
}
