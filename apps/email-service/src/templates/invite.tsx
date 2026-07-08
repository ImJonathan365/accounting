import {
  Body,
  Button,
  Container,
  Head,
  Heading,
  Hr,
  Html,
  Preview,
  Section,
  Text,
} from '@react-email/components';
import * as React from 'react';
import { InvitePayload } from '../types.js';

const ROLE_LABELS: Record<string, string> = {
  owner:  'Propietario',
  admin:  'Administrador',
  member: 'Miembro',
};

export function InviteEmail({ firstName, orgName, inviterName, role, acceptUrl, declineUrl }: InvitePayload) {
  const roleLabel = ROLE_LABELS[role] ?? role;
  return (
    <Html lang="es">
      <Head />
      <Preview>{inviterName} te invitó a unirte a {orgName}</Preview>
      <Body style={body}>
        <Container style={container}>
          <Heading style={h1}>Te invitaron a {orgName}</Heading>
          <Text style={text}>
            Hola {firstName}, <strong>{inviterName}</strong> te ha invitado a unirte a{' '}
            <strong>{orgName}</strong> como <strong>{roleLabel}</strong>.
          </Text>
          <Text style={text}>
            Acepta la invitación para acceder a la organización. El enlace expira en{' '}
            <strong>7 días</strong>.
          </Text>
          <Section style={btnSection}>
            <Button style={acceptBtn} href={acceptUrl}>
              Aceptar invitación
            </Button>
          </Section>
          <Hr style={hr} />
          <Text style={declineText}>
            ¿No quieres unirte?{' '}
            <a href={declineUrl} style={declineLink}>Rechazar invitación</a>
          </Text>
          <Text style={footer}>
            Si no esperabas esta invitación, puedes ignorar este mensaje con seguridad.
          </Text>
        </Container>
      </Body>
    </Html>
  );
}

const body: React.CSSProperties = {
  backgroundColor: '#f1f5f9',
  fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
  padding: '40px 0',
};

const container: React.CSSProperties = {
  backgroundColor: '#ffffff',
  borderRadius: '8px',
  maxWidth: '560px',
  margin: '0 auto',
  padding: '40px',
};

const h1: React.CSSProperties = {
  color: '#0f172a',
  fontSize: '24px',
  fontWeight: '700',
  margin: '0 0 16px',
};

const text: React.CSSProperties = {
  color: '#475569',
  fontSize: '15px',
  lineHeight: '1.6',
  margin: '0 0 16px',
};

const btnSection: React.CSSProperties = { margin: '24px 0' };

const acceptBtn: React.CSSProperties = {
  backgroundColor: '#4f46e5',
  borderRadius: '6px',
  color: '#ffffff',
  display: 'inline-block',
  fontSize: '14px',
  fontWeight: '600',
  padding: '12px 24px',
  textDecoration: 'none',
};

const hr: React.CSSProperties = { borderColor: '#e2e8f0', margin: '0 0 16px' };

const declineText: React.CSSProperties = {
  color: '#94a3b8',
  fontSize: '13px',
  margin: '0 0 16px',
};

const declineLink: React.CSSProperties = {
  color: '#64748b',
  textDecoration: 'underline',
};

const footer: React.CSSProperties = {
  color: '#94a3b8',
  fontSize: '12px',
  margin: '0',
};
