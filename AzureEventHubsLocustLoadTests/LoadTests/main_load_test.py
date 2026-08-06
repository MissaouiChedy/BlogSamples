"""The main load test publishes events to the `main-topic` Event Hub.

Each simulated Locust user owns a dedicated `EventHubProducerClient` created in
`on_start`, authenticated with `DefaultAzureCredential`. Every task iteration
sends a single event that conforms to `main-topic-event.schema.json`.
"""

import json
import os
import random
import time
import uuid
from datetime import datetime, timezone

from azure.eventhub import EventData, EventHubProducerClient
from azure.identity import DefaultAzureCredential
from locust import User, between, events, task

# Event Hub target can be overridden by environment variables for cloud runs.
EVENTHUB_FULLY_QUALIFIED_NAMESPACE = os.environ.get(
    "EVENTHUB_FULLY_QUALIFIED_NAMESPACE",
    "evh-test-main-eventhub-ns-463e.servicebus.windows.net",
)
EVENTHUB_NAME = os.environ.get("EVENTHUB_NAME", "main-topic")

REQUEST_TYPE = "EventHub"
REQUEST_NAME = f"send {EVENTHUB_NAME}"
AREA_CODES = ("EUROPE", "AMERICA", "AFRICA", "ASIA", "AUSTRALIA", "OTHER")


def _to_utc_millis(value: datetime) -> str:
    """Format a datetime as a UTC timestamp with millisecond precision (Z suffix)."""
    return value.strftime("%Y-%m-%dT%H:%M:%S.") + f"{value.microsecond // 1000:03d}Z"


def build_event() -> dict:
    """Build a payload matching main-topic-event.schema.json."""
    created_at = datetime.now(timezone.utc)
    sent_at = datetime.now(timezone.utc)
    area_code = random.choice(AREA_CODES)
    return {
        "Id": str(uuid.uuid4()),
        "Content": random.randint(1, 1000),
        "AreaCode": area_code,
        "CreatedAt": _to_utc_millis(created_at),
        "SentAt": _to_utc_millis(sent_at),
    }


class EventHubUser(User):
    """A virtual user that streams events to the Event Hub."""

    wait_time = between(0.1, 0.5)

    def on_start(self) -> None:
        # Each virtual user gets its own credential + producer client.
        self._credential = DefaultAzureCredential()
        self._producer = EventHubProducerClient(
            fully_qualified_namespace=EVENTHUB_FULLY_QUALIFIED_NAMESPACE,
            eventhub_name=EVENTHUB_NAME,
            credential=self._credential,
        )

    def on_stop(self) -> None:
        try:
            self._producer.close()
        finally:
            self._credential.close()

    @task
    def send_event(self) -> None:
        payload = build_event()
        body = json.dumps(payload)
        start = time.perf_counter()
        exception = None
        try:
            batch = self._producer.create_batch(partition_key=payload["AreaCode"])
            batch.add(EventData(body))
            self._producer.send_batch(batch)
        except Exception as exc:
            # report any failure to Locust
            exception = exc

        # Since Event Hub is asynchronous,
        # we report the time taken to create and send the batch to locust.
        events.request.fire(
            request_type=REQUEST_TYPE,
            name=REQUEST_NAME,
            response_time=(time.perf_counter() - start) * 1000,
            response_length=0,
            exception=exception,
        )
