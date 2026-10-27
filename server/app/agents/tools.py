"""Deterministic tools the agents may call.

Tools never write the player's state: they only read the context they are given or compute
figures. All money is integer colones computed with Decimal, so a figure the model quotes from a
tool result is exact.
"""

import json
from collections.abc import Callable, Iterable
from dataclasses import dataclass, field
from decimal import ROUND_HALF_UP, Decimal
from typing import Annotated, Any

from pydantic import BaseModel, ConfigDict, Field, ValidationError

from app.negotiation.seller_sheet import Offer
from app.providers.base import ToolCall
from app.schemas import TeachingConcept

Money = Annotated[int, Field(ge=0, le=100_000_000)]
RatePct = Annotated[float, Field(ge=0, le=25)]
Months = Annotated[int, Field(ge=1, le=60)]


class ToolArgs(BaseModel):
    model_config = ConfigDict(extra="forbid")


@dataclass(frozen=True, slots=True)
class HistoryItem:
    month: int
    event_id: str
    teaching_concept: str
    choice_id: str


@dataclass(frozen=True, slots=True)
class ToolContext:
    """Read-only data a tool may look at; built by the endpoint from stored telemetry."""

    history: list[HistoryItem] = field(default_factory=list)
    taught: list[TeachingConcept] = field(default_factory=list)


class SimulatePurchaseArgs(ToolArgs):
    price: Money = Field(description="Cash price in colones")
    months: Months = Field(description="Number of monthly installments")
    monthly_rate_pct: RatePct = Field(description="Monthly interest rate, percent")
    insurance_monthly: Money = Field(default=0, description="Monthly insurance fee")
    commission: Money = Field(default=0, description="One-time opening commission")


class ProjectDebtArgs(ToolArgs):
    balance: Money = Field(description="Current debt in colones")
    monthly_rate_pct: RatePct = Field(description="Monthly interest rate, percent")
    monthly_payment: Money = Field(description="Payment made every month")
    months: Months = Field(description="Months to project")


class NoArgs(ToolArgs):
    pass


def simulate_purchase(args: SimulatePurchaseArgs) -> dict[str, Any]:
    offer = Offer(
        offer_id="simulation",
        item="simulation",
        seller_name="",
        cash_price=args.price,
        term_months=args.months,
        monthly_rate_pct=Decimal(str(args.monthly_rate_pct)),
        insurance_monthly=args.insurance_monthly,
        commission=args.commission,
    )
    financed = offer.monthly_payment * args.months
    return {
        "monthlyPayment": offer.monthly_payment,
        "monthlyWithInsurance": offer.monthly_payment + args.insurance_monthly,
        "totalCost": offer.total_cost,
        "totalInterest": financed - args.price,
        "totalInsurance": args.insurance_monthly * args.months,
        "overCashPrice": offer.total_cost - args.price,
    }


def project_debt(args: ProjectDebtArgs) -> dict[str, Any]:
    rate = Decimal(str(args.monthly_rate_pct)) / 100
    balance = args.balance
    balances: list[int] = []
    total_interest = 0
    paid_off_in: int | None = None
    for month in range(1, args.months + 1):
        interest = int((balance * rate).to_integral_value(rounding=ROUND_HALF_UP))
        total_interest += interest
        balance = max(0, balance + interest - args.monthly_payment)
        balances.append(balance)
        if balance == 0 and paid_off_in is None:
            paid_off_in = month
            break
    return {
        "balances": balances,
        "finalBalance": balance,
        "monthsToPayOff": paid_off_in,
        "totalInterest": total_interest,
        "neverPaysOff": paid_off_in is None and balance >= args.balance,
    }


def get_player_history(_: NoArgs, context: ToolContext) -> dict[str, Any]:
    return {
        "decisions": [
            {
                "month": item.month,
                "eventId": item.event_id,
                "teachingConcept": item.teaching_concept,
                "choiceId": item.choice_id,
            }
            for item in context.history
        ]
    }


def get_concepts_not_yet_taught(_: NoArgs, context: ToolContext) -> dict[str, Any]:
    taught = set(context.taught)
    return {"notTaughtYet": [c.value for c in TeachingConcept if c not in taught]}


@dataclass(frozen=True, slots=True)
class Tool:
    name: str
    description: str
    args: type[ToolArgs]
    run: Callable[[Any, ToolContext], dict[str, Any]]

    def schema(self) -> dict[str, Any]:
        parameters = self.args.model_json_schema()
        parameters.pop("title", None)
        return {
            "type": "function",
            "function": {
                "name": self.name,
                "description": self.description,
                "parameters": parameters,
            },
        }


TOOLS: dict[str, Tool] = {
    tool.name: tool
    for tool in (
        Tool(
            "simulate_purchase",
            "Exact monthly payment and total real cost of buying something in installments.",
            SimulatePurchaseArgs,
            lambda args, _: simulate_purchase(args),
        ),
        Tool(
            "project_debt",
            "How a debt evolves month by month with a fixed payment, and when it is paid off.",
            ProjectDebtArgs,
            lambda args, _: project_debt(args),
        ),
        Tool(
            "get_player_history",
            "The player's previous decisions this season (month, event, concept, choice).",
            NoArgs,
            get_player_history,
        ),
        Tool(
            "get_concepts_not_yet_taught",
            "Teaching concepts the player has not seen yet this season.",
            NoArgs,
            get_concepts_not_yet_taught,
        ),
    )
}


def tool_schemas(names: Iterable[str]) -> list[dict[str, Any]]:
    return [TOOLS[name].schema() for name in names]


def execute_tool(call: ToolCall, context: ToolContext, allowed: Iterable[str]) -> dict[str, Any]:
    """Run one tool call. Bad calls return an error payload the model can read and correct."""
    if call.name not in set(allowed):
        return {"error": f"unknown tool {call.name}"}
    tool = TOOLS[call.name]
    try:
        raw = json.loads(call.arguments or "{}")
        args = tool.args.model_validate(raw)
    except (json.JSONDecodeError, ValidationError) as error:
        return {"error": f"invalid arguments: {error}"}
    return tool.run(args, context)
