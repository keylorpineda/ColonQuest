"""Negotiation endpoint (text). Voice input is added on top of the same flow."""

import uuid
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, Request, Response, status
from sqlalchemy import select

from app.api.deps import AppSettings, DbSession
from app.api.sessions import get_active_session
from app.db import Negotiation
from app.game_master.ledger import DbLedger
from app.negotiation.seller_sheet import get_offer
from app.negotiation.service import Negotiator, PreviousTurn
from app.schemas import NegotiationReply
from app.schemas.api import NegotiationRequest

router = APIRouter(prefix="/sessions", tags=["negotiation"])


def get_negotiator(request: Request) -> Negotiator:
    negotiator: Negotiator = request.app.state.negotiator
    return negotiator


NegotiatorDep = Annotated[Negotiator, Depends(get_negotiator)]


async def run_turn(
    db: DbSession,
    settings: AppSettings,
    negotiator: Negotiator,
    response: Response,
    session_id: uuid.UUID,
    offer_id: str,
    player_text: str,
) -> NegotiationReply:
    game_session = await get_active_session(db, session_id)
    offer = get_offer(offer_id)
    if offer is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "offer not found")

    previous = (
        await db.scalars(
            select(Negotiation)
            .where(Negotiation.session_id == session_id, Negotiation.offer_id == offer_id)
            .order_by(Negotiation.id)
        )
    ).all()
    history = [PreviousTurn(player=row.transcript, seller=row.npc_reply) for row in previous]

    ledger = DbLedger(db, settings, game_session.client_hash)
    outcome = await negotiator.reply(ledger, session_id, offer, player_text, history)
    reply = outcome.reply
    db.add(
        Negotiation(
            session_id=session_id,
            offer_id=offer_id,
            transcript=reply.transcript,
            npc_reply=reply.npc_reply,
            key_questions_asked=[q.value for q in reply.key_questions_asked],
            revealed_facts=[f.value for f in reply.revealed_facts],
            deal_status=reply.deal_status.value,
            source=outcome.source,
        )
    )
    await db.commit()
    response.headers["X-Reply-Source"] = outcome.source
    return reply


@router.post("/{session_id}/negotiation", response_model=NegotiationReply)
async def negotiate_text(
    session_id: uuid.UUID,
    body: NegotiationRequest,
    response: Response,
    db: DbSession,
    settings: AppSettings,
    negotiator: NegotiatorDep,
) -> NegotiationReply:
    """One negotiation turn with typed text (always available as the fallback to voice)."""
    return await run_turn(db, settings, negotiator, response, session_id, body.offer_id, body.text)
