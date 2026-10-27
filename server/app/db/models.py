"""Database tables (see the Database section in CLAUDE.md)."""

import uuid
from datetime import datetime
from decimal import Decimal
from typing import Any

from sqlalchemy import (
    BigInteger,
    Boolean,
    CheckConstraint,
    DateTime,
    ForeignKey,
    Integer,
    LargeBinary,
    Numeric,
    SmallInteger,
    String,
    Text,
    func,
)
from sqlalchemy.dialects.postgresql import JSONB, UUID
from sqlalchemy.orm import Mapped, mapped_column

from app.db.base import Base


def _created_at() -> Mapped[datetime]:
    return mapped_column(DateTime(timezone=True), server_default=func.now(), nullable=False)


class SchoolClass(Base):
    """A teacher's class in Class Mode, identified by a short join code."""

    __tablename__ = "classes"

    code: Mapped[str] = mapped_column(String(8), primary_key=True)
    name: Mapped[str] = mapped_column(String(80), nullable=False)
    created_at: Mapped[datetime] = _created_at()


class GameSession(Base):
    __tablename__ = "sessions"

    id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    class_code: Mapped[str | None] = mapped_column(
        ForeignKey("classes.code", ondelete="SET NULL"), index=True
    )
    client_hash: Mapped[str | None] = mapped_column(String(64), index=True)
    started_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), nullable=False
    )
    finished_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True))
    final_state: Mapped[dict[str, Any] | None] = mapped_column(JSONB)
    season_plan: Mapped[dict[str, Any] | None] = mapped_column(JSONB)


class Decision(Base):
    __tablename__ = "decisions"
    __table_args__ = (CheckConstraint("month BETWEEN 1 AND 12", name="month_range"),)

    id: Mapped[int] = mapped_column(BigInteger, primary_key=True, autoincrement=True)
    session_id: Mapped[uuid.UUID] = mapped_column(
        ForeignKey("sessions.id", ondelete="CASCADE"), index=True, nullable=False
    )
    month: Mapped[int] = mapped_column(SmallInteger, nullable=False)
    event_id: Mapped[str] = mapped_column(String(64), nullable=False)
    teaching_concept: Mapped[str] = mapped_column(String(40), index=True, nullable=False)
    choice_id: Mapped[str] = mapped_column(String(64), nullable=False)
    state_after: Mapped[dict[str, Any]] = mapped_column(JSONB, nullable=False)
    created_at: Mapped[datetime] = _created_at()


class Negotiation(Base):
    __tablename__ = "negotiations"

    id: Mapped[int] = mapped_column(BigInteger, primary_key=True, autoincrement=True)
    session_id: Mapped[uuid.UUID] = mapped_column(
        ForeignKey("sessions.id", ondelete="CASCADE"), index=True, nullable=False
    )
    offer_id: Mapped[str] = mapped_column(String(40), nullable=False, server_default="unknown")
    transcript: Mapped[str] = mapped_column(Text, nullable=False)
    npc_reply: Mapped[str] = mapped_column(Text, nullable=False, server_default="")
    source: Mapped[str] = mapped_column(String(10), nullable=False, server_default="model")
    key_questions_asked: Mapped[list[str]] = mapped_column(JSONB, nullable=False, default=list)
    revealed_facts: Mapped[list[str]] = mapped_column(JSONB, nullable=False, default=list)
    deal_status: Mapped[str] = mapped_column(String(20), nullable=False)
    created_at: Mapped[datetime] = _created_at()


class ModelCall(Base):
    """One row per model call: drives the cost guard and the cost-per-game metric."""

    __tablename__ = "model_calls"

    id: Mapped[int] = mapped_column(BigInteger, primary_key=True, autoincrement=True)
    session_id: Mapped[uuid.UUID | None] = mapped_column(
        ForeignKey("sessions.id", ondelete="SET NULL"), index=True
    )
    provider: Mapped[str] = mapped_column(String(20), nullable=False, default="meta_api")
    model: Mapped[str] = mapped_column(String(60), nullable=False)
    endpoint: Mapped[str] = mapped_column(String(40), nullable=False)
    input_tokens: Mapped[int] = mapped_column(Integer, nullable=False, default=0)
    cached_input_tokens: Mapped[int] = mapped_column(Integer, nullable=False, default=0)
    output_tokens: Mapped[int] = mapped_column(Integer, nullable=False, default=0)
    estimated_cost_usd: Mapped[Decimal] = mapped_column(
        Numeric(12, 6), nullable=False, default=Decimal(0)
    )
    latency_ms: Mapped[int] = mapped_column(Integer, nullable=False, default=0)
    ok: Mapped[bool] = mapped_column(Boolean, nullable=False)
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), server_default=func.now(), index=True, nullable=False
    )


class CachedImage(Base):
    __tablename__ = "images"

    key: Mapped[str] = mapped_column(String(64), primary_key=True)
    url: Mapped[str | None] = mapped_column(String(500))
    media_type: Mapped[str] = mapped_column(String(30), nullable=False, default="image/png")
    data: Mapped[bytes | None] = mapped_column(LargeBinary)
    created_at: Mapped[datetime] = _created_at()
