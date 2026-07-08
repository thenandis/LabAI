from __future__ import annotations

import uuid
from datetime import date
from typing import Any

from app.domain.commands import (
    CommandEnvelope,
    CommandType,
    GenerateInvoicePayload,
    SendNotificationPayload,
)


def handle_command(envelope: CommandEnvelope) -> dict[str, Any]:
    if envelope.command_type == CommandType.GENERATE_INVOICE:
        payload = GenerateInvoicePayload.model_validate(envelope.payload)
        return _generate_invoice(envelope, payload)
    if envelope.command_type == CommandType.SEND_NOTIFICATION:
        payload = SendNotificationPayload.model_validate(envelope.payload)
        return _send_notification(envelope, payload)
    raise ValueError(f"Unsupported command type {envelope.command_type}")


def _generate_invoice(envelope: CommandEnvelope, payload: GenerateInvoicePayload) -> dict[str, Any]:
    invoice_id = f"inv_{uuid.uuid4().hex[:12]}"
    due_date = payload.due_date or date.today()
    amount = f"{payload.amount_cents / 100:.2f} {payload.currency}"
    return {
        "invoice_id": invoice_id,
        "customer_name": payload.customer_name,
        "customer_email": payload.customer_email,
        "amount_cents": payload.amount_cents,
        "currency": payload.currency,
        "due_date": due_date.isoformat(),
        "memo": payload.memo,
        "notification": {
            "channel": "email",
            "recipient": payload.customer_email,
            "subject": f"Invoice {invoice_id} from Earthymix",
            "message": (
                f"Hello {payload.customer_name}, your invoice {invoice_id} "
                f"for {amount} is due on {due_date.isoformat()}."
            ),
        },
        "source_command_id": envelope.command_id,
    }


def _send_notification(
    envelope: CommandEnvelope, payload: SendNotificationPayload
) -> dict[str, Any]:
    return {
        "notification": {
            "channel": payload.channel.value,
            "recipient": payload.recipient,
            "subject": payload.subject,
            "message": payload.message,
        },
        "source_command_id": envelope.command_id,
    }

