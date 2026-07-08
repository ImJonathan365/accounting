export interface Member {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl?: string | null;
  role: string;
  joinedAtUtc: string;
}

export interface InviteMemberRequest {
  email: string;
  role: string;
}

export interface UpdateMemberRoleRequest {
  role: string;
}

export interface InvitationInfo {
  orgId:         string;
  orgName:       string;
  inviterName:   string;
  invitedEmail:  string;
  role:          string;
  isValid:       boolean;
  invalidReason: string;
}
