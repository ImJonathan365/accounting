export interface AuditLog {
  id:            string;
  userId:        string;
  userName:      string;
  action:        string;
  entityType:    string;
  entityId:      string;
  details:       string | null;
  createdAtUtc:  string;
}
