from __future__ import annotations

import os
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path

from dotenv import load_dotenv


def load_environment() -> None:
    app_root = Path(__file__).resolve().parents[1]
    repo_root = app_root.parent
    for env_file in (repo_root / ".env", app_root / ".env"):
        if env_file.exists():
            load_dotenv(env_file, override=False)


load_environment()


def _bool(name: str, default: bool = False) -> bool:
    raw = os.getenv(name)
    if raw is None:
        return default
    return raw.strip().lower() in {"1", "true", "yes", "on"}


def _int(name: str, default: int) -> int:
    raw = os.getenv(name)
    if raw is None:
        return default
    try:
        return int(raw)
    except ValueError:
        return default


def _normalize_database_url(url: str) -> str:
    if url.startswith("postgres://"):
        return "postgresql+psycopg://" + url.removeprefix("postgres://")
    if url.startswith("postgresql://") and "+psycopg" not in url:
        return "postgresql+psycopg://" + url.removeprefix("postgresql://")
    return url


@dataclass(frozen=True)
class Settings:
    app_name: str = os.getenv("APP_NAME", "LabAI Event Dashboard")
    app_env: str = os.getenv("APP_ENV", "development")
    app_base_url: str = os.getenv("APP_BASE_URL", "http://localhost:8000")
    database_url: str = os.getenv("DATABASE_URL", "sqlite:///./labai_dashboard.db")
    session_secret: str = os.getenv("SESSION_SECRET", "dev-only-change-me")
    admin_username: str = os.getenv("ADMIN_USERNAME", "admin")
    admin_password: str = os.getenv("ADMIN_PASSWORD", "admin")
    csrf_cookie_name: str = "labai_csrf"
    auto_create_tables: bool = _bool("AUTO_CREATE_TABLES", True)

    kafka_bootstrap_servers: str = os.getenv("KAFKA_BOOTSTRAP_SERVERS", "localhost:9092")
    kafka_security_protocol: str = os.getenv("KAFKA_SECURITY_PROTOCOL", "PLAINTEXT")
    kafka_sasl_mechanism: str = os.getenv("KAFKA_SASL_MECHANISM", "PLAIN")
    kafka_sasl_username: str | None = os.getenv("KAFKA_SASL_USERNAME")
    kafka_sasl_password: str | None = os.getenv("KAFKA_SASL_PASSWORD")
    kafka_client_id: str = os.getenv("KAFKA_CLIENT_ID", "labai-dashboard")
    kafka_enabled: bool = _bool("KAFKA_ENABLED", True)
    kafka_poll_seconds: float = float(os.getenv("KAFKA_POLL_SECONDS", "1.0"))

    openai_api_key: str | None = os.getenv("OPENAI_API_KEY")
    phi_api_key: str | None = os.getenv("PHI_API_KEY")
    openai_model: str = os.getenv("OPENAI_MODEL", "gpt-5-nano")
    agent_timeout_seconds: int = _int("AGENT_TIMEOUT_SECONDS", 30)

    mcp_server_token: str = os.getenv("MCP_SERVER_TOKEN", "dev-mcp-token")
    mcp_allow_mutations: bool = _bool("MCP_ALLOW_MUTATIONS", False)
    mcp_mutation_approval_token: str | None = os.getenv("MCP_MUTATION_APPROVAL_TOKEN")

    smtp_host: str | None = os.getenv("SMTP_HOST")
    smtp_port: int = _int("SMTP_PORT", 587)
    smtp_username: str | None = os.getenv("SMTP_USERNAME")
    smtp_password: str | None = os.getenv("SMTP_PASSWORD")
    smtp_from_email: str = os.getenv("SMTP_FROM_EMAIL", "notifications@earthymix.com")

    twilio_account_sid: str | None = os.getenv("TWILIO_ACCOUNT_SID")
    twilio_auth_token: str | None = os.getenv("TWILIO_AUTH_TOKEN")
    twilio_from_number: str | None = os.getenv("TWILIO_FROM_NUMBER")

    observer_max_attempts: int = _int("OBSERVER_MAX_ATTEMPTS", 3)
    observer_retry_backoff_seconds: int = _int("OBSERVER_RETRY_BACKOFF_SECONDS", 5)

    @property
    def database_url_for_sqlalchemy(self) -> str:
        return _normalize_database_url(self.database_url)

    @property
    def is_production(self) -> bool:
        return self.app_env.lower() == "production"

    @property
    def secure_cookies(self) -> bool:
        return self.is_production


@lru_cache
def get_settings() -> Settings:
    return Settings()


def kafka_config(group_id: str | None = None) -> dict[str, str]:
    settings = get_settings()
    config: dict[str, str] = {
        "bootstrap.servers": settings.kafka_bootstrap_servers,
        "client.id": settings.kafka_client_id,
    }
    if group_id:
        config["group.id"] = group_id
        config["auto.offset.reset"] = "earliest"
        config["enable.auto.commit"] = "false"
    if settings.kafka_security_protocol:
        config["security.protocol"] = settings.kafka_security_protocol
    if settings.kafka_security_protocol != "PLAINTEXT":
        config["sasl.mechanism"] = settings.kafka_sasl_mechanism
        if settings.kafka_sasl_username:
            config["sasl.username"] = settings.kafka_sasl_username
        if settings.kafka_sasl_password:
            config["sasl.password"] = settings.kafka_sasl_password
    return config
