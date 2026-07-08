from __future__ import annotations

import logging
from collections.abc import Iterator
from typing import Any

from app.config import get_settings, kafka_config
from app.kafka.serialization import dumps, loads

logger = logging.getLogger(__name__)


class KafkaUnavailable(RuntimeError):
    pass


class KafkaProducerClient:
    def __init__(self) -> None:
        try:
            from confluent_kafka import Producer
        except Exception as exc:  # pragma: no cover - depends on optional runtime package
            raise KafkaUnavailable("confluent-kafka is not installed") from exc
        self._producer = Producer(kafka_config())

    def publish(self, *, topic: str, key: str, payload: dict[str, Any]) -> None:
        errors: list[Exception] = []

        def delivery_callback(error, message) -> None:  # noqa: ANN001
            if error is not None:
                errors.append(RuntimeError(str(error)))
            else:
                logger.info(
                    "Published Kafka message topic=%s partition=%s offset=%s",
                    message.topic(),
                    message.partition(),
                    message.offset(),
                )

        self._producer.produce(
            topic=topic,
            key=key.encode("utf-8"),
            value=dumps(payload),
            callback=delivery_callback,
        )
        self._producer.poll(0)
        self._producer.flush(10)
        if errors:
            raise errors[0]


class KafkaConsumerClient:
    def __init__(self, *, group_id: str, topics: list[str]) -> None:
        try:
            from confluent_kafka import Consumer
        except Exception as exc:  # pragma: no cover - depends on optional runtime package
            raise KafkaUnavailable("confluent-kafka is not installed") from exc
        self._consumer = Consumer(kafka_config(group_id=group_id))
        self._consumer.subscribe(topics)
        self._settings = get_settings()

    def messages(self) -> Iterator[dict[str, Any]]:
        while True:
            message = self._consumer.poll(self._settings.kafka_poll_seconds)
            if message is None:
                continue
            if message.error():
                logger.warning("Kafka consumer error: %s", message.error())
                continue
            yield loads(message.value())
            self._consumer.commit(message=message, asynchronous=False)

    def close(self) -> None:
        self._consumer.close()

