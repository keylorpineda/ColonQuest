import uuid
from datetime import UTC, datetime
from typing import Any

import httpx2
import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.db import Decision, GameSession, SchoolClass

pytestmark = pytest.mark.anyio

STATE: dict[str, Any] = {
    "month": 3,
    "salaryNet": 450000,
    "cash": 35000,
    "savings": 50000,
    "emergencyFund": 0,
    "creditCard": {"balance": 180000, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10},
    "wellbeing": 60,
}


def decision_body(**overrides: Any) -> dict[str, Any]:
    body = {
        "month": 3,
        "eventId": "evt_moto_dañada",
        "teachingConcept": "emergency_fund",
        "choiceId": "pay_card",
        "stateAfter": STATE,
    }
    return body | overrides


async def test_create_session_hashes_the_client_ip(
    api: httpx2.AsyncClient, db: AsyncSession
) -> None:
    response = await api.post(
        "/api/sessions", json={}, headers={"X-Forwarded-For": "203.0.113.9, 10.0.0.1"}
    )

    assert response.status_code == 201
    game_session = await db.get(GameSession, uuid.UUID(response.json()["sessionId"]))
    assert game_session is not None
    assert game_session.client_hash and "203.0.113.9" not in game_session.client_hash


async def test_create_session_with_class_code(api: httpx2.AsyncClient, db: AsyncSession) -> None:
    db.add(SchoolClass(code="UNA2026", name="Finanzas 10-1"))
    await db.flush()

    ok = await api.post("/api/sessions", json={"classCode": "UNA2026"})
    missing = await api.post("/api/sessions", json={"classCode": "NOPE99"})
    invalid = await api.post("/api/sessions", json={"classCode": "bad code"})

    assert ok.status_code == 201
    assert missing.status_code == 404
    assert invalid.status_code == 422


async def test_record_decision(api: httpx2.AsyncClient, db: AsyncSession) -> None:
    session_id = (await api.post("/api/sessions", json={})).json()["sessionId"]

    response = await api.post(f"/api/sessions/{session_id}/decisions", json=decision_body())

    assert response.status_code == 201
    stored = (
        await db.scalars(select(Decision).where(Decision.session_id == uuid.UUID(session_id)))
    ).one()
    assert stored.choice_id == "pay_card"
    assert stored.state_after["creditCard"]["balance"] == 180000


async def test_decision_errors(api: httpx2.AsyncClient, db: AsyncSession) -> None:
    unknown = await api.post(f"/api/sessions/{uuid.uuid4()}/decisions", json=decision_body())
    assert unknown.status_code == 404

    session_id = (await api.post("/api/sessions", json={})).json()["sessionId"]
    mismatch = await api.post(f"/api/sessions/{session_id}/decisions", json=decision_body(month=4))
    assert mismatch.status_code == 422
    float_money = decision_body(stateAfter=STATE | {"cash": 35000.5})
    assert (
        await api.post(f"/api/sessions/{session_id}/decisions", json=float_money)
    ).status_code == 422

    game_session = await db.get(GameSession, uuid.UUID(session_id))
    assert game_session is not None
    game_session.finished_at = datetime.now(UTC)
    await db.flush()
    finished = await api.post(f"/api/sessions/{session_id}/decisions", json=decision_body())
    assert finished.status_code == 409
