"""Health check: service status, database reachability and remaining daily budget."""

from fastapi import APIRouter
from sqlalchemy.exc import SQLAlchemyError

from app.api.deps import AppSettings, OptionalDbSession
from app.cost_guard import budget_status
from app.schemas.api import BudgetInfo, HealthResponse

router = APIRouter(tags=["health"])

VERSION = "0.1.0"


@router.get("/health", response_model=HealthResponse)
async def get_health(db: OptionalDbSession, settings: AppSettings) -> HealthResponse:
    """Always answers 200 so uptime checks see the process; the body says what is degraded."""
    if db is None:
        return HealthResponse(status="ok", version=VERSION, database="not_configured")
    try:
        status = await budget_status(db, settings.daily_budget_usd)
    except (SQLAlchemyError, OSError):
        return HealthResponse(status="ok", version=VERSION, database="unavailable")
    return HealthResponse(
        status="ok",
        version=VERSION,
        database="ok",
        budget=BudgetInfo(
            spent_usd=float(status.spent_usd),
            budget_usd=float(status.budget_usd),
            remaining_usd=float(status.remaining_usd),
            mode=status.mode.value,
        ),
    )
