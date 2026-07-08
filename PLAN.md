# LabAI Event Dashboard & Notifications App

## Summary
- Create a new Python app folder: `dashboard_notifications_app/` inside the LabAI repo; leave existing `reference_agent.py` files untouched.
- Build one FastAPI app serving both UI and API using HTMX/Jinja, backed by Render Postgres, managed Kafka via Confluent Cloud, and Python background workers.
- Use Command + Mediator + Observer as real architecture boundaries: UI/API emits typed commands, the mediator records/publishes them, workers handle commands, and Kafka event fan-out drives Email, SMS, and Analytics observers.
- Use Agentic AI with the OpenAI Agents SDK and an app-owned MCP server at `/mcp`; the agent can inspect dashboard state and propose or submit commands through the same mediator path.

## Runtime Flow
- User opens `https://earthymix.com`; FastAPI renders dashboard panels for command status, notifications, event timeline, observer health, and AI assistant.
- UI submits `GenerateInvoiceCommand` or `SendNotificationCommand` to `/api/commands`; API validates with Pydantic, stores command + outbox row in Postgres, and returns `202 Accepted` with `command_id`.
- `outbox-dispatcher` publishes command envelopes to `labai.commands.v1`; `command-worker` consumes, runs the command handler, writes `CommandCompletedEvent` or `CommandFailedEvent`, and publishes to `labai.events.v1`.
- Email, SMS, and Analytics observers consume the same event topic using separate Kafka consumer groups, so each observer receives every event independently; scaled replicas of the same observer share that observer’s group.
- Observers write delivery status to Postgres; dashboard refreshes via HTMX polling/SSE-lite endpoints and never talks directly to workers or observers.
- AI assistant calls MCP tools like `get_dashboard_snapshot`, `list_recent_events`, `get_command_status`, and `trigger_command`; mutating MCP tools require approval/allow-listing before they create commands.

## Public Interfaces
- API endpoints: `GET /`, `GET /healthz`, `GET /readyz`, `POST /api/commands`, `GET /api/commands/{id}`, `GET /api/events`, `GET /api/notifications`, `POST /api/agent/query`, `GET|POST /mcp`.
- Command envelope: `schema_version`, `command_id`, `command_type`, `correlation_id`, `idempotency_key`, `actor`, `payload`, `created_at`.
- Event envelope: `schema_version`, `event_id`, `event_type`, `command_id`, `correlation_id`, `result`, `occurred_at`.
- Kafka topics: `labai.commands.v1`, `labai.events.v1`, `labai.commands.dlq.v1`, `labai.events.dlq.v1`.
- Core tables: `users`, `commands`, `events`, `outbox_messages`, `notifications`, `observer_deliveries`, `agent_runs`, `audit_log`.
- Required env vars: `DATABASE_URL`, `KAFKA_BOOTSTRAP_SERVERS`, `KAFKA_SECURITY_PROTOCOL=SASL_SSL`, `KAFKA_SASL_MECHANISM=PLAIN`, `KAFKA_SASL_USERNAME`, `KAFKA_SASL_PASSWORD`, `OPENAI_API_KEY`, `OPENAI_MODEL`, `MCP_SERVER_TOKEN`, `SESSION_SECRET`, SMTP vars, and Twilio SMS vars.

## Implementation Changes
- Add `dashboard_notifications_app/` with `app/main.py`, `app/api`, `app/domain`, `app/mediator`, `app/kafka`, `app/workers`, `app/observers`, `app/agents`, `app/mcp_server`, `app/templates`, `app/static`, and Alembic migrations.
- Use local VENV workflow: `python3.12 -m venv .venv`, `source .venv/bin/activate`, `pip install -r requirements-dev.txt`; keep `.venv/` ignored.
- Use `confluent-kafka`, `fastapi`, `uvicorn`, `sqlalchemy`, `alembic`, `pydantic`, `jinja2`, `python-multipart`, `openai-agents`, and `mcp>=1.27,<2`.
- Add `docker-compose.yml` for local Kafka and Postgres only; production Kafka is Confluent Cloud, not self-hosted on Render.
- Add `render.yaml` using `rootDir: dashboard_notifications_app`, one web service, one Postgres database, and workers: `outbox-dispatcher`, `command-worker`, `email-observer`, `sms-observer`, `analytics-observer`.
- Web start command: `uvicorn app.main:app --host 0.0.0.0 --port $PORT`; pre-deploy command: `alembic upgrade head`.
- Render custom domains: attach `earthymix.com` and `www.earthymix.com` to the web service, keep HTTPS enabled, and disable the default `onrender.com` subdomain after DNS verification.

## Reliability And Security
- Use transactional outbox to avoid losing commands between Postgres and Kafka.
- Use at-least-once Kafka processing with idempotency keys and unique observer delivery records.
- Retry transient observer failures with backoff; after max attempts, publish to DLQ and show failed delivery on the dashboard.
- Protect UI with session auth, CSRF on form posts, secure cookies, and admin bootstrap from env.
- Protect `/mcp` with bearer token; expose read-only tools by default and require explicit approval for command-triggering tools.

## Test Plan
- Unit tests for command/event schemas, mediator routing, idempotency behavior, and observer registry.
- Integration tests using local Postgres + Kafka for command publish, worker consume, event publish, and independent observer fan-out.
- Failure tests for duplicate commands, observer crash/retry, DLQ routing, Kafka unavailable, Postgres unavailable, and agent attempting unauthorized MCP mutation.
- API tests for `202` command creation, status lookup, notification listing, health/readiness behavior, and auth/CSRF failures.
- UI smoke tests with Playwright for dashboard load, command submission, status refresh, and AI assistant response.
- Deployment smoke test on Render: `/healthz`, `/readyz`, domain TLS, worker logs, Kafka consumer lag, and one end-to-end notification command.

## Assumptions And Sources
- Defaults chosen from your answers: managed Kafka, cloud LLM, FastAPI + HTMX.
- Render supports Python FastAPI deployment with `pip install -r requirements.txt` and `uvicorn ... --port $PORT`; web services support custom domains and must bind to `0.0.0.0` [Render FastAPI](https://render.com/docs/deploy-fastapi), [Render web services](https://render.com/docs/web-services).
- Render Blueprints support web/worker services, `rootDir`, custom domains, env vars, databases, and worker definitions [Render Blueprint YAML](https://render.com/docs/blueprint-spec).
- Render custom domains get automatic TLS and require DNS configuration/verification [Render custom domains](https://render.com/docs/custom-domains).
- MCP is the standard tool/context bridge for AI apps; the stable Python SDK line is v1.x, so pin `<2` [MCP intro](https://modelcontextprotocol.io/docs/getting-started/intro), [MCP Python SDK v1](https://raw.githubusercontent.com/modelcontextprotocol/python-sdk/v1.x/README.md).
- OpenAI Agents SDK supports agents, tools, tracing, MCP servers, and approval policies for MCP tools [Agents SDK](https://openai.github.io/openai-agents-python/), [Agents MCP](https://openai.github.io/openai-agents-python/mcp/).
- Confluent’s Python Kafka client supports Confluent Cloud with `SASL_SSL`, API key/secret credentials, and async producer/consumer patterns suitable for FastAPI/workers [Confluent Python client](https://docs.confluent.io/kafka-clients/python/current/overview.html).
