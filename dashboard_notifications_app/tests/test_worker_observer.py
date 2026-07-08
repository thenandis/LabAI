from __future__ import annotations

from app.domain.commands import CommandType, CreateCommandRequest, build_command_envelope
from app.domain.events import EventType
from app.models import EventRecord, Notification, ObserverDelivery
from app.observers.base import handle_event_for_observer
from app.observers.email import deliver_email
from app.workers.command_worker import process_command_payload


def test_command_worker_creates_completed_event(db_session) -> None:
    envelope = build_command_envelope(
        CreateCommandRequest(
            command_type=CommandType.GENERATE_INVOICE,
            payload={
                "customer_name": "Avery Carter",
                "customer_email": "avery@example.com",
                "amount_cents": 2500,
                "currency": "USD",
            },
        )
    )

    event = process_command_payload(db_session, envelope.model_dump(mode="json"))

    assert event.event_type == EventType.COMMAND_COMPLETED.value
    assert db_session.query(EventRecord).count() == 1


def test_email_observer_records_delivery_and_notification(db_session) -> None:
    event_payload = {
        "event_id": "evt_1",
        "event_type": "CommandCompletedEvent",
        "command_id": "cmd_1",
        "correlation_id": "cor_1",
        "result": {
            "notification": {
                "channel": "email",
                "recipient": "ops@example.com",
                "subject": "Subject",
                "message": "Message",
            }
        },
    }

    delivery = handle_event_for_observer(
        db_session,
        observer_name="email",
        event_payload=event_payload,
        deliver=deliver_email,
        dlq_producer=None,
    )

    assert delivery.status == "delivered"
    assert db_session.query(ObserverDelivery).count() == 1
    assert db_session.query(Notification).count() == 1

