"""Cached pixel-art portrait and scene endpoint (GET /api/images/{key})."""

from typing import Annotated

from fastapi import APIRouter, Depends, Path, Request, Response
from sqlalchemy import select

from app.api.deps import AppSettings, ClientHash, DbSession
from app.cost_guard import calls_for_client_today
from app.db import CachedImage
from app.game_master.ledger import DbLedger
from app.images.keys import PREGENERATED_KEYS
from app.images.service import FALLBACK_PNG, ImageService

router = APIRouter(prefix="/images", tags=["images"])

ImageKeyPath = Annotated[str, Path(min_length=1, max_length=64, pattern=r"^[a-z0-9_ñ]+$")]


def get_image_service(request: Request) -> ImageService:
    image_service: ImageService = request.app.state.image_service
    return image_service


ImageServiceDep = Annotated[ImageService, Depends(get_image_service)]


@router.get(
    "/{key}",
    responses={
        200: {
            "content": {"image/png": {}},
            "description": "Cached or newly generated pixel-art PNG image.",
        }
    },
    response_class=Response,
)
async def get_image(
    key: ImageKeyPath,
    db: DbSession,
    settings: AppSettings,
    client: ClientHash,
    image_service: ImageServiceDep,
) -> Response:
    """Return a cached portrait or scene by key, generating it with Muse Image only if missing."""
    cached = (
        await db.execute(select(CachedImage).where(CachedImage.key == key))
    ).scalar_one_or_none()
    if cached is not None and cached.data is not None:
        return Response(
            content=cached.data,
            media_type=cached.media_type,
            headers={
                "X-Image-Source": "cache",
                "Cache-Control": "public, max-age=86400",
            },
        )

    # Only the pre-generated set may be paid for; a key the model made up is not worth 17 s.
    if (
        key not in PREGENERATED_KEYS
        or await calls_for_client_today(db, client) >= settings.max_calls_per_client_per_day
    ):
        return Response(
            content=FALLBACK_PNG,
            media_type="image/png",
            headers={"X-Image-Source": "fallback"},
        )

    ledger = DbLedger(db, settings, client)
    outcome = await image_service.generate(ledger, key)
    if outcome.cacheable:
        await db.merge(
            CachedImage(
                key=key,
                media_type=outcome.media_type,
                data=outcome.data,
            )
        )
    await db.commit()

    headers = {"X-Image-Source": outcome.source}
    if outcome.cacheable:
        headers["Cache-Control"] = "public, max-age=86400"
    return Response(
        content=outcome.data,
        media_type=outcome.media_type,
        headers=headers,
    )
