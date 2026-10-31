"""Game sessions and decision telemetry."""

import uuid

from fastapi import APIRouter, HTTPException, status

from app.api.deps import ClientHash, DbSession
from app.db import Decision, GameSession, SchoolClass
from app.schemas.api import CreateSessionRequest, DecisionRecorded, DecisionRequest, SessionCreated

router = APIRouter(prefix="/sessions", tags=["sessions"])


async def get_active_session(db: DbSession, session_id: uuid.UUID) -> GameSession:
    game_session = await db.get(GameSession, session_id)
    if game_session is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "session not found")
    if game_session.finished_at is not None:
        raise HTTPException(status.HTTP_409_CONFLICT, "session already finished")
    return game_session


@router.post("", status_code=status.HTTP_201_CREATED, response_model=SessionCreated)
async def create_session(
    body: CreateSessionRequest, db: DbSession, hashed_client: ClientHash
) -> SessionCreated:
    """Start a game. A classCode joins the session to a Class Mode class."""
    if body.class_code is not None and await db.get(SchoolClass, body.class_code) is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "class not found")
    game_session = GameSession(
        class_code=body.class_code,
        client_hash=hashed_client,
        language=str(body.language.value if hasattr(body.language, "value") else body.language),
    )
    db.add(game_session)
    await db.commit()
    return SessionCreated(session_id=game_session.id)


@router.post(
    "/{session_id}/decisions",
    status_code=status.HTTP_201_CREATED,
    response_model=DecisionRecorded,
)
async def record_decision(
    session_id: uuid.UUID, body: DecisionRequest, db: DbSession
) -> DecisionRecorded:
    """Store the choice the player made and the resulting state (impact telemetry)."""
    await get_active_session(db, session_id)
    if body.state_after.month != body.month:
        raise HTTPException(status.HTTP_422_UNPROCESSABLE_CONTENT, "stateAfter.month must match")
    decision = Decision(
        session_id=session_id,
        month=body.month,
        event_id=body.event_id,
        teaching_concept=body.teaching_concept.value,
        choice_id=body.choice_id,
        state_after=body.state_after.model_dump(mode="json"),
    )
    db.add(decision)
    await db.commit()
    return DecisionRecorded(decision_id=decision.id)
