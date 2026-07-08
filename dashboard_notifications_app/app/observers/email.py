from __future__ import annotations

import smtplib
from email.message import EmailMessage
from typing import Any

from sqlalchemy.orm import Session

from app.config import get_settings
from app.models import Notification
from app.observers.base import run_observer
from app.observers.utils import notifications_for_channel


def deliver_email(db: Session, event_payload: dict[str, Any]) -> dict[str, Any]:
    settings = get_settings()
    notifications = notifications_for_channel(event_payload, "email")
    if not notifications:
        return {"skipped": True, "reason": "no email notifications"}

    delivered_ids: list[str] = []
    for item in notifications:
        notification = Notification(
            command_id=event_payload.get("command_id"),
            event_id=event_payload.get("event_id"),
            channel="email",
            recipient=item["recipient"],
            subject=item.get("subject"),
            message=item["message"],
            status="sent" if settings.smtp_host else "simulated",
        )
        if settings.smtp_host:
            message = EmailMessage()
            message["From"] = settings.smtp_from_email
            message["To"] = item["recipient"]
            message["Subject"] = item.get("subject") or "Earthymix notification"
            message.set_content(item["message"])
            with smtplib.SMTP(settings.smtp_host, settings.smtp_port, timeout=10) as smtp:
                smtp.starttls()
                if settings.smtp_username and settings.smtp_password:
                    smtp.login(settings.smtp_username, settings.smtp_password)
                smtp.send_message(message)
        db.add(notification)
        db.flush()
        delivered_ids.append(notification.id)
    db.commit()
    return {"delivered_notifications": delivered_ids, "mode": "smtp" if settings.smtp_host else "simulated"}


if __name__ == "__main__":
    run_observer("email", deliver_email)

