"""One turn of a talk with the collectors, the lender or the pyramid stand."""

import uuid
from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, Request, status

from app.api.deps import AppSettings, DbSession
from app.api.sessions import get_active_session
from app.game_master.ledger import DbLedger
from app.schemas.talk import TalkReply, TalkRequest
from app.talks.service import TalkResponder, TalkUnavailableError

router = APIRouter(prefix="/sessions", tags=["talk"])


def get_talker(request: Request) -> TalkResponder:
    talker: TalkResponder = request.app.state.talker
    return talker


TalkerDep = Annotated[TalkResponder, Depends(get_talker)]


@router.post("/{session_id}/talk", response_model=TalkReply)
async def talk(
    session_id: uuid.UUID,
    body: TalkRequest,
    db: DbSession,
    settings: AppSettings,
    talker: TalkerDep,
) -> TalkReply:
    """The character's words, from the true sheet sent to it. 503 means: use the local rules."""
    game_session = await get_active_session(db, session_id)
    ledger = DbLedger(db, settings, game_session.client_hash)
    try:
        reply = await talker.reply(ledger, session_id, body)
    except TalkUnavailableError as error:
        await db.commit()  # the calls that failed were still paid for
        raise HTTPException(status.HTTP_503_SERVICE_UNAVAILABLE, "use the local rules") from error
    await db.commit()
    return reply
