from __future__ import annotations

import uuid
from datetime import UTC, datetime
from enum import StrEnum
from typing import Any, Literal

from pydantic import BaseModel


class EventType(StrEnum):
    COMMAND_COMPLETED = "CommandCompletedEvent"
    COMMAND_FAILED = "CommandFailedEvent"


class EventEnvelope(BaseModel):
    schema_version: Literal["1.0"] = "1.0"
    event_id: str
    event_type: EventType
    command_id: str | None
    correlation_id: str
    result: dict[str, Any]
    occurred_at: datetime


def build_event_envelope(
    *,
    event_type: EventType,
    command_id: str | None,
    correlation_id: str,
    result: dict[str, Any],
) -> EventEnvelope:
    return EventEnvelope(
        event_id=f"evt_{uuid.uuid4().hex}",
        event_type=event_type,
        command_id=command_id,
        correlation_id=correlation_id,
        result=result,
        occurred_at=datetime.now(UTC),
    )

