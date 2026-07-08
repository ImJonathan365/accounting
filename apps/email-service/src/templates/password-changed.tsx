import {
  Body,
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
import { PasswordChangedPayload } from '../types.js';

export function PasswordChangedEmail({ firstName }: PasswordChangedPayload) {
  return (
    <Html lang="es">
      <Head />
      <Preview>Tu contraseña fue cambiada exitosamente</Preview>
      <Body style={body}>
        <Container style={container}>
          <Section style={iconSection}>
            <div style={iconCircle}>✓</div>
          </Section>
          <Heading style={h1}>Contraseña actualizada</Heading>
          <Text style={text}>
            Hola {firstName}, te confirmamos que la contraseña de tu cuenta fue cambiada exitosamente.
          </Text>
          <Text style={text}>
            Si fuiste tú, no necesitas hacer nada más.
          </Text>
          <Text style={alert}>
            Si <strong>no realizaste este cambio</strong>, tu cuenta podría estar comprometida.
            Contacta a soporte de inmediato.
          </Text>
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
  backgroundColor: '#dcfce7',
  color: '#16a34a',
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

const alert: React.CSSProperties = {
  backgroundColor: '#fef2f2',
  border: '1px solid #fecaca',
  borderRadius: '6px',
  color: '#dc2626',
  fontSize: '14px',
  lineHeight: '1.5',
  margin: '0 0 24px',
  padding: '12px 16px',
};

const hr: React.CSSProperties = { borderColor: '#e2e8f0', margin: '0 0 16px' };

const footer: React.CSSProperties = {
  color: '#94a3b8',
  fontSize: '12px',
  lineHeight: '1.5',
  margin: '0',
};
