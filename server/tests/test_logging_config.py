import logging

import pytest

from app.logging_config import configure_logging


def test_app_loggers_get_level_and_format(capsys: pytest.CaptureFixture[str]) -> None:
    configure_logging("WARNING")
    logger = logging.getLogger("app.game_master.service")

    logger.info("hidden")
    logger.warning("serving fallback")

    err = capsys.readouterr().err
    assert "hidden" not in err
    assert "WARNING app.game_master.service: serving fallback" in err
    configure_logging("INFO")
