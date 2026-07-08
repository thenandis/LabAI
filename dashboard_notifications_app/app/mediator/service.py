from __future__ import annotations

from datetime import UTC, datetime
from typing import Any

from sqlalchemy import Select, desc, func, select
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from app.domain.commands import CommandEnvelope, CreateCommandRequest, build_command_envelope
from app.kafka.topics import COMMANDS_TOPIC
from app.models import AuditLog, CommandRecord, EventRecord, Notification, ObserverDelivery, OutboxMessage


class CommandMediator:
    """Central command ingress.

    UI, API, MCP tools, and agents submit commands here instead of talking
    directly to workers or observers.
    """

    def submit_command(self, db: Session, request: CreateCommandRequest) -> CommandRecord:
        if request.idempotency_key:
            existing = db.scalar(
                select(CommandRecord).where(CommandRecord.idempotency_key == request.idempotency_key)
            )
            if existing:
                return existing

        envelope = build_command_envelope(request)
        record = CommandRecord(
            id=envelope.command_id,
            command_type=envelope.command_type.value,
            correlation_id=envelope.correlation_id,
            idempotency_key=envelope.idempotency_key,
            actor=envelope.actor,
            payload=envelope.payload,
            status="pending",
            created_at=envelope.created_at,
            updated_at=envelope.created_at,
        )
        outbox = OutboxMessage(
            topic=COMMANDS_TOPIC,
            message_key=envelope.command_id,
            payload=envelope.model_dump(mode="json"),
        )
        audit = AuditLog(
            actor=envelope.actor,
            action="command.submitted",
            entity_type="command",
            entity_id=envelope.command_id,
            metadata_json={
                "command_type": envelope.command_type.value,
                "correlation_id": envelope.correlation_id,
            },
        )
        db.add_all([record, outbox, audit])
        try:
            db.commit()
        except IntegrityError:
            db.rollback()
            if request.idempotency_key:
                existing = db.scalar(
                    select(CommandRecord).where(
                        CommandRecord.idempotency_key == request.idempotency_key
                    )
                )
                if existing:
                    return existing
            raise
        db.refresh(record)
        return record

    def get_command(self, db: Session, command_id: str) -> CommandRecord | None:
        return db.get(CommandRecord, command_id)


def _rows(statement: Select[tuple[Any, ...]], db: Session) -> list[dict[str, Any]]:
    return [dict(row._mapping) for row in db.execute(statement).all()]


def dashboard_snapshot(db: Session) -> dict[str, Any]:
    command_counts = _rows(
        select(CommandRecord.status, func.count(CommandRecord.id).label("count"))
        .group_by(CommandRecord.status)
        .order_by(CommandRecord.status),
        db,
    )
    observer_counts = _rows(
        select(
            ObserverDelivery.observer_name,
            ObserverDelivery.status,
            func.count(ObserverDelivery.id).label("count"),
        )
        .group_by(ObserverDelivery.observer_name, ObserverDelivery.status)
        .order_by(ObserverDelivery.observer_name),
        db,
    )
    pending_outbox = db.scalar(
        select(func.count(OutboxMessage.id)).where(OutboxMessage.status == "pending")
    )
    recent_commands = db.scalars(
        select(CommandRecord).order_by(desc(CommandRecord.created_at)).limit(10)
    ).all()
    recent_events = db.scalars(select(EventRecord).order_by(desc(EventRecord.occurred_at)).limit(10)).all()
    recent_notifications = db.scalars(
        select(Notification).order_by(desc(Notification.created_at)).limit(10)
    ).all()
    return {
        "generated_at": datetime.now(UTC).isoformat(),
        "command_counts": command_counts,
        "observer_counts": observer_counts,
        "pending_outbox": pending_outbox or 0,
        "recent_commands": [
            {
                "id": command.id,
                "command_type": command.command_type,
                "status": command.status,
                "actor": command.actor,
                "created_at": command.created_at.isoformat(),
            }
            for command in recent_commands
        ],
        "recent_events": [
            {
                "id": event.id,
                "event_type": event.event_type,
                "command_id": event.command_id,
                "occurred_at": event.occurred_at.isoformat(),
            }
            for event in recent_events
        ],
        "recent_notifications": [
            {
                "id": notification.id,
                "channel": notification.channel,
                "recipient": notification.recipient,
                "status": notification.status,
                "created_at": notification.created_at.isoformat(),
            }
            for notification in recent_notifications
        ],
    }


def command_to_envelope(record: CommandRecord) -> CommandEnvelope:
    return CommandEnvelope(
        command_id=record.id,
        command_type=record.command_type,
        correlation_id=record.correlation_id,
        idempotency_key=record.idempotency_key,
        actor=record.actor,
        payload=record.payload,
        created_at=record.created_at,
    )

