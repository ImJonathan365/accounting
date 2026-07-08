import nodemailer from 'nodemailer';
import React from 'react';
import { render } from '@react-email/render';
import { config } from './config.js';
import { EmailRequest } from './types.js';
import { WelcomeEmail } from './templates/welcome.js';
import { InviteEmail } from './templates/invite.js';
import { VerifyEmailEmail } from './templates/verify-email.js';
import { ResetPasswordEmail } from './templates/reset-password.js';
import { PasswordChangedEmail } from './templates/password-changed.js';
import { InvitationAcceptedEmail } from './templates/invitation-accepted.js';

const transporter = nodemailer.createTransport({
  host: config.smtpHost,
  port: config.smtpPort,
  auth: {
    user: config.smtpUser,
    pass: config.smtpPass,
  },
});

export async function sendEmail(req: EmailRequest): Promise<void> {
  let subject: string;
  let element: React.ReactElement;

  switch (req.type) {
    case 'welcome':
      subject = `¡Bienvenido a Accounting, ${req.payload.firstName}!`;
      element = React.createElement(WelcomeEmail, req.payload);
      break;
    case 'invite':
      subject = `${req.payload.inviterName} te invitó a unirte a ${req.payload.orgName}`;
      element = React.createElement(InviteEmail, req.payload);
      break;
    case 'verify-email':
      subject = 'Verifica tu dirección de correo electrónico';
      element = React.createElement(VerifyEmailEmail, req.payload);
      break;
    case 'reset-password':
      subject = 'Solicitud para restablecer tu contraseña';
      element = React.createElement(ResetPasswordEmail, req.payload);
      break;
    case 'password-changed':
      subject = 'Tu contraseña fue cambiada exitosamente';
      element = React.createElement(PasswordChangedEmail, req.payload);
      break;
    case 'invitation-accepted':
      subject = `¡Bienvenido a ${req.payload.orgName}!`;
      element = React.createElement(InvitationAcceptedEmail, req.payload);
      break;
  }

  const html = await render(element);

  if (!config.smtpPass) {
    console.log(`[email-service] DRY RUN — would send "${subject}" to ${req.to}`);
    return;
  }

  await transporter.sendMail({
    from:    config.emailFrom,
    to:      req.to,
    subject,
    html,
  });
}
