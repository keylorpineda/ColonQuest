"""Health check and ping: service status, database reachability and remaining daily budgets."""

from fastapi import APIRouter, Response
from sqlalchemy.exc import SQLAlchemyError

from app.api.deps import AppSettings, OptionalDbSession
from app.cost_guard import OPEN_WEIGHTS_PROVIDER, BudgetStatus, budget_status
from app.schemas.api import BudgetInfo, HealthResponse, PingResponse

router = APIRouter(tags=["health"])

VERSION = "0.1.0"


def budget_info(status: BudgetStatus) -> BudgetInfo:
    return BudgetInfo(
        spent_usd=float(status.spent_usd),
        budget_usd=float(status.budget_usd),
        remaining_usd=float(status.remaining_usd),
        mode=status.mode.value,
    )


@router.get("/ping", response_model=PingResponse)
async def ping() -> PingResponse:
    """Keep-alive for UptimeRobot: wakes Render without touching Neon or any model."""
    return PingResponse(status="ok")


# UptimeRobot checks with HEAD by default and a GET-only route answers 405 Method Not Allowed.
# Separate HEAD routes stay out of the OpenAPI contract (one operation per path) and never touch
# Neon or the models, so a monitor can keep Render awake for free.
@router.head("/ping", include_in_schema=False)
@router.head("/health", include_in_schema=False)
async def head_check() -> Response:
    return Response(status_code=200)


@router.get("/health", response_model=HealthResponse)
async def get_health(db: OptionalDbSession, settings: AppSettings) -> HealthResponse:
    """Always answers 200 so uptime checks see the process; the body says what is degraded."""
    if db is None:
        return HealthResponse(status="ok", version=VERSION, database="not_configured")
    try:
        status = await budget_status(db, settings.daily_budget_usd)
        open_weights = await budget_status(
            db, settings.open_weights_daily_budget_usd, OPEN_WEIGHTS_PROVIDER
        )
    except (SQLAlchemyError, OSError):
        return HealthResponse(status="ok", version=VERSION, database="unavailable")
    return HealthResponse(
        status="ok",
        version=VERSION,
        database="ok",
        budget=budget_info(status),
        open_weights_budget=budget_info(open_weights),
    )
