from __future__ import annotations

from contextlib import asynccontextmanager
from pathlib import Path

from fastapi import FastAPI, Form, Request
from fastapi.responses import RedirectResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from starlette.status import HTTP_303_SEE_OTHER

from app.api.routes import router as api_router
from app.config import get_settings
from app.database import SessionLocal, init_db
from app.mcp_server.routes import router as mcp_router
from app.mediator import dashboard_snapshot
from app.security import bootstrap_admin_user, ensure_csrf_token, verify_admin_credentials, verify_csrf_token
from app.session import CookieSessionMiddleware

BASE_DIR = Path(__file__).resolve().parent
templates = Jinja2Templates(directory=str(BASE_DIR / "templates"))


def create_app() -> FastAPI:
    settings = get_settings()

    @asynccontextmanager
    async def lifespan(app: FastAPI):  # noqa: ARG001
        if settings.auto_create_tables and not settings.is_production:
            init_db()
        with SessionLocal() as db:
            bootstrap_admin_user(db)
        yield

    app = FastAPI(title=settings.app_name, version="1.0.0", lifespan=lifespan)
    app.add_middleware(
        CookieSessionMiddleware,
        secret_key=settings.session_secret,
        https_only=settings.secure_cookies,
        same_site="lax",
        session_cookie="labai_session",
    )
    app.mount("/static", StaticFiles(directory=str(BASE_DIR / "static")), name="static")
    app.include_router(api_router)
    app.include_router(mcp_router)

    @app.get("/")
    def dashboard(request: Request):
        if not request.session.get("username"):
            return RedirectResponse("/login", status_code=HTTP_303_SEE_OTHER)
        csrf_token = ensure_csrf_token(request)
        with SessionLocal() as db:
            return templates.TemplateResponse(
                request,
                "dashboard.html",
                {
                    "settings": settings,
                    "csrf_token": csrf_token,
                    "snapshot": dashboard_snapshot(db),
                    "username": request.session["username"],
                },
            )

    @app.get("/login")
    def login_page(request: Request):
        csrf_token = ensure_csrf_token(request)
        return templates.TemplateResponse(
            request,
            "login.html",
            {"csrf_token": csrf_token, "settings": settings, "error": None},
        )

    @app.post("/login")
    def login(
        request: Request,
        username: str = Form(),
        password: str = Form(),
        csrf_token: str = Form(),
    ):
        verify_csrf_token(request, csrf_token)
        if not verify_admin_credentials(username, password):
            return templates.TemplateResponse(
                request,
                "login.html",
                {
                    "csrf_token": ensure_csrf_token(request),
                    "settings": settings,
                    "error": "Invalid username or password.",
                },
                status_code=401,
            )
        request.session["username"] = username
        ensure_csrf_token(request)
        return RedirectResponse("/", status_code=HTTP_303_SEE_OTHER)

    @app.post("/logout")
    def logout(request: Request, csrf_token: str = Form()):
        verify_csrf_token(request, csrf_token)
        request.session.clear()
        return RedirectResponse("/login", status_code=HTTP_303_SEE_OTHER)

    return app


app = create_app()
