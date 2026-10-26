"""Migration tests. They run only against a disposable database set in TEST_DATABASE_URL.

Never point TEST_DATABASE_URL at Neon: the downgrade step drops every table.
"""

import os
from pathlib import Path

import pytest
from alembic import command
from alembic.autogenerate import compare_metadata
from alembic.config import Config
from alembic.migration import MigrationContext
from sqlalchemy import create_engine, inspect

from app.db import Base
from app.db.session import normalize_url

TEST_DATABASE_URL = os.environ.get("TEST_DATABASE_URL", "")
SERVER_DIR = Path(__file__).resolve().parents[1]

pytestmark = pytest.mark.skipif(not TEST_DATABASE_URL, reason="TEST_DATABASE_URL not set")

EXPECTED_TABLES = {"classes", "sessions", "decisions", "negotiations", "model_calls", "images"}


def alembic_config() -> Config:
    config = Config(str(SERVER_DIR / "alembic.ini"))
    config.set_main_option("script_location", str(SERVER_DIR / "migrations"))
    config.set_main_option("sqlalchemy.url", normalize_url(TEST_DATABASE_URL).replace("%", "%%"))
    return config


def test_upgrade_downgrade_and_no_drift() -> None:
    config = alembic_config()
    engine = create_engine(normalize_url(TEST_DATABASE_URL))

    command.upgrade(config, "head")
    with engine.connect() as connection:
        assert set(inspect(connection).get_table_names()) >= EXPECTED_TABLES
        diff = compare_metadata(MigrationContext.configure(connection), Base.metadata)
        assert diff == [], f"models and migrations differ: {diff}"

    command.downgrade(config, "base")
    with engine.connect() as connection:
        assert not EXPECTED_TABLES & set(inspect(connection).get_table_names())

    command.upgrade(config, "head")
    engine.dispose()
