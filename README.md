# Accounting SaaS

Multi-tenant accounting platform for SMBs. Double-entry bookkeeping, invoicing, bank reconciliation, budgets, and financial reports.

**Stack:** .NET 10 API · Next.js 14 · PostgreSQL · Docker Compose

---

## Prerequisites

| Tool | Version | Check |
|------|---------|-------|
| Docker + Compose | v24+ | `docker --version` |
| .NET SDK | 10.x | `dotnet --version` |
| pnpm | 11.x | `pnpm --version` |
| Node.js | 20.x | `node --version` |

---

## Quick start

### 1. Clone and configure environment

```bash
git clone https://github.com/ImJonathan365/accounting.git
cd accounting

cp .env.example .env
```

Open `.env` and fill in the required secrets:

```bash
# Generate a secure random value for each:
JWT_SECRET=          # min 32 chars
EMAIL_SERVICE_SECRET= # min 32 chars

# Email delivery (production)
RESEND_API_KEY=      # from resend.com — leave empty to use Mailpit in dev
```

Everything else in `.env` can stay as-is for local development.

### 2. Start all services

```bash
docker compose up -d
```

This starts: PostgreSQL, the .NET API, Next.js web, the email microservice, Mailpit (dev SMTP), and Adminer.

Wait ~15 seconds for the API to initialize, then verify:

```bash
docker compose ps        # all services should be "running" or "healthy"
curl http://localhost:8081/health  # should return 200
```

### 3. Run database migrations

```bash
cd apps/api
dotnet ef database update -p src/Accounting.Infrastructure -s src/Accounting.Api
```

> First time: install the EF CLI tool with `dotnet tool install --global dotnet-ef`

### 4. Open the app

| Service | URL |
|---------|-----|
| Web app | http://localhost:3000 |
| API | http://localhost:8081 |
| Mailpit (dev email) | http://localhost:8025 |
| Adminer (DB UI) | http://localhost:8080 |

Register an account at http://localhost:3000 — the first user automatically becomes the organization owner.

---

## Development (without Docker)

To run services locally for active development (hot reload):

**API:**
```bash
cd apps/api/src/Accounting.Api
dotnet watch run
```
Requires a running PostgreSQL instance. Set the connection string in `apps/api/src/Accounting.Api/appsettings.Development.json`.

**Web:**
```bash
pnpm install
pnpm dev
```
Requires `NEXT_PUBLIC_API_URL` pointing to the running API (default: `http://localhost:8081`).

---

## Running tests

```bash
cd apps/api
dotnet test Accounting.slnx --configuration Release --nologo --logger "console;verbosity=minimal"
```

175 tests, all passing.

---

## Adding a database migration

```bash
cd apps/api
dotnet ef migrations add <MigrationName> -p src/Accounting.Infrastructure -s src/Accounting.Api
dotnet ef database update -p src/Accounting.Infrastructure -s src/Accounting.Api
```

---

## Project structure

```
apps/
  api/            → .NET 10 Clean Architecture (Domain / Application / Infrastructure / Api)
  web/            → Next.js 14 App Router
  email-service/  → React Email + Express (transactional emails)
packages/
  types/          → Shared TypeScript types
docker-compose.yml
```
