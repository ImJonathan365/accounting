import 'dotenv/config';
import Fastify from 'fastify';
import helmet from '@fastify/helmet';
import rateLimit from '@fastify/rate-limit';
import { config } from './config.js';
import { sendEmail } from './sender.js';
import { EMAIL_TYPES, EmailRequest } from './types.js';

const app = Fastify({ logger: true });

app.register(helmet);

// 60 requests per minute
app.register(rateLimit, {
  max: 60,
  timeWindow: '1 minute',
  errorResponseBuilder: () => ({ error: 'Too Many Requests' }),
});

// All routes require Bearer secret
app.addHook('onRequest', async (req, reply) => {
  const auth = req.headers.authorization;
  if (!auth || auth !== `Bearer ${config.secret}`) {
    reply.code(401).send({ error: 'Unauthorized' });
  }
});

const sendSchema = {
  body: {
    type: 'object',
    required: ['type', 'to', 'payload'],
    properties: {
      type:    { type: 'string', enum: EMAIL_TYPES },
      to:      { type: 'string', format: 'email' },
      payload: { type: 'object' },
    },
    additionalProperties: false,
  },
} as const;

app.post<{ Body: EmailRequest }>('/send', { schema: sendSchema }, async (req, reply) => {
  await sendEmail(req.body);
  return reply.code(200).send({ ok: true });
});

app.get('/health', async () => ({ status: 'ok' }));

app.listen({ port: config.port, host: '0.0.0.0' }, (err) => {
  if (err) {
    app.log.error(err);
    process.exit(1);
  }
});
