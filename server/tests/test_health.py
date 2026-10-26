import pytest
from fastapi.testclient import TestClient

from app.main import app

from .conftest import requires_db


def test_health_without_database() -> None:
    response = TestClient(app).get("/api/health")

    assert response.status_code == 200
    assert response.json() == {
        "status": "ok",
        "version": "0.1.0",
        "database": "not_configured",
        "budget": None,
    }


@requires_db
@pytest.mark.anyio
async def test_health_reports_budget(api) -> None:  # type: ignore[no-untyped-def]
    body = (await api.get("/api/health")).json()

    assert body["database"] == "ok"
    assert body["budget"]["mode"] == "normal"
    assert body["budget"]["remainingUsd"] == body["budget"]["budgetUsd"]
