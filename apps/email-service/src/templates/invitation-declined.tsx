import { Body, Container, Head, Heading, Hr, Html, Preview, Section, Text } from '@react-email/components';
import * as React from 'react';
import { InvitationDeclinedPayload } from '../types.js';

export function InvitationDeclinedEmail({ firstName, orgName }: InvitationDeclinedPayload) {
  return (
    <Html lang="es">
      <Head />
      <Preview>Rechazaste la invitación a {orgName}</Preview>
      <Body style={body}>
        <Container style={container}>
          <Section style={iconSection}><div style={iconCircle}>👋</div></Section>
          <Heading style={h1}>Invitación rechazada</Heading>
          <Text style={text}>
            Hola {firstName}, confirmamos que rechazaste la invitación para unirte a{' '}
            <strong>{orgName}</strong>.
          </Text>
          <Text style={text}>
            Si esto fue un error, pídele al administrador de {orgName} que te envíe una nueva invitación.
          </Text>
          <Hr style={hr} />
          <Text style={footer}>Este mensaje fue generado automáticamente.</Text>
        </Container>
      </Body>
    </Html>
  );
}

const body: React.CSSProperties      = { backgroundColor: '#f1f5f9', fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif', padding: '40px 0' };
const container: React.CSSProperties = { backgroundColor: '#ffffff', borderRadius: '8px', maxWidth: '560px', margin: '0 auto', padding: '40px' };
const iconSection: React.CSSProperties = { textAlign: 'center', margin: '0 0 24px' };
const iconCircle: React.CSSProperties  = { display: 'inline-block', fontSize: '40px' };
const h1: React.CSSProperties    = { color: '#0f172a', fontSize: '24px', fontWeight: '700', margin: '0 0 16px', textAlign: 'center' };
const text: React.CSSProperties  = { color: '#475569', fontSize: '15px', lineHeight: '1.6', margin: '0 0 16px' };
const hr: React.CSSProperties    = { borderColor: '#e2e8f0', margin: '0 0 16px' };
const footer: React.CSSProperties = { color: '#94a3b8', fontSize: '12px', margin: '0' };
