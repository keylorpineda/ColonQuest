"""FastAPI application entry point."""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api import health
from app.config import get_settings


def create_app() -> FastAPI:
    """Build the FastAPI application with middleware and routers."""
    settings = get_settings()
    app = FastAPI(
        title="Colón Quest API",
        version="0.1.0",
        description="Backend for the Colón Quest financial-literacy game.",
    )
    app.add_middleware(
        CORSMiddleware,
        allow_origins=settings.allowed_origins,
        allow_methods=["GET", "POST"],
        allow_headers=["*"],
    )
    app.include_router(health.router, prefix="/api")
    return app


app = create_app()
