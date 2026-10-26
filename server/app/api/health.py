"""Health check endpoint."""

from fastapi import APIRouter
from pydantic import BaseModel

router = APIRouter(tags=["health"])


class HealthResponse(BaseModel):
    """Service status reported to the Unity client and uptime monitors."""

    status: str
    version: str


@router.get("/health", response_model=HealthResponse)
def get_health() -> HealthResponse:
    """Report that the service is up."""
    return HealthResponse(status="ok", version="0.1.0")
