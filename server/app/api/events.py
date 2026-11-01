"""Monthly event endpoint backed by the AI game master."""

import logging
import uuid
from collections.abc import Callable
from contextlib import AbstractAsyncContextManager
from typing import Annotated, Any

from fastapi import APIRouter, BackgroundTasks, Depends, Request, Response
from pydantic import ValidationError
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.agents.tools import HistoryItem
from app.api.deps import AppSettings, DbSession
from app.api.sessions import get_active_session
from app.config import Settings
from app.db import Decision, GameSession
from app.db.session import get_sessionmaker
from app.game_master.ledger import DbLedger
from app.game_master.planner import SeasonPlanner
from app.game_master.prompt import PromptContext
from app.game_master.season import (
    PlannedMonth,
    SeasonPlan,
    adjust_plan,
    default_plan,
    is_english,
)
from app.game_master.service import EventGenerator
from app.schemas import GameEvent, PlayerState, TeachingConcept
from app.summary.stats import total_debt

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/sessions", tags=["events"])

SessionFactory = Callable[[], AbstractAsyncContextManager[AsyncSession]]

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


def get_session_factory() -> SessionFactory:
    """Background tasks outlive the request, so they open their own database session."""
    return lambda: get_sessionmaker()()  # resolved when the task runs, not per request


GameMasterDep = Annotated[EventGenerator, Depends(get_game_master)]
SeasonPlannerDep = Annotated[SeasonPlanner | None, Depends(get_season_planner)]
SessionFactoryDep = Annotated[SessionFactory, Depends(get_session_factory)]


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


def dump_plan(plan: SeasonPlan) -> dict[str, Any]:
    return plan.model_dump(mode="json", by_alias=True)


async def plan_month(
    db: AsyncSession, game_session: GameSession, state: PlayerState
) -> tuple[PlannedMonth, SeasonPlan | None]:
    """Store the default plan on the first event, then adjust it to the player's decisions.

    Returns this month's focus and, on the first event, the default plan that the model's plan
    replaces once it arrives in the background.
    """
    plan: SeasonPlan | None = None
    if game_session.season_plan is not None:
        try:
            plan = SeasonPlan.model_validate(game_session.season_plan)
        except ValidationError:
            plan = None
    placeholder: SeasonPlan | None = None
    if plan is None:
        plan = placeholder = default_plan(state)
    else:
        plan = adjust_plan(
            plan,
            state.month,
            await risky_concepts(db, game_session.id),
            english=is_english(state),
        )
    game_session.season_plan = dump_plan(plan)
    return plan.for_month(state.month), placeholder


async def refine_season_plan(
    session_factory: SessionFactory,
    planner: SeasonPlanner,
    settings: Settings,
    session_id: uuid.UUID,
    client_hash: str | None,
    state: PlayerState,
    placeholder: SeasonPlan,
) -> None:
    """Background task: replace the default plan with the model's plan, in force from month 2."""
    try:
        async with session_factory() as db:
            planned = await planner.plan(DbLedger(db, settings, client_hash), session_id, state)
            game_session = await db.get(GameSession, session_id)
            # A plan already adjusted to the player's decisions wins over a late answer.
            if (
                planned.source == "model"
                and game_session is not None
                and game_session.season_plan == dump_plan(placeholder)
            ):
                game_session.season_plan = dump_plan(planned.plan)
            await db.commit()
    except Exception:
        # The player never sees this: the default plan is already stored.
        logger.exception("season planner task failed for session %s", session_id)


@router.post("/{session_id}/event", response_model=GameEvent)
async def next_event(
    session_id: uuid.UUID,
    state: PlayerState,
    response: Response,
    db: DbSession,
    settings: AppSettings,
    game_master: GameMasterDep,
    planner: SeasonPlannerDep,
    background: BackgroundTasks,
    session_factory: SessionFactoryDep,
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
    planned: PlannedMonth | None = None
    if planner is not None:
        planned, placeholder = await plan_month(db, game_session, state)
        if placeholder is not None:
            # Runs after the response is sent: Unity's 8 s never include the planner.
            background.add_task(
                refine_season_plan,
                session_factory,
                planner,
                settings,
                session_id,
                game_session.client_hash,
                state,
                placeholder,
            )
    language_val = (
        (state.language.value if state.language else None) or game_session.language or "es"
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
        language=language_val,
    )
    outcome = await game_master.next_event(ledger, session_id, context)
    await db.commit()
    response.headers["X-Event-Source"] = outcome.source.value
    return outcome.event
