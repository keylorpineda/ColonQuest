"""Monthly event produced by the game master and validated before reaching Unity."""

from typing import Annotated, Self

from pydantic import Field, model_validator

from app.schemas.base import ContractModel, SignedColones
from app.schemas.enums import EffectKind, EventType, MoneySource, TeachingConcept

Identifier = Annotated[str, Field(min_length=1, max_length=64, pattern=r"^[a-z0-9_ñ]+$")]

SIGNED_EFFECTS = frozenset({EffectKind.SAVINGS_CHANGE, EffectKind.WELLBEING_CHANGE})
EFFECTS_WITH_MONTHS = frozenset({EffectKind.NEW_INSTALLMENT, EffectKind.NEW_INFORMAL_LOAN})


class Npc(ContractModel):
    id: Identifier
    name: Annotated[str, Field(min_length=1, max_length=40)]
    role: Annotated[str, Field(min_length=1, max_length=40)]


class Effect(ContractModel):
    """A bounded change the finance engine knows how to apply.

    ``amount`` may be negative only for savings_change and wellbeing_change.
    ``months`` is required for new_installment and new_informal_loan.
    """

    kind: EffectKind
    amount: SignedColones
    source: MoneySource | None = None
    months: Annotated[int, Field(ge=1, le=60)] | None = None

    @model_validator(mode="after")
    def check_kind_rules(self) -> Self:
        if self.amount < 0 and self.kind not in SIGNED_EFFECTS:
            raise ValueError(f"amount must be >= 0 for {self.kind}")
        if self.kind == EffectKind.EXPENSE and self.source is None:
            raise ValueError("expense effects need a source")
        if self.kind in EFFECTS_WITH_MONTHS and self.months is None:
            raise ValueError(f"{self.kind} effects need months")
        return self


class Choice(ContractModel):
    id: Identifier
    label: Annotated[str, Field(min_length=1, max_length=60)]
    effects: Annotated[list[Effect], Field(max_length=5)]


class GameEvent(ContractModel):
    event_id: Identifier
    type: EventType
    title: Annotated[str, Field(min_length=1, max_length=80)]
    narrative: Annotated[str, Field(min_length=1, max_length=280)]
    npc: Npc | None = None
    image_key: Identifier | None = None
    choices: Annotated[list[Choice], Field(min_length=2, max_length=4)]
    teaching_concept: TeachingConcept

    @model_validator(mode="after")
    def check_unique_choice_ids(self) -> Self:
        ids = [choice.id for choice in self.choices]
        if len(ids) != len(set(ids)):
            raise ValueError("choice ids must be unique")
        return self
