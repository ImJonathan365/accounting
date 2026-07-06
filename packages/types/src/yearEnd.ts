export interface YearEndStatus {
  year:           number;
  isClosed:       boolean;
  closedByName:   string | null;
  closedAtUtc:    string | null;
  journalEntryId: string | null;
}

export interface YearEndCloseRequest {
  year:                       number;
  retainedEarningsAccountId:  string;
}
