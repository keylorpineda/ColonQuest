"""Negotiation endpoints: typed text and voice share the same turn logic."""

import time
import uuid
from typing import Annotated

from fastapi import (
    APIRouter,
    Depends,
    File,
    Form,
    HTTPException,
    Request,
    Response,
    UploadFile,
    status,
)
from sqlalchemy import select

from app.api.deps import AppSettings, DbSession
from app.api.sessions import get_active_session
from app.cost_guard import BudgetMode
from app.db import Negotiation
from app.game_master.ledger import DbLedger
from app.negotiation.audio import MAX_BYTES, AudioError, normalize_wav
from app.negotiation.seller_sheet import get_offer
from app.negotiation.service import NegotiationResponder, PreviousTurn
from app.providers.base import ProviderError, Transcriber, Usage
from app.schemas import NegotiationReply
from app.schemas.api import NegotiationRequest

router = APIRouter(prefix="/sessions", tags=["negotiation"])

VOICE_UNAVAILABLE = "voice is unavailable right now, use text input"


def get_negotiator(request: Request) -> NegotiationResponder:
    negotiator: NegotiationResponder = request.app.state.negotiator
    return negotiator


def get_transcriber(request: Request) -> Transcriber:
    transcriber: Transcriber = request.app.state.provider
    return transcriber


NegotiatorDep = Annotated[NegotiationResponder, Depends(get_negotiator)]
TranscriberDep = Annotated[Transcriber, Depends(get_transcriber)]


async def run_turn(
    db: DbSession,
    settings: AppSettings,
    negotiator: NegotiationResponder,
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


@router.post("/{session_id}/negotiation/voice", response_model=NegotiationReply)
async def negotiate_voice(
    session_id: uuid.UUID,
    response: Response,
    db: DbSession,
    settings: AppSettings,
    negotiator: NegotiatorDep,
    transcriber: TranscriberDep,
    offer_id: Annotated[str, Form(alias="offerId", pattern=r"^[a-z0-9_]{1,40}$")],
    audio: Annotated[UploadFile, File(description="WAV clip, 16-bit PCM, up to 15 s")],
) -> NegotiationReply:
    """One negotiation turn spoken by the player.

    Validation happens before paying for transcription. If voice fails the response is 503 and
    the client switches to the text endpoint, so the negotiation never blocks.
    """
    game_session = await get_active_session(db, session_id)
    if get_offer(offer_id) is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "offer not found")
    try:
        wav, _ = normalize_wav(await audio.read(MAX_BYTES + 1))
    except AudioError as error:
        raise HTTPException(status.HTTP_422_UNPROCESSABLE_CONTENT, str(error)) from error

    ledger = DbLedger(db, settings, game_session.client_hash)
    if await ledger.budget_mode() == BudgetMode.FALLBACK_ONLY:
        raise HTTPException(status.HTTP_503_SERVICE_UNAVAILABLE, VOICE_UNAVAILABLE)

    started = time.perf_counter()
    try:
        transcription = await transcriber.transcribe(wav)
    except ProviderError as error:
        await ledger.record(
            usage=Usage(model=settings.transcribe_model),
            endpoint="transcribe",
            latency_ms=int((time.perf_counter() - started) * 1000),
            ok=False,
            session_id=session_id,
        )
        await db.commit()
        raise HTTPException(status.HTTP_503_SERVICE_UNAVAILABLE, VOICE_UNAVAILABLE) from error
    await ledger.record(
        usage=transcription.usage,
        endpoint="transcribe",
        latency_ms=transcription.latency_ms,
        ok=True,
        session_id=session_id,
    )
    if not transcription.text:
        await db.commit()
        raise HTTPException(status.HTTP_422_UNPROCESSABLE_CONTENT, "no speech detected")

    return await run_turn(
        db, settings, negotiator, response, session_id, offer_id, transcription.text
    )
