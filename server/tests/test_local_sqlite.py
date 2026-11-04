"""A local run keeps its data in SQLite, with no database to install (RUN_AND_BUILD.md)."""

from pathlib import Path
from typing import Any

import pytest
from sqlalchemy.ext.asyncio import async_sessionmaker, create_async_engine

from app.config import get_settings
from app.db import Base, Decision, GameSession
from app.db import session as db_session


@pytest.mark.anyio
async def test_the_tables_hold_a_session_and_its_decision(tmp_path: Path) -> None:
    engine = create_async_engine(f"sqlite+aiosqlite:///{tmp_path / 'local.db'}")
    async with engine.begin() as connection:
        await connection.run_sync(Base.metadata.create_all)

    async with async_sessionmaker(engine, expire_on_commit=False)() as db:
        run = GameSession(language="en")
        db.add(run)
        await db.commit()
        decision = Decision(
            session_id=run.id,
            month=1,
            event_id="evt",
            teaching_concept="budget",
            choice_id="pay_cash",
            state_after={"month": 1, "cash": 120000},
        )
        db.add(decision)
        await db.commit()

        stored = await db.get(Decision, decision.id)
        assert decision.id == 1
        assert stored is not None
        assert stored.state_after["cash"] == 120000
    await engine.dispose()


def test_only_sqlite_counts_as_local() -> None:
    assert db_session.is_sqlite("sqlite+aiosqlite:///./colonquest_local.db")
    assert not db_session.is_sqlite("postgresql+psycopg://user:password@host/dbname")


@pytest.mark.anyio
async def test_postgres_keeps_its_schema_from_the_migrations(monkeypatch: Any) -> None:
    def fail() -> None:
        raise AssertionError("the engine must not be touched")

    monkeypatch.setenv("DATABASE_URL", "postgresql+psycopg://user:password@host/dbname")
    monkeypatch.setattr(db_session, "get_engine", fail)
    get_settings.cache_clear()
    try:
        await db_session.prepare_local_database()
    finally:
        monkeypatch.undo()
        get_settings.cache_clear()
