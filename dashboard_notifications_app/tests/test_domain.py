from __future__ import annotations

import pytest
from pydantic import ValidationError

from app.domain.commands import CommandType, CreateCommandRequest, build_command_envelope
from app.domain.events import EventType, build_event_envelope


def test_build_generate_invoice_command_envelope() -> None:
    request = CreateCommandRequest(
        command_type=CommandType.GENERATE_INVOICE,
        idempotency_key="invoice-1",
        payload={
            "customer_name": "Avery Carter",
            "customer_email": "avery@example.com",
            "amount_cents": 2500,
            "currency": "usd",
        },
    )

    envelope = build_command_envelope(request)

    assert envelope.schema_version == "1.0"
    assert envelope.command_id.startswith("cmd_")
    assert envelope.command_type == CommandType.GENERATE_INVOICE
    assert envelope.payload["currency"] == "USD"


def test_rejects_invalid_invoice_amount() -> None:
    request = CreateCommandRequest(
        command_type=CommandType.GENERATE_INVOICE,
        payload={
            "customer_name": "Avery Carter",
            "customer_email": "avery@example.com",
            "amount_cents": 0,
        },
    )

    with pytest.raises(ValidationError):
        request.typed_payload()


def test_build_event_envelope() -> None:
    event = build_event_envelope(
        event_type=EventType.COMMAND_COMPLETED,
        command_id="cmd_123",
        correlation_id="cor_123",
        result={"ok": True},
    )

    assert event.event_id.startswith("evt_")
    assert event.event_type == EventType.COMMAND_COMPLETED
    assert event.result == {"ok": True}

