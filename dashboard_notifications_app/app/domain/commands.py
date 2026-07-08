from __future__ import annotations

import uuid
from datetime import UTC, datetime, date
from enum import StrEnum
from typing import Any, Literal

from pydantic import BaseModel, Field, field_validator


class CommandType(StrEnum):
    GENERATE_INVOICE = "GenerateInvoiceCommand"
    SEND_NOTIFICATION = "SendNotificationCommand"


class NotificationChannel(StrEnum):
    EMAIL = "email"
    SMS = "sms"


class GenerateInvoicePayload(BaseModel):
    customer_name: str = Field(min_length=1, max_length=160)
    customer_email: str = Field(min_length=3, max_length=240)
    amount_cents: int = Field(gt=0)
    currency: str = Field(default="USD", min_length=3, max_length=3)
    due_date: date | None = None
    memo: str | None = Field(default=None, max_length=500)

    @field_validator("currency")
    @classmethod
    def normalize_currency(cls, value: str) -> str:
        return value.upper()


class SendNotificationPayload(BaseModel):
    channel: NotificationChannel
    recipient: str = Field(min_length=3, max_length=240)
    subject: str | None = Field(default=None, max_length=240)
    message: str = Field(min_length=1, max_length=2000)


PayloadModel = GenerateInvoicePayload | SendNotificationPayload


class CreateCommandRequest(BaseModel):
    command_type: CommandType
    payload: dict[str, Any]
    idempotency_key: str | None = Field(default=None, max_length=160)
    actor: str = Field(default="dashboard-user", max_length=160)

    def typed_payload(self) -> PayloadModel:
        if self.command_type == CommandType.GENERATE_INVOICE:
            return GenerateInvoicePayload.model_validate(self.payload)
        if self.command_type == CommandType.SEND_NOTIFICATION:
            return SendNotificationPayload.model_validate(self.payload)
        raise ValueError(f"Unsupported command type: {self.command_type}")


class CommandEnvelope(BaseModel):
    schema_version: Literal["1.0"] = "1.0"
    command_id: str
    command_type: CommandType
    correlation_id: str
    idempotency_key: str | None
    actor: str
    payload: dict[str, Any]
    created_at: datetime


def build_command_envelope(request: CreateCommandRequest) -> CommandEnvelope:
    typed_payload = request.typed_payload()
    command_id = f"cmd_{uuid.uuid4().hex}"
    return CommandEnvelope(
        command_id=command_id,
        command_type=request.command_type,
        correlation_id=f"cor_{uuid.uuid4().hex}",
        idempotency_key=request.idempotency_key,
        actor=request.actor,
        payload=typed_payload.model_dump(mode="json"),
        created_at=datetime.now(UTC),
    )

