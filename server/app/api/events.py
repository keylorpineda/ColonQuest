"""Monthly event endpoint backed by the AI game master."""

import uuid
from typing import Annotated

from fastapi import APIRouter, Depends, Request, Response
from pydantic import ValidationError
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.agents.tools import HistoryItem
from app.api.deps import AppSettings, DbSession
from app.api.sessions import get_active_session
from app.db import Decision, GameSession
from app.game_master.ledger import DbLedger, Ledger
from app.game_master.planner import SeasonPlanner
from app.game_master.prompt import PromptContext
from app.game_master.season import PlannedMonth, SeasonPlan, adjust_plan
from app.game_master.service import EventGenerator
from app.schemas import GameEvent, PlayerState, TeachingConcept
from app.summary.stats import total_debt

router = APIRouter(prefix="/sessions", tags=["events"])

RECENT_EVENTS = 6


def get_game_master(request: Request) -> EventGenerator:
    game_master: EventGenerator = request.app.state.game_master
    return game_master


def get_season_planner(request: Request, settings: AppSettings) -> SeasonPlanner | None:
    """The planner only when SEASON_PLANNER is on; otherwise events are planned month by month."""
    if not settings.season_planner:
        return None
    planner: SeasonPlanner = request.app.state.season_planner
    return planner


GameMasterDep = Annotated[EventGenerator, Depends(get_game_master)]
SeasonPlannerDep = Annotated[SeasonPlanner | None, Depends(get_season_planner)]


async def risky_concepts(db: AsyncSession, session_id: uuid.UUID) -> list[TeachingConcept]:
    """The last decision's concept when that decision left the player with more debt."""
    rows = (
        await db.execute(
            select(Decision.teaching_concept, Decision.state_after)
            .where(Decision.session_id == session_id)
            .order_by(Decision.id.desc())
            .limit(2)
        )
    ).all()
    if len(rows) < 2:
        return []
    latest, previous = rows
    try:
        debt_now = total_debt(PlayerState.model_validate(latest.state_after))
        debt_before = total_debt(PlayerState.model_validate(previous.state_after))
    except ValidationError:
        return []
    if debt_now <= debt_before:
        return []
    return [TeachingConcept(latest.teaching_concept)]


async def plan_month(
    db: AsyncSession,
    ledger: Ledger,
    planner: SeasonPlanner,
    game_session: GameSession,
    state: PlayerState,
) -> PlannedMonth:
    """Create the season plan on the first event, then adjust it to the player's decisions."""
    plan: SeasonPlan | None = None
    if game_session.season_plan is not None:
        try:
            plan = SeasonPlan.model_validate(game_session.season_plan)
        except ValidationError:
            plan = None
    if plan is None:
        plan = (await planner.plan(ledger, game_session.id, state)).plan
    else:
        plan = adjust_plan(plan, state.month, await risky_concepts(db, game_session.id))
    game_session.season_plan = plan.model_dump(mode="json", by_alias=True)
    return plan.for_month(state.month)


@router.post("/{session_id}/event", response_model=GameEvent)
async def next_event(
    session_id: uuid.UUID,
    state: PlayerState,
    response: Response,
    db: DbSession,
    settings: AppSettings,
    game_master: GameMasterDep,
    planner: SeasonPlannerDep,
) -> GameEvent:
    """Return this month's event. Never fails because of the model: it falls back instead.

    The ``X-Event-Source`` header says whether the event came from the model, the cache or the
    hand-written fallback catalog.
    """
    game_session = await get_active_session(db, session_id)
    rows = (
        await db.execute(
            select(Decision.month, Decision.event_id, Decision.teaching_concept, Decision.choice_id)
            .where(Decision.session_id == session_id)
            .order_by(Decision.id)
        )
    ).all()
    ledger = DbLedger(db, settings, game_session.client_hash)
    planned = (
        await plan_month(db, ledger, planner, game_session, state) if planner is not None else None
    )
    context = PromptContext(
        state=state,
        recent_event_ids=[row.event_id for row in rows[-RECENT_EVENTS:]],
        taught_concepts=[TeachingConcept(row.teaching_concept) for row in rows],
        history=[
            HistoryItem(row.month, row.event_id, row.teaching_concept, row.choice_id)
            for row in rows
        ],
        planned=planned,
    )
    outcome = await game_master.next_event(ledger, session_id, context)
    await db.commit()
    response.headers["X-Event-Source"] = outcome.source.value
    return outcome.event
