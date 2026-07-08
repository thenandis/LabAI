from __future__ import annotations

import json
from datetime import date, datetime
from typing import Any


def _default(value: Any) -> str:
    if isinstance(value, datetime | date):
        return value.isoformat()
    return str(value)


def dumps(payload: dict[str, Any]) -> bytes:
    return json.dumps(payload, default=_default, separators=(",", ":"), sort_keys=True).encode("utf-8")


def loads(payload: bytes | str) -> dict[str, Any]:
    if isinstance(payload, bytes):
        payload = payload.decode("utf-8")
    return json.loads(payload)

