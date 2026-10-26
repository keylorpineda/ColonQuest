"""End-of-game summary endpoint. Closes the session and stores the final state."""

import uuid
from datetime import UTC, datetime
from typing import Annotated

from fastapi import APIRouter, Depends, Request
from sqlalchemy import select

from app.api.deps import AppSettings, DbSession
from app.api.sessions import get_active_session
from app.db import Decision, Negotiation
from app.game_master.ledger import DbLedger
from app.schemas import PlayerState
from app.schemas.api import SessionSummary
from app.summary.stats import NegotiationRecord, build_stats
from app.summary.writer import SummaryWriter

router = APIRouter(prefix="/sessions", tags=["summary"])


def get_summary_writer(request: Request) -> SummaryWriter:
    writer: SummaryWriter = request.app.state.summary_writer
    return writer


SummaryWriterDep = Annotated[SummaryWriter, Depends(get_summary_writer)]


@router.post("/{session_id}/summary", response_model=SessionSummary)
async def finish_session(
    session_id: uuid.UUID,
    state: PlayerState,
    db: DbSession,
    settings: AppSettings,
    writer: SummaryWriterDep,
) -> SessionSummary:
    """Compute the final figures, write the narrative and close the session (409 afterwards)."""
    game_session = await get_active_session(db, session_id)
    concepts = (
        await db.scalars(
            select(Decision.teaching_concept)
            .where(Decision.session_id == session_id)
            .order_by(Decision.id)
        )
    ).all()
    negotiations = (
        await db.execute(
            select(Negotiation.key_questions_asked, Negotiation.deal_status).where(
                Negotiation.session_id == session_id
            )
        )
    ).all()
    stats = build_stats(
        state,
        decision_concepts=list(concepts),
        negotiations=[NegotiationRecord(row[0], row[1]) for row in negotiations],
    )
    ledger = DbLedger(db, settings, game_session.client_hash)
    written = await writer.write(ledger, session_id, stats)

    game_session.finished_at = datetime.now(UTC)
    game_session.final_state = state.model_dump(mode="json")
    await db.commit()
    return SessionSummary(
        months_played=stats.months_played,
        decisions=stats.decisions,
        concepts_taught=stats.concepts_taught,
        negotiations=stats.negotiations,
        deals_accepted=stats.deals_accepted,
        key_questions_asked=stats.key_questions_asked,
        total_debt=stats.total_debt,
        net_worth=stats.net_worth,
        narrative=written.narrative,
        source="model" if written.source == "model" else "fallback",
    )
