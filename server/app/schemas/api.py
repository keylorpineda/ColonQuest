"""Request and response bodies for the HTTP endpoints."""

import uuid
from typing import Annotated, Literal

from pydantic import Field

from app.schemas.base import ContractModel
from app.schemas.enums import KeyQuestion, TeachingConcept
from app.schemas.game_event import Identifier
from app.schemas.player_state import PlayerState

ClassCode = Annotated[str, Field(pattern=r"^[A-Z0-9]{4,8}$")]


class CreateClassRequest(ContractModel):
    name: Annotated[str, Field(min_length=1, max_length=80)]


class ClassCreated(ContractModel):
    code: ClassCode
    name: str


class ConceptStat(ContractModel):
    concept: TeachingConcept
    decisions: int
    risky_choice_pct: int | None


class ClassDashboard(ContractModel):
    code: ClassCode
    name: str
    students: int
    finished_games: int
    negotiations: int
    trap_detection_pct: int | None
    average_final_debt: int | None
    concepts: list[ConceptStat]
    explanation: str
    source: Literal["model", "fallback"]


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


class NegotiationRequest(ContractModel):
    offer_id: Annotated[str, Field(pattern=r"^[a-z0-9_]{1,40}$")]
    text: Annotated[str, Field(min_length=1, max_length=500)]


class SessionSummary(ContractModel):
    months_played: int
    decisions: int
    concepts_taught: list[TeachingConcept]
    negotiations: int
    deals_accepted: int
    key_questions_asked: list[KeyQuestion]
    total_debt: int
    net_worth: int
    narrative: str
    source: Literal["model", "fallback"]
    # Pre-generated illustration without text; Unity draws the figures above on top of it.
    card_image_key: Literal["card_year_debt_free", "card_year_some_debt", "card_year_heavy_debt"]


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
