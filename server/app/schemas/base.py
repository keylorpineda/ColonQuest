"""Shared building blocks for contract models."""

from typing import Annotated

from pydantic import BaseModel, ConfigDict, Field, StrictInt
from pydantic.alias_generators import to_camel

# Money is always whole colones. StrictInt rejects floats such as 85000.5 or 85000.0.
Colones = Annotated[StrictInt, Field(ge=0, le=100_000_000)]
SignedColones = Annotated[StrictInt, Field(ge=-100_000_000, le=100_000_000)]
Percent = Annotated[float, Field(ge=0, le=100)]


class ContractModel(BaseModel):
    """Base model: camelCase on the wire, snake_case in Python, unknown fields rejected."""

    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
        serialize_by_alias=True,
        extra="forbid",
        frozen=True,
    )
