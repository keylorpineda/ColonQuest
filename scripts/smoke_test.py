"""End-to-end smoke test of the game flow over HTTP (local or deployed backend).

Usage:
    python scripts/smoke_test.py                       # http://127.0.0.1:8000
    API_URL=https://colon-quest-api.onrender.com python scripts/smoke_test.py

It plays one short game: class -> session -> event -> decision -> event -> negotiation by text
and voice -> summary -> dashboard, and checks the contract of every response. It works with or
without a Meta API key: without one, every model call must fall back cleanly.
Exit code 0 when every check passes.
"""

import io
import json
import math
import os
import sys
import urllib.error
import urllib.request
import uuid
import wave
from array import array
from typing import Any

API_URL = os.environ.get("API_URL", "http://127.0.0.1:8000").rstrip("/")
TIMEOUT_S = 30

STATE: dict[str, Any] = {
    "month": 1,
    "salaryNet": 450000,
    "cash": 120000,
    "savings": 50000,
    "emergencyFund": 0,
    "creditCard": {"balance": 0, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10},
    "installments": [],
    "informalLoans": [],
    "history": [],
    "wellbeing": 60,
}

failures: list[str] = []


def request(
    method: str,
    path: str,
    body: dict[str, Any] | None = None,
    raw: bytes | None = None,
    content_type: str = "application/json",
) -> tuple[int, dict[str, str], Any]:
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    req = urllib.request.Request(  # noqa: S310 - URL comes from the operator's environment
        API_URL + path, data=data, method=method, headers={"Content-Type": content_type}
    )
    try:
        with urllib.request.urlopen(req, timeout=TIMEOUT_S) as response:  # noqa: S310
            return response.status, dict(response.headers), json.load(response)
    except urllib.error.HTTPError as error:
        return error.code, dict(error.headers), json.loads(error.read() or b"null")


def request_bytes(path: str) -> tuple[int, dict[str, str], bytes]:
    req = urllib.request.Request(  # noqa: S310 - URL comes from the operator's environment
        API_URL + path, method="GET"
    )
    try:
        with urllib.request.urlopen(req, timeout=TIMEOUT_S) as response:  # noqa: S310
            return response.status, dict(response.headers), response.read()
    except urllib.error.HTTPError as error:
        return error.code, dict(error.headers), error.read()


def check(name: str, condition: bool, detail: object = "") -> None:
    print(f"[{'PASS' if condition else 'FAIL'}] {name}" + (f" -> {detail}" if not condition else ""))
    if not condition:
        failures.append(name)


def wav_48k_stereo(seconds: float = 1.0) -> bytes:
    rate = 48_000
    tone = [int(6000 * math.sin(2 * math.pi * 220 * i / rate)) for i in range(int(rate * seconds))]
    output = io.BytesIO()
    with wave.open(output, "wb") as clip:
        clip.setnchannels(2)
        clip.setsampwidth(2)
        clip.setframerate(rate)
        clip.writeframes(array("h", [s for s in tone for _ in range(2)]).tobytes())
    return output.getvalue()


def multipart(fields: dict[str, str], file_name: str, file_bytes: bytes) -> tuple[bytes, str]:
    boundary = uuid.uuid4().hex
    parts = [
        f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode()
        for key, value in fields.items()
    ]
    parts.append(
        f'--{boundary}\r\nContent-Disposition: form-data; name="audio"; filename="{file_name}"'
        f"\r\nContent-Type: audio/wav\r\n\r\n".encode()
        + file_bytes
        + b"\r\n"
    )
    parts.append(f"--{boundary}--\r\n".encode())
    return b"".join(parts), f"multipart/form-data; boundary={boundary}"


def main() -> int:
    print(f"Smoke test against {API_URL}\n")

    status, _, health = request("GET", "/api/health")
    check("health is ok", status == 200 and health["status"] == "ok", health)
    check("database reachable", health.get("database") == "ok", health)

    status, _, created_class = request("POST", "/api/classes", {"name": "Smoke test"})
    check("create class", status == 201 and len(created_class.get("code", "")) == 6, created_class)
    code = created_class.get("code", "")

    status, _, session = request("POST", "/api/sessions", {"classCode": code})
    check("create session in class", status == 201 and "sessionId" in session, session)
    session_id = session.get("sessionId", "")
    base = f"/api/sessions/{session_id}"

    status, headers, event = request("POST", f"{base}/event", STATE)
    source = headers.get("X-Event-Source") or headers.get("x-event-source")
    check("first event", status == 200 and 2 <= len(event.get("choices", [])) <= 4, event)
    check("event source header", source in {"model", "cache", "fallback", "agent"}, source)
    choice = event["choices"][0] if status == 200 else {"id": "x"}

    decision = {
        "month": 1,
        "eventId": event.get("eventId", "evt_x"),
        "teachingConcept": event.get("teachingConcept", "budget"),
        "choiceId": choice["id"],
        "stateAfter": STATE,
    }
    status, _, recorded = request("POST", f"{base}/decisions", decision)
    check("record decision", status == 201 and "decisionId" in recorded, recorded)

    month_two = STATE | {"month": 2}
    status, _, second = request("POST", f"{base}/event", month_two)
    check("second event", status == 200, second)
    check(
        "second event avoids the first",
        second.get("eventId") != event.get("eventId"),
        (event.get("eventId"), second.get("eventId")),
    )

    status, headers, reply = request(
        "POST",
        f"{base}/negotiation",
        {"offerId": "celular_gama_alta", "text": "¿Cuánto pago en total con el seguro?"},
    )
    check("text negotiation", status == 200 and reply.get("npcReply"), reply)
    check(
        "key questions detected",
        set(reply.get("keyQuestionsAsked", [])) >= {"total_cost", "insurance"},
        reply.get("keyQuestionsAsked"),
    )

    body, content_type = multipart({"offerId": "celular_gama_alta"}, "clip.wav", wav_48k_stereo())
    status, _, voice = request(
        "POST", f"{base}/negotiation/voice", raw=body, content_type=content_type
    )
    check(
        "voice negotiation answers or asks for text",
        status == 200 or (status in {422, 503} and "detail" in voice),
        (status, voice),
    )

    ogg_body, ogg_type = multipart({"offerId": "celular_gama_alta"}, "x.ogg", b"OggS")
    status, _, bad_audio = request(
        "POST", f"{base}/negotiation/voice", raw=ogg_body, content_type=ogg_type
    )
    check("non-WAV audio rejected", status == 422, (status, bad_audio))

    final_state = STATE | {"month": 12, "cash": 90000}
    status, _, summary = request("POST", f"{base}/summary", final_state)
    check("summary", status == 200 and summary.get("narrative"), summary)
    check("summary net worth", summary.get("netWorth") == 140000, summary.get("netWorth"))

    status, _, closed = request("POST", f"{base}/event", month_two)
    check("finished session rejects new events", status == 409, (status, closed))

    status, _, dashboard = request("GET", f"/api/classes/{code}/dashboard")
    check("dashboard", status == 200, dashboard)
    check(
        "dashboard counts",
        (dashboard.get("students"), dashboard.get("finishedGames")) == (1, 1),
        dashboard,
    )
    check("dashboard explanation", bool(dashboard.get("explanation")), dashboard)

    status, _, invalid = request("POST", f"{base}/event", STATE | {"cash": 10.5})
    check("float money rejected", status in {409, 422}, status)

    img_status, img_headers, img_bytes = request_bytes("/api/images/scene_taller_mecanico")
    img_source = img_headers.get("X-Image-Source") or img_headers.get("x-image-source")
    check(
        "cached image endpoint returns PNG",
        img_status == 200 and img_bytes.startswith(b"\x89PNG\r\n\x1a\n"),
        img_status,
    )
    check(
        "cached image source header",
        img_source in {"model", "cache", "fallback"},
        img_source,
    )

    print(f"\n{len(failures)} failed" if failures else "\nAll checks passed")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
