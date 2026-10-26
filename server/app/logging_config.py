"""Readable application logs (timestamp, level, module) for local runs and Render."""

import logging
import logging.config

LOG_FORMAT = "%(asctime)s %(levelname)s %(name)s: %(message)s"


def configure_logging(level: str = "INFO") -> None:
    """Route the app's loggers to stderr with a consistent format. Safe to call more than once."""
    logging.config.dictConfig(
        {
            "version": 1,
            "disable_existing_loggers": False,
            "formatters": {"default": {"format": LOG_FORMAT}},
            "handlers": {
                "stderr": {
                    "class": "logging.StreamHandler",
                    "formatter": "default",
                    "stream": "ext://sys.stderr",
                }
            },
            "loggers": {"app": {"handlers": ["stderr"], "level": level, "propagate": False}},
        }
    )
