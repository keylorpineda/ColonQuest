"""FastAPI application entry point."""

from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api import classes, events, health, negotiation, sessions, summary
from app.config import get_settings
from app.game_master.service import GameMaster
from app.negotiation.service import Negotiator
from app.providers.meta import MetaProvider
from app.response_cache import TTLCache
from app.schemas import GameEvent
from app.summary.writer import SummaryWriter


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    """Create one provider and one game master per process and close them on shutdown."""
    settings = get_settings()
    provider = MetaProvider(settings)
    cache: TTLCache[GameEvent] = TTLCache(
        max_entries=settings.response_cache_max_entries, ttl_s=settings.response_cache_ttl_s
    )
    app.state.provider = provider
    app.state.game_master = GameMaster(provider, settings, cache)
    app.state.negotiator = Negotiator(provider, settings)
    app.state.summary_writer = SummaryWriter(provider, settings)
    try:
        yield
    finally:
        await provider.aclose()


def create_app() -> FastAPI:
    """Build the FastAPI application with middleware and routers."""
    settings = get_settings()
    app = FastAPI(
        title="Colón Quest API",
        version="0.1.0",
        description="Backend for the Colón Quest financial-literacy game.",
        lifespan=lifespan,
    )
    app.add_middleware(
        CORSMiddleware,
        allow_origins=settings.allowed_origins,
        allow_methods=["GET", "POST"],
        allow_headers=["*"],
        expose_headers=["X-Event-Source", "X-Reply-Source"],
    )
    app.include_router(health.router, prefix="/api")
    app.include_router(sessions.router, prefix="/api")
    app.include_router(classes.router, prefix="/api")
    app.include_router(events.router, prefix="/api")
    app.include_router(negotiation.router, prefix="/api")
    app.include_router(summary.router, prefix="/api")
    return app


app = create_app()
