from __future__ import annotations

import hmac
import secrets
from typing import Annotated

from fastapi import Depends, Form, HTTPException, Request, status
from sqlalchemy.orm import Session

from app.config import get_settings
from app.database import get_db
from app.models import User


def ensure_csrf_token(request: Request) -> str:
    token = request.session.get("csrf_token")
    if not token:
        token = secrets.token_urlsafe(32)
        request.session["csrf_token"] = token
    return token


def verify_csrf_token(request: Request, submitted_token: str | None) -> None:
    session_token = request.session.get("csrf_token")
    if not session_token or not submitted_token:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Missing CSRF token")
    if not hmac.compare_digest(session_token, submitted_token):
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Invalid CSRF token")


def require_user(request: Request) -> str:
    username = request.session.get("username")
    if not username:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Authentication required")
    return str(username)


def verify_admin_credentials(username: str, password: str) -> bool:
    settings = get_settings()
    return hmac.compare_digest(username, settings.admin_username) and hmac.compare_digest(
        password,
        settings.admin_password,
    )


def bootstrap_admin_user(db: Session) -> User:
    settings = get_settings()
    user = db.query(User).filter(User.username == settings.admin_username).one_or_none()
    if user is None:
        user = User(username=settings.admin_username, role="admin")
        db.add(user)
        db.commit()
        db.refresh(user)
    return user


def login_form_data(
    username: Annotated[str, Form()],
    password: Annotated[str, Form()],
    csrf_token: Annotated[str, Form()],
) -> tuple[str, str, str]:
    return username, password, csrf_token


DbSession = Annotated[Session, Depends(get_db)]
CurrentUser = Annotated[str, Depends(require_user)]

