"""The conversations with the collectors, the lender and the pyramid stand."""

from enum import StrEnum
from typing import Annotated, Literal

from pydantic import Field

from app.schemas.base import ContractModel
from app.schemas.enums import GameLanguage

MAX_FIGURE = 100_000_000


class TalkPartner(StrEnum):
    COLLECTORS = "collectors"
    LENDER = "lender"
    PYRAMID = "pyramid"
    NEIGHBOR = "neighbor"
    STRANGER = "stranger"


class TalkFact(StrEnum):
    TOTAL_COST = "total_cost"
    RATE = "rate"
    ARRANGEMENT = "arrangement"
    WHAT_IF_NOT_PAID = "what_if_not_paid"
    WHERE_MONEY_COMES = "where_money_comes"
    GUARANTEE = "guarantee"
    LEGAL = "legal"


class TalkSheetLine(ContractModel):
    """One true thing about the deal, from the game's engine, worded for the character."""

    fact: TalkFact
    text: Annotated[str, Field(min_length=1, max_length=200)]


class TalkExchange(ContractModel):
    player: Annotated[str, Field(max_length=500)]
    reply: Annotated[str, Field(max_length=500)]


class TalkRequest(ContractModel):
    partner: TalkPartner
    text: Annotated[str, Field(min_length=1, max_length=500)]
    language: GameLanguage = GameLanguage.SPANISH
    sheet: Annotated[list[TalkSheetLine], Field(min_length=1, max_length=len(TalkFact))]
    # The only figures the character may say. The game sends them: the model never computes one.
    amounts: Annotated[list[Annotated[int, Field(ge=0, le=MAX_FIGURE)]], Field(max_length=20)]
    percents: Annotated[list[Annotated[int, Field(ge=0, le=1000)]], Field(max_length=10)]
    history: Annotated[list[TalkExchange], Field(default_factory=list, max_length=20)]


class TalkTurn(ContractModel):
    """What the model returns."""

    npc_reply: Annotated[str, Field(min_length=1, max_length=320)]
    asked_facts: Annotated[list[TalkFact], Field(max_length=len(TalkFact))]


class TalkReply(ContractModel):
    npc_reply: str
    asked_facts: list[TalkFact]
    source: Literal["model"]
