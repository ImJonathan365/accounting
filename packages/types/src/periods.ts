export interface Period {
  year:         number;
  month:        number;
  monthName:    string;
  isClosed:     boolean;
  closedByName: string | null;
  closedAtUtc:  string | null;
}

export interface ClosePeriodRequest {
  year:  number;
  month: number;
}
