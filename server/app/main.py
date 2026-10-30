"""FastAPI application entry point."""

from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api import classes, dashboard, events, health, images, negotiation, sessions, summary
from app.classes.explainer import ClassExplainer
from app.config import get_settings
from app.game_master.agentic import AgenticGameMaster
from app.game_master.planner import SeasonPlanner
from app.game_master.service import GameMaster
from app.images.service import ImageService
from app.logging_config import configure_logging
from app.negotiation.agent import SellerAgent
from app.negotiation.service import Negotiator
from app.providers.meta import MetaProvider
from app.providers.open_weights import OpenWeightsProvider
from app.providers.routing import build_npc_provider
from app.response_cache import TTLCache
from app.schemas import GameEvent
from app.summary.writer import SummaryWriter


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    """Create one provider and one game master per process and close them on shutdown."""
    settings = get_settings()
    provider = MetaProvider(settings)
    open_weights = OpenWeightsProvider(settings)
    cache: TTLCache[GameEvent] = TTLCache(
        max_entries=settings.response_cache_max_entries, ttl_s=settings.response_cache_ttl_s
    )
    app.state.provider = provider
    # The agentic wrapper delegates to the single-call game master unless AGENTIC_GAME_MASTER.
    app.state.game_master = AgenticGameMaster(
        GameMaster(provider, settings, cache), provider, settings
    )
    # NPC dialogue and the seller agent (SELLER_AGENT) run on open weights (Baseten) when
    # NPC_PROVIDER=open_weights, with automatic fallback to Meta on every call.
    npc_provider = build_npc_provider(settings, provider, open_weights)
    app.state.negotiator = SellerAgent(Negotiator(npc_provider, settings), npc_provider, settings)
    # Only consulted when SEASON_PLANNER is on (see app.api.events.get_season_planner).
    app.state.season_planner = SeasonPlanner(provider, settings)
    app.state.summary_writer = SummaryWriter(provider, settings)
    app.state.class_explainer = ClassExplainer(provider, settings)
    app.state.image_service = ImageService(provider, settings)
    try:
        yield
    finally:
        await provider.aclose()
        await open_weights.aclose()


def create_app() -> FastAPI:
    """Build the FastAPI application with middleware and routers."""
    settings = get_settings()
    configure_logging(settings.log_level)
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
        expose_headers=["X-Event-Source", "X-Reply-Source", "X-Image-Source"],
    )
    app.include_router(health.router, prefix="/api")
    app.include_router(sessions.router, prefix="/api")
    app.include_router(classes.router, prefix="/api")
    app.include_router(dashboard.router, prefix="/api")
    app.include_router(events.router, prefix="/api")
    app.include_router(negotiation.router, prefix="/api")
    app.include_router(summary.router, prefix="/api")
    app.include_router(images.router, prefix="/api")
    return app


app = create_app()
