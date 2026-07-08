from __future__ import annotations

import httpx
from typing import Any

from sqlalchemy.orm import Session

from app.config import get_settings
from app.models import Notification
from app.observers.base import run_observer
from app.observers.utils import notifications_for_channel


def deliver_sms(db: Session, event_payload: dict[str, Any]) -> dict[str, Any]:
    settings = get_settings()
    notifications = notifications_for_channel(event_payload, "sms")
    if not notifications:
        return {"skipped": True, "reason": "no sms notifications"}

    delivered_ids: list[str] = []
    for item in notifications:
        provider_message_id = None
        status = "simulated"
        if settings.twilio_account_sid and settings.twilio_auth_token and settings.twilio_from_number:
            response = httpx.post(
                f"https://api.twilio.com/2010-04-01/Accounts/{settings.twilio_account_sid}/Messages.json",
                data={
                    "From": settings.twilio_from_number,
                    "To": item["recipient"],
                    "Body": item["message"],
                },
                auth=(settings.twilio_account_sid, settings.twilio_auth_token),
                timeout=10,
            )
            response.raise_for_status()
            body = response.json()
            provider_message_id = body.get("sid")
            status = "sent"
        notification = Notification(
            command_id=event_payload.get("command_id"),
            event_id=event_payload.get("event_id"),
            channel="sms",
            recipient=item["recipient"],
            subject=item.get("subject"),
            message=item["message"],
            status=status,
            provider_message_id=provider_message_id,
        )
        db.add(notification)
        db.flush()
        delivered_ids.append(notification.id)
    db.commit()
    return {"delivered_notifications": delivered_ids, "mode": status}


if __name__ == "__main__":
    run_observer("sms", deliver_sms)

