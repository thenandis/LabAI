from __future__ import annotations

from typing import Any


def notifications_for_channel(event_payload: dict[str, Any], channel: str) -> list[dict[str, Any]]:
    result = event_payload.get("result") or {}
    raw_notifications: list[dict[str, Any]] = []
    if isinstance(result.get("notification"), dict):
        raw_notifications.append(result["notification"])
    if isinstance(result.get("notifications"), list):
        raw_notifications.extend(item for item in result["notifications"] if isinstance(item, dict))
    return [
        notification
        for notification in raw_notifications
        if str(notification.get("channel", "")).lower() == channel
    ]

