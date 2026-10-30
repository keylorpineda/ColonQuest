"""Player financial state, computed by the Unity finance engine and sent to the backend."""

from typing import Annotated

from pydantic import Field, field_validator

from app.schemas.base import Colones, ContractModel, Percent

ShortText = Annotated[str, Field(min_length=1, max_length=60)]


class CreditCard(ContractModel):
    balance: Colones
    limit: Colones
    monthly_rate_pct: Percent
    min_payment_pct: Percent


class Installment(ContractModel):
    item: ShortText
    monthly: Colones
    months_left: Annotated[int, Field(ge=0, le=120)]


class InformalLoan(ContractModel):
    lender: ShortText
    balance: Colones
    monthly_rate_pct: Percent


class PlayerState(ContractModel):
    month: Annotated[int, Field(ge=1, le=12)]
    salary_net: Colones
    cash: Colones
    savings: Colones
    emergency_fund: Colones
    credit_card: CreditCard
    installments: Annotated[list[Installment], Field(default_factory=list, max_length=20)]
    informal_loans: Annotated[list[InformalLoan], Field(default_factory=list, max_length=10)]
    history: Annotated[list[ShortText], Field(default_factory=list, max_length=50)]
    wellbeing: Annotated[int, Field(ge=0, le=100)]
    character_name: Annotated[ShortText | None, Field(default=None)] = None
    week: Annotated[int | None, Field(default=None, ge=1, le=4)] = None
    job_position: Annotated[ShortText | None, Field(default=None)] = None

    @field_validator("character_name", "job_position", mode="before")
    @classmethod
    def _empty_str_to_none(cls, v: object) -> object:
        if isinstance(v, str) and not v.strip():
            return None
        return v

    @field_validator("week", mode="before")
    @classmethod
    def _zero_to_none(cls, v: object) -> object:
        if v == 0 or v == "0":
            return None
        return v
