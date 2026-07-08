function required(name: string): string {
  const value = process.env[name];
  if (!value) throw new Error(`Missing required environment variable: ${name}`);
  return value;
}

function optional(name: string, fallback: string): string {
  return process.env[name] || fallback;
}

export const config = {
  port:      parseInt(optional('EMAIL_SERVICE_PORT', '3001'), 10),
  secret:    required('EMAIL_SERVICE_SECRET'),
  smtpHost:  optional('SMTP_HOST', 'live.smtp.mailtrap.io'),
  smtpPort:  parseInt(optional('SMTP_PORT', '587'), 10),
  smtpUser:  optional('SMTP_USER', 'api'),
  smtpPass:  optional('SMTP_PASS', ''),
  emailFrom: optional('EMAIL_FROM', 'Accounting <hello@demomailtrap.co>'),
  appUrl:    optional('APP_URL', 'http://localhost:3000'),
} as const;
