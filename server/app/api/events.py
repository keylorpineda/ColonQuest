"""Monthly event endpoint backed by the AI game master."""

import uuid
from typing import Annotated

from fastapi import APIRouter, Depends, Request, Response
from sqlalchemy import select

from app.agents.tools import HistoryItem
from app.api.deps import AppSettings, DbSession
from app.api.sessions import get_active_session
from app.db import Decision
from app.game_master.ledger import DbLedger
from app.game_master.prompt import PromptContext
from app.game_master.service import EventGenerator
from app.schemas import GameEvent, PlayerState, TeachingConcept

router = APIRouter(prefix="/sessions", tags=["events"])

RECENT_EVENTS = 6


def get_game_master(request: Request) -> EventGenerator:
    game_master: EventGenerator = request.app.state.game_master
    return game_master


GameMasterDep = Annotated[EventGenerator, Depends(get_game_master)]


@router.post("/{session_id}/event", response_model=GameEvent)
async def next_event(
    session_id: uuid.UUID,
    state: PlayerState,
    response: Response,
    db: DbSession,
    settings: AppSettings,
    game_master: GameMasterDep,
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
    context = PromptContext(
        state=state,
        recent_event_ids=[row.event_id for row in rows[-RECENT_EVENTS:]],
        taught_concepts=[TeachingConcept(row.teaching_concept) for row in rows],
        history=[
            HistoryItem(row.month, row.event_id, row.teaching_concept, row.choice_id)
            for row in rows
        ],
    )
    ledger = DbLedger(db, settings, game_session.client_hash)
    outcome = await game_master.next_event(ledger, session_id, context)
    await db.commit()
    response.headers["X-Event-Source"] = outcome.source.value
    return outcome.event
