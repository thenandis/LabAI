from __future__ import annotations

"""Standalone MCP server entrypoint for local inspector use.

The FastAPI app exposes the same tools at /mcp. This module lets developers run
`python -m app.mcp_server.server` when they want a plain MCP server process.
"""

from app.database import SessionLocal
from app.mcp_server.tools import call_tool

try:  # pragma: no cover - exercised only when MCP SDK is installed and launched
    from mcp.server.fastmcp import FastMCP
except Exception:  # pragma: no cover
    FastMCP = None  # type: ignore[assignment]


if FastMCP is not None:  # pragma: no branch
    mcp = FastMCP("LabAI Dashboard", json_response=True)

    @mcp.tool()
    def get_dashboard_snapshot() -> dict:
        """Read aggregate dashboard state and recent activity."""
        with SessionLocal() as db:
            return call_tool(db, "get_dashboard_snapshot", {})

    @mcp.tool()
    def list_recent_events(limit: int = 10) -> dict:
        """List recent event envelopes."""
        with SessionLocal() as db:
            return call_tool(db, "list_recent_events", {"limit": limit})

    @mcp.tool()
    def get_command_status(command_id: str) -> dict:
        """Read status for one command."""
        with SessionLocal() as db:
            return call_tool(db, "get_command_status", {"command_id": command_id})

    @mcp.tool()
    def trigger_command(
        command_type: str,
        payload: dict,
        idempotency_key: str | None = None,
        approval_token: str | None = None,
    ) -> dict:
        """Submit a command through the mediator. Requires mutation approval settings."""
        with SessionLocal() as db:
            return call_tool(
                db,
                "trigger_command",
                {
                    "command_type": command_type,
                    "payload": payload,
                    "idempotency_key": idempotency_key,
                    "approval_token": approval_token,
                },
            )


def main() -> None:
    if FastMCP is None:
        raise RuntimeError("mcp is not installed. Install requirements.txt first.")
    mcp.run(transport="streamable-http")


if __name__ == "__main__":
    main()

