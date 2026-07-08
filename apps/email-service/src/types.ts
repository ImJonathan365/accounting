export interface WelcomePayload {
  firstName: string;
  orgName:   string;
}

export interface InvitePayload {
  firstName:   string;
  orgName:     string;
  inviterName: string;
  role:        string;
  acceptUrl:   string;
  declineUrl:  string;
}

export interface InvitationAcceptedPayload {
  firstName: string;
  orgName:   string;
  role:      string;
  appUrl:    string;
}

export interface VerifyEmailPayload {
  firstName:       string;
  verificationUrl: string;
}

export interface ResetPasswordPayload {
  firstName: string;
  resetUrl:  string;
}

export interface PasswordChangedPayload {
  firstName: string;
}

export type EmailRequest =
  | { type: 'welcome';           to: string; payload: WelcomePayload          }
  | { type: 'invite';            to: string; payload: InvitePayload           }
  | { type: 'verify-email';      to: string; payload: VerifyEmailPayload      }
  | { type: 'reset-password';    to: string; payload: ResetPasswordPayload    }
  | { type: 'password-changed';     to: string; payload: PasswordChangedPayload     }
  | { type: 'invitation-accepted';  to: string; payload: InvitationAcceptedPayload  };

export type EmailType = EmailRequest['type'];

export const EMAIL_TYPES: EmailType[] = ['welcome', 'invite', 'verify-email', 'reset-password', 'password-changed', 'invitation-accepted'];
