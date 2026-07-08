"""Initial LabAI dashboard schema.

Revision ID: 0001_initial_schema
Revises:
Create Date: 2026-07-06
"""

from alembic import op
import sqlalchemy as sa

revision = "0001_initial_schema"
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.create_table(
        "users",
        sa.Column("id", sa.String(length=64), primary_key=True),
        sa.Column("username", sa.String(length=120), nullable=False),
        sa.Column("role", sa.String(length=64), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.UniqueConstraint("username"),
    )
    op.create_index("ix_users_username", "users", ["username"])

    op.create_table(
        "commands",
        sa.Column("id", sa.String(length=80), primary_key=True),
        sa.Column("command_type", sa.String(length=120), nullable=False),
        sa.Column("correlation_id", sa.String(length=80), nullable=False),
        sa.Column("idempotency_key", sa.String(length=160), nullable=True),
        sa.Column("actor", sa.String(length=160), nullable=False),
        sa.Column("payload", sa.JSON(), nullable=False),
        sa.Column("status", sa.String(length=32), nullable=False),
        sa.Column("result", sa.JSON(), nullable=True),
        sa.Column("error", sa.Text(), nullable=True),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("updated_at", sa.DateTime(timezone=True), nullable=False),
        sa.UniqueConstraint("idempotency_key"),
    )
    op.create_index("ix_commands_command_type", "commands", ["command_type"])
    op.create_index("ix_commands_correlation_id", "commands", ["correlation_id"])
    op.create_index("ix_commands_status", "commands", ["status"])

    op.create_table(
        "events",
        sa.Column("id", sa.String(length=80), primary_key=True),
        sa.Column("event_type", sa.String(length=120), nullable=False),
        sa.Column("command_id", sa.String(length=80), sa.ForeignKey("commands.id"), nullable=True),
        sa.Column("correlation_id", sa.String(length=80), nullable=False),
        sa.Column("result", sa.JSON(), nullable=False),
        sa.Column("occurred_at", sa.DateTime(timezone=True), nullable=False),
    )
    op.create_index("ix_events_event_type", "events", ["event_type"])
    op.create_index("ix_events_correlation_id", "events", ["correlation_id"])

    op.create_table(
        "outbox_messages",
        sa.Column("id", sa.String(length=80), primary_key=True),
        sa.Column("topic", sa.String(length=160), nullable=False),
        sa.Column("message_key", sa.String(length=160), nullable=False),
        sa.Column("payload", sa.JSON(), nullable=False),
        sa.Column("status", sa.String(length=32), nullable=False),
        sa.Column("attempts", sa.Integer(), nullable=False),
        sa.Column("error", sa.Text(), nullable=True),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("published_at", sa.DateTime(timezone=True), nullable=True),
    )
    op.create_index("ix_outbox_messages_topic", "outbox_messages", ["topic"])
    op.create_index("ix_outbox_messages_message_key", "outbox_messages", ["message_key"])
    op.create_index("ix_outbox_messages_status", "outbox_messages", ["status"])

    op.create_table(
        "notifications",
        sa.Column("id", sa.String(length=80), primary_key=True),
        sa.Column("command_id", sa.String(length=80), sa.ForeignKey("commands.id"), nullable=True),
        sa.Column("event_id", sa.String(length=80), sa.ForeignKey("events.id"), nullable=True),
        sa.Column("channel", sa.String(length=32), nullable=False),
        sa.Column("recipient", sa.String(length=240), nullable=False),
        sa.Column("subject", sa.String(length=240), nullable=True),
        sa.Column("message", sa.Text(), nullable=False),
        sa.Column("status", sa.String(length=32), nullable=False),
        sa.Column("provider_message_id", sa.String(length=240), nullable=True),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("updated_at", sa.DateTime(timezone=True), nullable=False),
    )
    op.create_index("ix_notifications_status", "notifications", ["status"])

    op.create_table(
        "observer_deliveries",
        sa.Column("id", sa.String(length=80), primary_key=True),
        sa.Column("observer_name", sa.String(length=80), nullable=False),
        sa.Column("event_id", sa.String(length=80), nullable=False),
        sa.Column("status", sa.String(length=32), nullable=False),
        sa.Column("attempts", sa.Integer(), nullable=False),
        sa.Column("error", sa.Text(), nullable=True),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.Column("updated_at", sa.DateTime(timezone=True), nullable=False),
        sa.UniqueConstraint("observer_name", "event_id", name="uq_observer_event"),
    )
    op.create_index("ix_observer_deliveries_observer_name", "observer_deliveries", ["observer_name"])
    op.create_index("ix_observer_deliveries_event_id", "observer_deliveries", ["event_id"])
    op.create_index("ix_observer_deliveries_status", "observer_deliveries", ["status"])

    op.create_table(
        "agent_runs",
        sa.Column("id", sa.String(length=80), primary_key=True),
        sa.Column("query", sa.Text(), nullable=False),
        sa.Column("response", sa.Text(), nullable=False),
        sa.Column("status", sa.String(length=32), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
    )
    op.create_index("ix_agent_runs_status", "agent_runs", ["status"])

    op.create_table(
        "audit_log",
        sa.Column("id", sa.String(length=80), primary_key=True),
        sa.Column("actor", sa.String(length=160), nullable=False),
        sa.Column("action", sa.String(length=160), nullable=False),
        sa.Column("entity_type", sa.String(length=120), nullable=False),
        sa.Column("entity_id", sa.String(length=120), nullable=False),
        sa.Column("metadata_json", sa.JSON(), nullable=True),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
    )
    op.create_index("ix_audit_log_action", "audit_log", ["action"])


def downgrade() -> None:
    op.drop_index("ix_audit_log_action", table_name="audit_log")
    op.drop_table("audit_log")
    op.drop_index("ix_agent_runs_status", table_name="agent_runs")
    op.drop_table("agent_runs")
    op.drop_index("ix_observer_deliveries_status", table_name="observer_deliveries")
    op.drop_index("ix_observer_deliveries_event_id", table_name="observer_deliveries")
    op.drop_index("ix_observer_deliveries_observer_name", table_name="observer_deliveries")
    op.drop_table("observer_deliveries")
    op.drop_index("ix_notifications_status", table_name="notifications")
    op.drop_table("notifications")
    op.drop_index("ix_outbox_messages_status", table_name="outbox_messages")
    op.drop_index("ix_outbox_messages_message_key", table_name="outbox_messages")
    op.drop_index("ix_outbox_messages_topic", table_name="outbox_messages")
    op.drop_table("outbox_messages")
    op.drop_index("ix_events_correlation_id", table_name="events")
    op.drop_index("ix_events_event_type", table_name="events")
    op.drop_table("events")
    op.drop_index("ix_commands_status", table_name="commands")
    op.drop_index("ix_commands_correlation_id", table_name="commands")
    op.drop_index("ix_commands_command_type", table_name="commands")
    op.drop_table("commands")
    op.drop_index("ix_users_username", table_name="users")
    op.drop_table("users")

