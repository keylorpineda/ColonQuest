"""Budget, limits and call recording behind one small interface.

The game master depends on this protocol, not on the database, so its retry and fallback logic
is unit-tested in memory; DbLedger is the production implementation over Postgres.
"""

import uuid
from typing import Protocol

from sqlalchemy.ext.asyncio import AsyncSession

from app.config import Settings
from app.cost_guard import BudgetMode, LimitExceeded, budget_status, check_limits, record_call
from app.providers.base import Usage


class Ledger(Protocol):
    async def budget_mode(self) -> BudgetMode: ...

    async def limit_exceeded(self, session_id: uuid.UUID) -> LimitExceeded | None: ...

    async def record(
        self,
        *,
        usage: Usage,
        endpoint: str,
        latency_ms: int,
        ok: bool,
        session_id: uuid.UUID | None,
    ) -> None: ...


class DbLedger:
    def __init__(self, db: AsyncSession, settings: Settings, hashed_client: str | None) -> None:
        self._db = db
        self._settings = settings
        self._hashed_client = hashed_client

    async def budget_mode(self) -> BudgetMode:
        return (await budget_status(self._db, self._settings.daily_budget_usd)).mode

    async def limit_exceeded(self, session_id: uuid.UUID) -> LimitExceeded | None:
        return await check_limits(self._db, self._settings, session_id, self._hashed_client)

    async def record(
        self,
        *,
        usage: Usage,
        endpoint: str,
        latency_ms: int,
        ok: bool,
        session_id: uuid.UUID | None,
    ) -> None:
        await record_call(
            self._db,
            usage=usage,
            endpoint=endpoint,
            latency_ms=latency_ms,
            ok=ok,
            session_id=session_id,
        )
