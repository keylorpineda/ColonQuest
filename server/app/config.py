"""Application settings loaded from environment variables."""

from functools import lru_cache
from typing import Annotated

from pydantic import Field, SecretStr, field_validator
from pydantic_settings import BaseSettings, NoDecode, SettingsConfigDict


class Settings(BaseSettings):
    """Runtime configuration. Secrets are only ever read from the environment."""

    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    model_api_key: SecretStr = SecretStr("")
    meta_base_url: str = "https://api.meta.ai/v1"
    model_timeout_s: float = Field(default=8.0, gt=0)
    database_url: str = ""
    game_master_model: str = "muse-spark-1.3-contributor"
    summary_model: str = "muse-spark-1.3-contributor"
    image_model: str = "muse-image-1.0"
    transcribe_model: str = "muse-voice-transcribe-1.0"
    daily_budget_usd: float = Field(default=5.0, ge=0)
    allowed_origins: Annotated[list[str], NoDecode] = ["http://localhost:8000"]

    @field_validator("allowed_origins", mode="before")
    @classmethod
    def split_origins(cls, value: str | list[str]) -> list[str]:
        """Accept a comma-separated string from the environment."""
        if isinstance(value, str):
            return [origin.strip() for origin in value.split(",") if origin.strip()]
        return value


@lru_cache
def get_settings() -> Settings:
    """Return the cached settings instance."""
    return Settings()
