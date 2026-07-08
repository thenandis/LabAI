from __future__ import annotations

import re

import pytest

pytest.importorskip("jinja2")

from fastapi.testclient import TestClient

from app.database import get_db
from app.main import create_app
from app.models import CommandRecord


def test_command_api_accepts_form_command(db_session) -> None:
    app = create_app()

    def override_db():
        yield db_session

    app.dependency_overrides[get_db] = override_db
    client = TestClient(app)

    login_page = client.get("/login")
    csrf = re.search('name="csrf_token" value="([^"]+)"', login_page.text).group(1)
    login = client.post(
        "/login",
        data={"username": "admin", "password": "admin", "csrf_token": csrf},
        follow_redirects=False,
    )
    assert login.status_code == 303

    response = client.post(
        "/api/commands",
        data={
            "csrf_token": csrf,
            "command_type": "SendNotificationCommand",
            "channel": "email",
            "recipient": "ops@example.com",
            "subject": "Hello",
            "message": "World",
        },
        headers={"HX-Request": "true"},
    )

    assert response.status_code == 202
    assert db_session.query(CommandRecord).count() == 1
