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
import { config } from '../config.js';
import { WelcomePayload } from '../types.js';

export function WelcomeEmail({ firstName, orgName }: WelcomePayload) {
  return (
    <Html lang="es">
      <Head />
      <Preview>Bienvenido a Accounting, {firstName}</Preview>
      <Body style={body}>
        <Container style={container}>
          <Heading style={h1}>Bienvenido, {firstName}</Heading>
          <Text style={text}>
            Tu cuenta y la organización <strong>{orgName}</strong> han sido creadas correctamente.
            Ya puedes empezar a registrar tus movimientos contables.
          </Text>
          <Section style={btnSection}>
            <Button style={button} href={`${config.appUrl}/dashboard`}>
              Ir al dashboard
            </Button>
          </Section>
          <Hr style={hr} />
          <Text style={footer}>
            Si no creaste esta cuenta, puedes ignorar este mensaje.
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

const btnSection: React.CSSProperties = { margin: '0 0 32px' };

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

const hr: React.CSSProperties = { borderColor: '#e2e8f0', margin: '0 0 24px' };

const footer: React.CSSProperties = {
  color: '#94a3b8',
  fontSize: '12px',
  margin: '0',
};
