from __future__ import annotations

from pathlib import Path
from typing import Any

from fastapi import APIRouter, Depends, HTTPException, Request, status
from fastapi.responses import HTMLResponse, JSONResponse
from fastapi.templating import Jinja2Templates
from sqlalchemy import desc, func, select
from sqlalchemy.orm import Session

from app.agents import ask_dashboard_agent
from app.database import check_database, get_db
from app.domain.commands import CommandType, CreateCommandRequest
from app.mediator import CommandMediator, dashboard_snapshot
from app.models import CommandRecord, EventRecord, Notification, ObserverDelivery
from app.security import CurrentUser, verify_csrf_token

router = APIRouter()
templates = Jinja2Templates(directory=str(Path(__file__).resolve().parents[1] / "templates"))


@router.get("/healthz")
def healthz() -> dict[str, str]:
    return {"status": "ok"}


@router.get("/readyz")
def readyz() -> dict[str, str]:
    check_database()
    return {"status": "ready"}


@router.post("/api/commands", response_model=None)
async def create_command(
    request: Request,
    current_user: CurrentUser,
    db: Session = Depends(get_db),
) -> HTMLResponse | JSONResponse:
    command_request = await _parse_command_request(request, current_user)
    record = CommandMediator().submit_command(db, command_request)
    if _is_htmx(request):
        return _render_commands(request, db, status_code=status.HTTP_202_ACCEPTED)
    return JSONResponse(
        status_code=status.HTTP_202_ACCEPTED,
        content={
            "command_id": record.id,
            "command_type": record.command_type,
            "status": record.status,
            "correlation_id": record.correlation_id,
        },
    )


@router.get("/api/commands/{command_id}")
def get_command(command_id: str, current_user: CurrentUser, db: Session = Depends(get_db)) -> dict[str, Any]:
    command = db.get(CommandRecord, command_id)
    if command is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Command not found")
    return {
        "command_id": command.id,
        "command_type": command.command_type,
        "status": command.status,
        "payload": command.payload,
        "result": command.result,
        "error": command.error,
        "correlation_id": command.correlation_id,
        "created_at": command.created_at.isoformat(),
        "updated_at": command.updated_at.isoformat(),
    }


@router.get("/api/events")
def list_events(current_user: CurrentUser, db: Session = Depends(get_db)) -> dict[str, Any]:
    events = _recent_events(db, 50)
    return {
        "events": [
            {
                "event_id": event.id,
                "event_type": event.event_type,
                "command_id": event.command_id,
                "correlation_id": event.correlation_id,
                "result": event.result,
                "occurred_at": event.occurred_at.isoformat(),
            }
            for event in events
        ]
    }


@router.get("/api/notifications")
def list_notifications(current_user: CurrentUser, db: Session = Depends(get_db)) -> dict[str, Any]:
    notifications = _recent_notifications(db, 50)
    return {
        "notifications": [
            {
                "notification_id": item.id,
                "channel": item.channel,
                "recipient": item.recipient,
                "subject": item.subject,
                "status": item.status,
                "created_at": item.created_at.isoformat(),
            }
            for item in notifications
        ]
    }


@router.post("/api/agent/query", response_model=None)
async def query_agent(
    request: Request,
    current_user: CurrentUser,
    db: Session = Depends(get_db),
) -> HTMLResponse | JSONResponse:
    if request.headers.get("content-type", "").startswith("application/json"):
        body = await request.json()
        verify_csrf_token(request, request.headers.get("x-csrf-token"))
        query = str(body.get("query", "")).strip()
    else:
        form = await request.form()
        verify_csrf_token(request, str(form.get("csrf_token", "")))
        query = str(form.get("query", "")).strip()
    if not query:
        raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="query is required")
    response = await ask_dashboard_agent(db, query)
    if _is_htmx(request):
        return templates.TemplateResponse(
            request,
            "partials/agent_response.html",
            {"response": response},
            status_code=status.HTTP_200_OK,
        )
    return JSONResponse({"response": response})


@router.get("/partials/commands", response_class=HTMLResponse)
def commands_partial(request: Request, current_user: CurrentUser, db: Session = Depends(get_db)):
    return _render_commands(request, db)


@router.get("/partials/events", response_class=HTMLResponse)
def events_partial(request: Request, current_user: CurrentUser, db: Session = Depends(get_db)):
    return templates.TemplateResponse(
        request,
        "partials/event_timeline.html",
        {"events": _recent_events(db, 20)},
    )


@router.get("/partials/notifications", response_class=HTMLResponse)
def notifications_partial(request: Request, current_user: CurrentUser, db: Session = Depends(get_db)):
    return templates.TemplateResponse(
        request,
        "partials/notification_list.html",
        {"notifications": _recent_notifications(db, 20)},
    )


@router.get("/partials/observers", response_class=HTMLResponse)
def observers_partial(request: Request, current_user: CurrentUser, db: Session = Depends(get_db)):
    return templates.TemplateResponse(
        request,
        "partials/observer_health.html",
        {"observer_health": _observer_health(db)},
    )


def _render_commands(
    request: Request,
    db: Session,
    status_code: int = status.HTTP_200_OK,
) -> HTMLResponse:
    return templates.TemplateResponse(
        request,
        "partials/command_table.html",
        {"commands": _recent_commands(db, 20)},
        status_code=status_code,
    )


async def _parse_command_request(request: Request, actor: str) -> CreateCommandRequest:
    content_type = request.headers.get("content-type", "")
    if content_type.startswith("application/json"):
        body = await request.json()
        verify_csrf_token(request, request.headers.get("x-csrf-token"))
        body["actor"] = actor
        return CreateCommandRequest.model_validate(body)

    form = await request.form()
    verify_csrf_token(request, str(form.get("csrf_token", "")))
    command_type = CommandType(str(form["command_type"]))
    if command_type == CommandType.GENERATE_INVOICE:
        dollars = str(form.get("amount_dollars", "0")).replace(",", "").strip()
        amount_cents = int(round(float(dollars) * 100))
        due_date = str(form.get("due_date", "")).strip() or None
        payload = {
            "customer_name": str(form["customer_name"]).strip(),
            "customer_email": str(form["customer_email"]).strip(),
            "amount_cents": amount_cents,
            "currency": str(form.get("currency", "USD")).strip() or "USD",
            "due_date": due_date,
            "memo": str(form.get("memo", "")).strip() or None,
        }
    else:
        payload = {
            "channel": str(form["channel"]),
            "recipient": str(form["recipient"]).strip(),
            "subject": str(form.get("subject", "")).strip() or None,
            "message": str(form["message"]).strip(),
        }
    return CreateCommandRequest(
        command_type=command_type,
        payload=payload,
        idempotency_key=str(form.get("idempotency_key", "")).strip() or None,
        actor=actor,
    )


def _recent_commands(db: Session, limit: int) -> list[CommandRecord]:
    return db.scalars(select(CommandRecord).order_by(desc(CommandRecord.created_at)).limit(limit)).all()


def _recent_events(db: Session, limit: int) -> list[EventRecord]:
    return db.scalars(select(EventRecord).order_by(desc(EventRecord.occurred_at)).limit(limit)).all()


def _recent_notifications(db: Session, limit: int) -> list[Notification]:
    return db.scalars(select(Notification).order_by(desc(Notification.created_at)).limit(limit)).all()


def _observer_health(db: Session) -> list[dict[str, Any]]:
    rows = db.execute(
        select(
            ObserverDelivery.observer_name,
            ObserverDelivery.status,
            func.count(ObserverDelivery.id).label("count"),
        )
        .group_by(ObserverDelivery.observer_name, ObserverDelivery.status)
        .order_by(ObserverDelivery.observer_name, ObserverDelivery.status)
    ).all()
    if not rows:
        return [
            {"observer_name": name, "status": "waiting", "count": 0}
            for name in ["email", "sms", "analytics"]
        ]
    return [dict(row._mapping) for row in rows]


def _is_htmx(request: Request) -> bool:
    return request.headers.get("hx-request", "").lower() == "true"
