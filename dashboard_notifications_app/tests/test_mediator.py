from __future__ import annotations

from sqlalchemy import select

from app.domain.commands import CommandType, CreateCommandRequest
from app.kafka.topics import COMMANDS_TOPIC
from app.mediator import CommandMediator
from app.models import CommandRecord, OutboxMessage


def test_mediator_persists_command_and_outbox(db_session) -> None:
    request = CreateCommandRequest(
        command_type=CommandType.SEND_NOTIFICATION,
        idempotency_key="notify-1",
        payload={
            "channel": "email",
            "recipient": "ops@example.com",
            "subject": "Hello",
            "message": "World",
        },
        actor="tester",
    )

    record = CommandMediator().submit_command(db_session, request)

    saved = db_session.get(CommandRecord, record.id)
    outbox = db_session.scalar(select(OutboxMessage).where(OutboxMessage.message_key == record.id))
    assert saved is not None
    assert saved.status == "pending"
    assert outbox is not None
    assert outbox.topic == COMMANDS_TOPIC


def test_mediator_idempotency_returns_existing_command(db_session) -> None:
    mediator = CommandMediator()
    request = CreateCommandRequest(
        command_type=CommandType.SEND_NOTIFICATION,
        idempotency_key="notify-1",
        payload={
            "channel": "email",
            "recipient": "ops@example.com",
            "message": "World",
        },
    )

    first = mediator.submit_command(db_session, request)
    second = mediator.submit_command(db_session, request)

    assert first.id == second.id
    assert db_session.query(CommandRecord).count() == 1

