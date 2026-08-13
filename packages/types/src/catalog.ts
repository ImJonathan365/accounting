export interface TaxRate {
  id:             string;
  name:           string;
  rate:           number;
  taxAccountId:   string;
  taxAccountName: string;
  isActive:       boolean;
}

export interface CreateTaxRateRequest {
  name:         string;
  rate:         number;
  taxAccountId: string;
}

export interface UpdateTaxRateRequest {
  name?:         string;
  rate?:         number;
  taxAccountId?: string;
  isActive?:     boolean;
}

export interface Product {
  id:           string;
  name:         string;
  description:  string | null;
  defaultPrice: number;
  accountId:    string;
  accountCode:  string;
  accountName:  string;
  taxRateId:    string | null;
  taxRateName:  string | null;
  taxRate:      number;
  isActive:     boolean;
}

export interface CreateProductRequest {
  name:         string;
  description?: string;
  defaultPrice: number;
  accountId:    string;
  taxRateId?:   string;
}

export interface UpdateProductRequest {
  name?:         string;
  description?:  string | null;
  defaultPrice?: number;
  accountId?:    string;
  taxRateId?:    string | null;
  isActive?:     boolean;
}
