from __future__ import annotations

from typing import Any

from sqlalchemy import desc, select
from sqlalchemy.orm import Session

from app.config import get_settings
from app.domain.commands import CommandType, CreateCommandRequest
from app.mediator import CommandMediator, dashboard_snapshot
from app.models import CommandRecord, EventRecord


def tool_definitions() -> list[dict[str, Any]]:
    return [
        {
            "name": "get_dashboard_snapshot",
            "description": "Read aggregate dashboard state, recent commands, recent events, and observer health.",
            "inputSchema": {"type": "object", "properties": {}, "additionalProperties": False},
        },
        {
            "name": "list_recent_events",
            "description": "List recent event envelopes from the event timeline.",
            "inputSchema": {
                "type": "object",
                "properties": {"limit": {"type": "integer", "minimum": 1, "maximum": 50}},
                "additionalProperties": False,
            },
        },
        {
            "name": "get_command_status",
            "description": "Read one command's current status, result, and error.",
            "inputSchema": {
                "type": "object",
                "properties": {"command_id": {"type": "string"}},
                "required": ["command_id"],
                "additionalProperties": False,
            },
        },
        {
            "name": "trigger_command",
            "description": "Submit a command through the same mediator used by the dashboard.",
            "inputSchema": {
                "type": "object",
                "properties": {
                    "command_type": {
                        "type": "string",
                        "enum": [item.value for item in CommandType],
                    },
                    "payload": {"type": "object"},
                    "idempotency_key": {"type": "string"},
                    "approval_token": {"type": "string"},
                },
                "required": ["command_type", "payload"],
                "additionalProperties": False,
            },
        },
    ]


def call_tool(db: Session, name: str, arguments: dict[str, Any] | None = None) -> dict[str, Any]:
    arguments = arguments or {}
    if name == "get_dashboard_snapshot":
        return dashboard_snapshot(db)
    if name == "list_recent_events":
        return _list_recent_events(db, int(arguments.get("limit", 10)))
    if name == "get_command_status":
        return _get_command_status(db, str(arguments["command_id"]))
    if name == "trigger_command":
        return _trigger_command(db, arguments)
    raise ValueError(f"Unknown MCP tool: {name}")


def _list_recent_events(db: Session, limit: int) -> dict[str, Any]:
    limit = max(1, min(limit, 50))
    events = db.scalars(select(EventRecord).order_by(desc(EventRecord.occurred_at)).limit(limit)).all()
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


def _get_command_status(db: Session, command_id: str) -> dict[str, Any]:
    command = db.get(CommandRecord, command_id)
    if command is None:
        return {"found": False, "command_id": command_id}
    return {
        "found": True,
        "command_id": command.id,
        "command_type": command.command_type,
        "status": command.status,
        "result": command.result,
        "error": command.error,
        "created_at": command.created_at.isoformat(),
        "updated_at": command.updated_at.isoformat(),
    }


def _trigger_command(db: Session, arguments: dict[str, Any]) -> dict[str, Any]:
    settings = get_settings()
    if not settings.mcp_allow_mutations:
        raise PermissionError("Mutating MCP tools are disabled. Set MCP_ALLOW_MUTATIONS=true to enable.")
    approval_token = arguments.get("approval_token")
    if settings.mcp_mutation_approval_token and approval_token != settings.mcp_mutation_approval_token:
        raise PermissionError("Invalid MCP mutation approval token.")
    request = CreateCommandRequest(
        command_type=arguments["command_type"],
        payload=arguments["payload"],
        idempotency_key=arguments.get("idempotency_key"),
        actor="mcp-agent",
    )
    record = CommandMediator().submit_command(db, request)
    return {
        "command_id": record.id,
        "status": record.status,
        "command_type": record.command_type,
        "correlation_id": record.correlation_id,
    }

