from __future__ import annotations

import asyncio
from typing import Any

from sqlalchemy.orm import Session

from app.config import get_settings
from app.mediator import dashboard_snapshot
from app.models import AgentRun


async def ask_dashboard_agent(db: Session, query: str) -> str:
    settings = get_settings()
    if not settings.openai_api_key:
        response = _fallback_response(db, query, "OPENAI_API_KEY is not configured")
        _record_agent_run(db, query, response, "fallback")
        return response

    try:
        response = await asyncio.wait_for(_run_openai_agent(query), settings.agent_timeout_seconds)
        _record_agent_run(db, query, response, "completed")
        return response
    except Exception as exc:
        response = _fallback_response(db, query, f"agent runtime failed: {exc}")
        _record_agent_run(db, query, response, "fallback")
        return response


async def _run_openai_agent(query: str) -> str:
    settings = get_settings()
    from agents import Agent, Runner
    from agents.mcp import MCPServerStreamableHttp

    async with MCPServerStreamableHttp(
        name="LabAI Dashboard MCP",
        params={
            "url": f"{settings.app_base_url.rstrip('/')}/mcp",
            "headers": {"Authorization": f"Bearer {settings.mcp_server_token}"},
            "timeout": 10,
        },
        cache_tools_list=True,
        require_approval={"always": {"tool_names": ["trigger_command"]}},
    ) as mcp_server:
        agent = Agent(
            name="LabAI Dashboard Operator",
            instructions=(
                "You help operators understand command processing, notification delivery, "
                "Kafka event flow, and observer health. Prefer read-only MCP tools. "
                "Only trigger commands when the user explicitly asks and approval is available."
            ),
            model=settings.openai_model,
            mcp_servers=[mcp_server],
        )
        result = await Runner.run(agent, query)
        return str(result.final_output)


def _fallback_response(db: Session, query: str, reason: str) -> str:
    snapshot = dashboard_snapshot(db)
    return (
        f"Agent fallback active because {reason}. "
        f"Query: {query}. "
        f"Current snapshot: {snapshot['pending_outbox']} pending outbox messages, "
        f"{len(snapshot['recent_commands'])} recent commands, "
        f"{len(snapshot['recent_events'])} recent events."
    )


def _record_agent_run(db: Session, query: str, response: str, status: str) -> AgentRun:
    run = AgentRun(query=query, response=response, status=status)
    db.add(run)
    db.commit()
    db.refresh(run)
    return run

