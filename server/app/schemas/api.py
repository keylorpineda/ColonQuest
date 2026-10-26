"""Request and response bodies for the HTTP endpoints."""

import uuid
from typing import Annotated, Literal

from pydantic import Field

from app.schemas.base import ContractModel
from app.schemas.enums import TeachingConcept
from app.schemas.game_event import Identifier
from app.schemas.player_state import PlayerState

ClassCode = Annotated[str, Field(pattern=r"^[A-Z0-9]{4,8}$")]


class CreateSessionRequest(ContractModel):
    class_code: ClassCode | None = None


class SessionCreated(ContractModel):
    session_id: uuid.UUID


class DecisionRequest(ContractModel):
    month: Annotated[int, Field(ge=1, le=12)]
    event_id: Identifier
    teaching_concept: TeachingConcept
    choice_id: Identifier
    state_after: PlayerState


class DecisionRecorded(ContractModel):
    decision_id: int


class BudgetInfo(ContractModel):
    spent_usd: float
    budget_usd: float
    remaining_usd: float
    mode: Literal["normal", "downgraded", "fallback_only"]


class HealthResponse(ContractModel):
    status: Literal["ok"]
    version: str
    database: Literal["ok", "unavailable", "not_configured"]
    budget: BudgetInfo | None = None
