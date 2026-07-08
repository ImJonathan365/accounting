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
import { VerifyEmailPayload } from '../types.js';

export function VerifyEmailEmail({ firstName, verificationUrl }: VerifyEmailPayload) {
  return (
    <Html lang="es">
      <Head />
      <Preview>Verifica tu dirección de correo electrónico</Preview>
      <Body style={body}>
        <Container style={container}>
          <Heading style={h1}>Verifica tu email</Heading>
          <Text style={text}>
            Hola {firstName}, gracias por registrarte. Para activar tu cuenta haz clic en el
            botón de abajo.
          </Text>
          <Section style={btnSection}>
            <Button style={button} href={verificationUrl}>
              Verificar email
            </Button>
          </Section>
          <Text style={note}>
            Este enlace expira en <strong>24 horas</strong>. Si no creaste esta cuenta, ignora
            este mensaje.
          </Text>
          <Hr style={hr} />
          <Text style={footer}>
            Si el botón no funciona, copia y pega este enlace en tu navegador:{' '}
            <span style={{ color: '#4f46e5' }}>{verificationUrl}</span>
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
  margin: '0 0 24px',
};

const note: React.CSSProperties = {
  color: '#64748b',
  fontSize: '13px',
  lineHeight: '1.5',
  margin: '0 0 24px',
};

const btnSection: React.CSSProperties = { margin: '0 0 24px' };

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
  lineHeight: '1.5',
  margin: '0',
};
