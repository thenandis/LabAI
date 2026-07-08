from __future__ import annotations

import logging
import time
from datetime import UTC, datetime

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.config import get_settings
from app.database import SessionLocal
from app.kafka.client import KafkaProducerClient, KafkaUnavailable
from app.models import OutboxMessage

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO)


def dispatch_once(db: Session, producer: KafkaProducerClient, limit: int = 50) -> int:
    messages = db.scalars(
        select(OutboxMessage)
        .where(OutboxMessage.status == "pending")
        .order_by(OutboxMessage.created_at)
        .limit(limit)
    ).all()
    dispatched = 0
    for message in messages:
        message.attempts += 1
        try:
            producer.publish(topic=message.topic, key=message.message_key, payload=message.payload)
            message.status = "published"
            message.published_at = datetime.now(UTC)
            message.error = None
            dispatched += 1
        except Exception as exc:
            logger.exception("Failed to publish outbox message %s", message.id)
            message.error = str(exc)
            if message.attempts >= 5:
                message.status = "failed"
        finally:
            db.commit()
    return dispatched


def run() -> None:
    settings = get_settings()
    try:
        producer = KafkaProducerClient()
    except KafkaUnavailable as exc:
        logger.error("Kafka unavailable for outbox dispatcher: %s", exc)
        raise

    while True:
        with SessionLocal() as db:
            dispatched = dispatch_once(db, producer)
            if dispatched:
                logger.info("Dispatched %s outbox messages", dispatched)
        time.sleep(settings.kafka_poll_seconds)


if __name__ == "__main__":
    run()

