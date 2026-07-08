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
import { InvitationAcceptedPayload } from '../types.js';

const ROLE_LABELS: Record<string, string> = {
  owner:  'Propietario',
  admin:  'Administrador',
  member: 'Miembro',
};

export function InvitationAcceptedEmail({ firstName, orgName, role, appUrl }: InvitationAcceptedPayload) {
  const roleLabel = ROLE_LABELS[role] ?? role;
  return (
    <Html lang="es">
      <Head />
      <Preview>Ahora eres parte de {orgName}</Preview>
      <Body style={body}>
        <Container style={container}>
          <Section style={iconSection}>
            <div style={iconCircle}>✓</div>
          </Section>
          <Heading style={h1}>¡Bienvenido a {orgName}!</Heading>
          <Text style={text}>
            Hola {firstName}, ya eres parte del equipo de <strong>{orgName}</strong> como{' '}
            <strong>{roleLabel}</strong>.
          </Text>
          <Text style={text}>
            Ingresa a la aplicación para comenzar a colaborar.
          </Text>
          <Section style={btnSection}>
            <Button style={button} href={appUrl}>
              Ir a la aplicación
            </Button>
          </Section>
          <Hr style={hr} />
          <Text style={footer}>
            Este mensaje fue generado automáticamente. Por favor no respondas a este correo.
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

const iconSection: React.CSSProperties = { textAlign: 'center', margin: '0 0 24px' };

const iconCircle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  width: '48px',
  height: '48px',
  borderRadius: '50%',
  backgroundColor: '#ede9fe',
  color: '#4f46e5',
  fontSize: '22px',
  fontWeight: '700',
};

const h1: React.CSSProperties = {
  color: '#0f172a',
  fontSize: '24px',
  fontWeight: '700',
  margin: '0 0 16px',
  textAlign: 'center',
};

const text: React.CSSProperties = {
  color: '#475569',
  fontSize: '15px',
  lineHeight: '1.6',
  margin: '0 0 16px',
};

const btnSection: React.CSSProperties = { margin: '24px 0' };

const button: React.CSSProperties = {
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

const footer: React.CSSProperties = {
  color: '#94a3b8',
  fontSize: '12px',
  margin: '0',
};
