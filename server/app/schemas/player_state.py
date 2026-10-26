"""Player financial state, computed by the Unity finance engine and sent to the backend."""

from typing import Annotated

from pydantic import Field

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
