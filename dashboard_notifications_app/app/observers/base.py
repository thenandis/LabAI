from __future__ import annotations

import logging
import time
from collections.abc import Callable
from datetime import UTC, datetime
from typing import Any

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.config import get_settings
from app.database import SessionLocal
from app.kafka.client import KafkaConsumerClient, KafkaProducerClient
from app.kafka.topics import EVENTS_DLQ_TOPIC, EVENTS_TOPIC
from app.models import AuditLog, ObserverDelivery

logger = logging.getLogger(__name__)

DeliveryFunction = Callable[[Session, dict[str, Any]], dict[str, Any]]


def handle_event_for_observer(
    db: Session,
    *,
    observer_name: str,
    event_payload: dict[str, Any],
    deliver: DeliveryFunction,
    dlq_producer: KafkaProducerClient | None = None,
) -> ObserverDelivery:
    settings = get_settings()
    event_id = str(event_payload["event_id"])
    delivery = db.scalar(
        select(ObserverDelivery).where(
            ObserverDelivery.observer_name == observer_name,
            ObserverDelivery.event_id == event_id,
        )
    )
    if delivery and delivery.status == "delivered":
        return delivery
    if delivery is None:
        delivery = ObserverDelivery(observer_name=observer_name, event_id=event_id)
        db.add(delivery)
        db.commit()
        db.refresh(delivery)

    last_error: str | None = None
    while delivery.attempts < settings.observer_max_attempts:
        delivery.attempts += 1
        delivery.updated_at = datetime.now(UTC)
        try:
            result = deliver(db, event_payload)
            delivery.status = "delivered"
            delivery.error = None
            db.add(
                AuditLog(
                    actor=observer_name,
                    action="observer.delivered",
                    entity_type="event",
                    entity_id=event_id,
                    metadata_json=result,
                )
            )
            db.commit()
            db.refresh(delivery)
            return delivery
        except Exception as exc:
            last_error = str(exc)
            delivery.status = "retrying"
            delivery.error = last_error
            db.commit()
            logger.warning(
                "Observer %s failed event %s attempt %s/%s: %s",
                observer_name,
                event_id,
                delivery.attempts,
                settings.observer_max_attempts,
                exc,
            )
            time.sleep(settings.observer_retry_backoff_seconds)

    delivery.status = "failed"
    delivery.error = last_error
    delivery.updated_at = datetime.now(UTC)
    db.commit()
    if dlq_producer is not None:
        dlq_producer.publish(
            topic=EVENTS_DLQ_TOPIC,
            key=event_id,
            payload={
                "observer_name": observer_name,
                "event": event_payload,
                "error": last_error,
            },
        )
    return delivery


def run_observer(observer_name: str, deliver: DeliveryFunction) -> None:
    logging.basicConfig(level=logging.INFO)
    consumer = KafkaConsumerClient(
        group_id=f"labai-{observer_name}-observer",
        topics=[EVENTS_TOPIC],
    )
    dlq_producer = KafkaProducerClient()
    try:
        for payload in consumer.messages():
            with SessionLocal() as db:
                delivery = handle_event_for_observer(
                    db,
                    observer_name=observer_name,
                    event_payload=payload,
                    deliver=deliver,
                    dlq_producer=dlq_producer,
                )
                logger.info(
                    "Observer %s handled event %s status=%s",
                    observer_name,
                    payload.get("event_id"),
                    delivery.status,
                )
    finally:
        consumer.close()

