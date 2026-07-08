from __future__ import annotations

import logging
from datetime import UTC, datetime
from typing import Any

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.database import SessionLocal
from app.domain.commands import CommandEnvelope
from app.domain.events import EventType, build_event_envelope
from app.kafka.client import KafkaConsumerClient, KafkaProducerClient, KafkaUnavailable
from app.kafka.topics import COMMANDS_DLQ_TOPIC, COMMANDS_TOPIC, EVENTS_TOPIC
from app.models import AuditLog, CommandRecord, EventRecord, OutboxMessage
from app.workers.command_handlers import handle_command

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO)


def process_command_payload(db: Session, payload: dict[str, Any]) -> EventRecord:
    envelope = CommandEnvelope.model_validate(payload)
    existing_event = db.scalar(
        select(EventRecord).where(
            EventRecord.command_id == envelope.command_id,
            EventRecord.event_type.in_(
                [EventType.COMMAND_COMPLETED.value, EventType.COMMAND_FAILED.value]
            ),
        )
    )
    if existing_event:
        return existing_event

    command = db.get(CommandRecord, envelope.command_id)
    if command is None:
        command = CommandRecord(
            id=envelope.command_id,
            command_type=envelope.command_type.value,
            correlation_id=envelope.correlation_id,
            idempotency_key=envelope.idempotency_key,
            actor=envelope.actor,
            payload=envelope.payload,
            status="processing",
            created_at=envelope.created_at,
            updated_at=datetime.now(UTC),
        )
        db.add(command)
    else:
        command.status = "processing"
        command.updated_at = datetime.now(UTC)

    try:
        result = handle_command(envelope)
        command.status = "completed"
        command.result = result
        command.error = None
        event_type = EventType.COMMAND_COMPLETED
    except Exception as exc:
        result = {"error": str(exc), "command_type": envelope.command_type.value}
        command.status = "failed"
        command.result = None
        command.error = str(exc)
        event_type = EventType.COMMAND_FAILED

    command.updated_at = datetime.now(UTC)
    event_envelope = build_event_envelope(
        event_type=event_type,
        command_id=command.id,
        correlation_id=command.correlation_id,
        result=result,
    )
    event = EventRecord(
        id=event_envelope.event_id,
        event_type=event_envelope.event_type.value,
        command_id=event_envelope.command_id,
        correlation_id=event_envelope.correlation_id,
        result=event_envelope.result,
        occurred_at=event_envelope.occurred_at,
    )
    outbox = OutboxMessage(
        topic=EVENTS_TOPIC,
        message_key=event.id,
        payload=event_envelope.model_dump(mode="json"),
    )
    audit = AuditLog(
        actor="command-worker",
        action="command.processed",
        entity_type="command",
        entity_id=command.id,
        metadata_json={"event_id": event.id, "event_type": event.event_type},
    )
    db.add_all([event, outbox, audit])
    db.commit()
    db.refresh(event)
    return event


def run() -> None:
    try:
        consumer = KafkaConsumerClient(group_id="labai-command-worker", topics=[COMMANDS_TOPIC])
        producer = KafkaProducerClient()
    except KafkaUnavailable as exc:
        logger.error("Kafka unavailable for command worker: %s", exc)
        raise

    try:
        for payload in consumer.messages():
            with SessionLocal() as db:
                try:
                    event = process_command_payload(db, payload)
                    logger.info("Processed command into event %s", event.id)
                except Exception as exc:
                    logger.exception("Invalid command payload sent to DLQ")
                    producer.publish(
                        topic=COMMANDS_DLQ_TOPIC,
                        key=str(payload.get("command_id", "unknown")),
                        payload={"payload": payload, "error": str(exc)},
                    )
    finally:
        consumer.close()


if __name__ == "__main__":
    run()

