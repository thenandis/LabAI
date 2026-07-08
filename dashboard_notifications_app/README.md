# LabAI Event Dashboard and Notifications App

Python/FastAPI implementation of a dashboard and notification system using Command, Mediator, and Observer patterns with Kafka fan-out and an Agentic AI/MCP control surface.

## Architecture Flow

1. The dashboard or API submits a typed command to `POST /api/commands`.
2. `CommandMediator` validates the payload, writes a `commands` row, and writes a transactional `outbox_messages` row.
3. `python -m app.workers.outbox_dispatcher` publishes pending outbox messages to Kafka.
4. `python -m app.workers.command_worker` consumes `labai.commands.v1`, executes command handlers, writes `events`, and writes a new outbox row for `labai.events.v1`.
5. Email, SMS, and Analytics observers consume `labai.events.v1` with separate consumer groups. Every observer receives every event without the command sender knowing who is subscribed.
6. The dashboard refreshes status, event timeline, notifications, and observer health through HTMX partial endpoints.
7. The AI operator uses OpenAI Agents SDK and MCP tools to inspect state. Mutating MCP tool calls are disabled unless explicitly enabled by environment.

## Local VENV Setup

```bash
cd dashboard_notifications_app
python3.12 -m venv .venv
source .venv/bin/activate
pip install -r requirements-dev.txt
```

The app automatically loads `.env` from the repo root and then `dashboard_notifications_app/.env` if present. Keep real keys in `.env`; do not commit them.

Create a local `.env` or export values:

```bash
export DATABASE_URL=postgresql://labai:labai@localhost:5432/labai_dashboard
export KAFKA_BOOTSTRAP_SERVERS=localhost:9092
export KAFKA_SECURITY_PROTOCOL=PLAINTEXT
export SESSION_SECRET=local-dev-secret
export ADMIN_USERNAME=admin
export ADMIN_PASSWORD=admin
export MCP_SERVER_TOKEN=local-mcp-token
export OPENAI_API_KEY=...
export PHI_API_KEY=...
```

Start local infrastructure:

```bash
docker compose up -d postgres kafka
alembic upgrade head
uvicorn app.main:app --reload
```

Start workers in separate terminals:

```bash
python -m app.workers.outbox_dispatcher
python -m app.workers.command_worker
python -m app.observers.email
python -m app.observers.sms
python -m app.observers.analytics
```

Open `http://localhost:8000` and sign in with `ADMIN_USERNAME` / `ADMIN_PASSWORD`.

## Production Environment

Render uses `render.yaml` from the repo root. The web service uses:

```bash
uvicorn app.main:app --host 0.0.0.0 --port $PORT
```

Render runs migrations with:

```bash
alembic upgrade head
```

Production Kafka is managed Kafka, for example Confluent Cloud. Configure:

```bash
KAFKA_BOOTSTRAP_SERVERS=...
KAFKA_SECURITY_PROTOCOL=SASL_SSL
KAFKA_SASL_MECHANISM=PLAIN
KAFKA_SASL_USERNAME=...
KAFKA_SASL_PASSWORD=...
```

Attach `earthymix.com` and `www.earthymix.com` to the web service in Render. Render verifies DNS, provisions TLS, and `renderSubdomainPolicy: disabled` disables the default `onrender.com` route after verification.

## API Surface

- `GET /`
- `GET /healthz`
- `GET /readyz`
- `POST /api/commands`
- `GET /api/commands/{id}`
- `GET /api/events`
- `GET /api/notifications`
- `POST /api/agent/query`
- `GET /mcp`
- `POST /mcp`

## Command Payloads

`GenerateInvoiceCommand`

```json
{
  "command_type": "GenerateInvoiceCommand",
  "idempotency_key": "invoice-123",
  "payload": {
    "customer_name": "Avery Carter",
    "customer_email": "avery@example.com",
    "amount_cents": 24900,
    "currency": "USD",
    "due_date": "2026-08-01",
    "memo": "Dashboard implementation milestone"
  }
}
```

`SendNotificationCommand`

```json
{
  "command_type": "SendNotificationCommand",
  "idempotency_key": "notify-123",
  "payload": {
    "channel": "email",
    "recipient": "ops@example.com",
    "subject": "LabAI notification",
    "message": "Observer fan-out check."
  }
}
```

## MCP Tools

The `/mcp` endpoint requires:

```text
Authorization: Bearer $MCP_SERVER_TOKEN
```

Tools:

- `get_dashboard_snapshot`
- `list_recent_events`
- `get_command_status`
- `trigger_command`

`trigger_command` is disabled by default. Enable it only for controlled environments:

```bash
export MCP_ALLOW_MUTATIONS=true
export MCP_MUTATION_APPROVAL_TOKEN=reviewed-change-token
```

## Tests

```bash
pytest
```

The tests use SQLite and in-process service functions for fast feedback. Full Kafka integration can be run with Docker Compose and the worker commands above.
