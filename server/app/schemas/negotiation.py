"""Reply returned for each negotiation turn."""

from typing import Annotated

from pydantic import Field

from app.schemas.base import ContractModel
from app.schemas.enums import DealStatus, KeyQuestion, SellerFact


class NegotiationReply(ContractModel):
    transcript: Annotated[str, Field(max_length=1000)]
    npc_reply: Annotated[str, Field(min_length=1, max_length=500)]
    revealed_facts: Annotated[
        list[SellerFact], Field(default_factory=list, max_length=len(SellerFact))
    ]
    key_questions_asked: Annotated[
        list[KeyQuestion], Field(default_factory=list, max_length=len(KeyQuestion))
    ]
    deal_status: DealStatus
    # The term on the table after this turn: the one the user signs. 0 when it is the offer's own.
    term_months: Annotated[int, Field(ge=0, le=120)] = 0
