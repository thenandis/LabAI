from __future__ import annotations

from typing import Any

from fastapi import APIRouter, Depends, HTTPException, Request, status
from sqlalchemy.orm import Session

from app.config import get_settings
from app.database import get_db
from app.mcp_server.tools import call_tool, tool_definitions

router = APIRouter()


def require_mcp_token(request: Request) -> None:
    expected = f"Bearer {get_settings().mcp_server_token}"
    provided = request.headers.get("authorization")
    if provided != expected:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid MCP token")


@router.get("/mcp", dependencies=[Depends(require_mcp_token)])
def mcp_manifest() -> dict[str, Any]:
    return {
        "name": "labai-dashboard-mcp",
        "version": "1.0.0",
        "transport": "streamable-http-json",
        "tools": tool_definitions(),
    }


@router.post("/mcp", dependencies=[Depends(require_mcp_token)])
async def mcp_rpc(request: Request, db: Session = Depends(get_db)) -> dict[str, Any]:
    body = await request.json()
    request_id = body.get("id")
    method = body.get("method")
    try:
        if method == "initialize":
            result = {
                "serverInfo": {"name": "labai-dashboard-mcp", "version": "1.0.0"},
                "capabilities": {"tools": {}},
            }
        elif method == "tools/list":
            result = {"tools": tool_definitions()}
        elif method == "tools/call":
            params = body.get("params") or {}
            structured = call_tool(db, params["name"], params.get("arguments"))
            result = {
                "content": [
                    {
                        "type": "text",
                        "text": str(structured),
                    }
                ],
                "structuredContent": structured,
            }
        elif "tool" in body:
            result = call_tool(db, body["tool"], body.get("arguments"))
        else:
            raise ValueError(f"Unsupported MCP method: {method}")
        return {"jsonrpc": "2.0", "id": request_id, "result": result}
    except Exception as exc:
        return {
            "jsonrpc": "2.0",
            "id": request_id,
            "error": {"code": -32000, "message": str(exc)},
        }
