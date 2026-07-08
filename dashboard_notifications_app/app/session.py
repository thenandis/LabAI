from __future__ import annotations

import base64
import hashlib
import hmac
import json
from http.cookies import SimpleCookie
from typing import Any

from starlette.datastructures import MutableHeaders
from starlette.types import ASGIApp, Message, Receive, Scope, Send


class CookieSessionMiddleware:
    """Small signed-cookie session middleware.

    Starlette's SessionMiddleware is excellent, but it depends on itsdangerous.
    This keeps the sample app self-contained in local environments while still
    providing signed, HTTP-only session cookies for the dashboard.
    """

    def __init__(
        self,
        app: ASGIApp,
        *,
        secret_key: str,
        session_cookie: str = "session",
        max_age: int = 14 * 24 * 60 * 60,
        same_site: str = "lax",
        https_only: bool = False,
    ) -> None:
        self.app = app
        self.secret_key = secret_key.encode("utf-8")
        self.session_cookie = session_cookie
        self.max_age = max_age
        self.same_site = same_site
        self.https_only = https_only

    async def __call__(self, scope: Scope, receive: Receive, send: Send) -> None:
        if scope["type"] not in {"http", "websocket"}:
            await self.app(scope, receive, send)
            return

        session = self._load_session(scope)
        initial_session = dict(session)
        scope["session"] = session

        async def send_wrapper(message: Message) -> None:
            if message["type"] == "http.response.start":
                headers = MutableHeaders(scope=message)
                if session:
                    headers.append("Set-Cookie", self._cookie_header(session))
                elif initial_session:
                    headers.append("Set-Cookie", self._expired_cookie_header())
            await send(message)

        await self.app(scope, receive, send_wrapper)

    def _load_session(self, scope: Scope) -> dict[str, Any]:
        raw_headers = dict(scope.get("headers") or [])
        cookie_header = raw_headers.get(b"cookie", b"").decode("latin-1")
        cookies = SimpleCookie(cookie_header)
        morsel = cookies.get(self.session_cookie)
        if not morsel:
            return {}
        try:
            payload, signature = morsel.value.rsplit(".", 1)
        except ValueError:
            return {}
        expected = self._sign(payload)
        if not hmac.compare_digest(signature, expected):
            return {}
        try:
            decoded = base64.urlsafe_b64decode(_pad_base64(payload)).decode("utf-8")
            data = json.loads(decoded)
        except (ValueError, json.JSONDecodeError):
            return {}
        return data if isinstance(data, dict) else {}

    def _cookie_header(self, session: dict[str, Any]) -> str:
        data = json.dumps(session, separators=(",", ":"), sort_keys=True).encode("utf-8")
        payload = base64.urlsafe_b64encode(data).decode("ascii").rstrip("=")
        value = f"{payload}.{self._sign(payload)}"
        parts = [
            f"{self.session_cookie}={value}",
            f"Max-Age={self.max_age}",
            "Path=/",
            "HttpOnly",
            f"SameSite={self.same_site}",
        ]
        if self.https_only:
            parts.append("Secure")
        return "; ".join(parts)

    def _expired_cookie_header(self) -> str:
        return (
            f"{self.session_cookie}=null; Max-Age=0; Path=/; "
            f"HttpOnly; SameSite={self.same_site}"
        )

    def _sign(self, payload: str) -> str:
        return hmac.new(self.secret_key, payload.encode("ascii"), hashlib.sha256).hexdigest()


def _pad_base64(value: str) -> bytes:
    padding = "=" * (-len(value) % 4)
    return f"{value}{padding}".encode("ascii")

