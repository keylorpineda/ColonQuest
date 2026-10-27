"""Application settings loaded from environment variables."""

from functools import lru_cache
from typing import Annotated, Literal

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
    downgrade_model: str = "muse-spark-1.3"
    max_calls_per_session: int = Field(default=40, ge=1)
    max_calls_per_client_per_day: int = Field(default=200, ge=1)
    response_cache_ttl_s: int = Field(default=3600, ge=0)
    response_cache_max_entries: int = Field(default=500, ge=1)
    client_hash_salt: SecretStr = SecretStr("")
    allowed_origins: Annotated[list[str], NoDecode] = ["http://localhost:8000"]
    log_level: str = "INFO"

    # Open-weights NPCs (SCRUM-68): conditional on Baseten or Modal credits.
    npc_provider: Literal["meta_api", "open_weights"] = "meta_api"
    open_weights_base_url: str = ""
    open_weights_api_key: SecretStr = SecretStr("")
    open_weights_npc_model: str = "muse-spark-open"
    open_weights_timeout_s: float = Field(default=4.0, gt=0)

    # Agentic game master and seller (SCRUM-69..71): only if the playable month is ready.
    agentic_game_master: bool = False
    season_planner: bool = False
    seller_agent: bool = False
    agent_max_steps: int = Field(default=5, ge=1, le=10)
    agent_timeout_s: float = Field(default=10.0, gt=0)

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
