from __future__ import annotations

from typing import Any

from sqlalchemy.orm import Session

from app.models import AuditLog
from app.observers.base import run_observer


def deliver_analytics(db: Session, event_payload: dict[str, Any]) -> dict[str, Any]:
    event_id = str(event_payload["event_id"])
    db.add(
        AuditLog(
            actor="analytics-observer",
            action="analytics.event_recorded",
            entity_type="event",
            entity_id=event_id,
            metadata_json={
                "event_type": event_payload.get("event_type"),
                "command_id": event_payload.get("command_id"),
                "correlation_id": event_payload.get("correlation_id"),
            },
        )
    )
    db.commit()
    return {"recorded": True, "event_id": event_id}


if __name__ == "__main__":
    run_observer("analytics", deliver_analytics)

