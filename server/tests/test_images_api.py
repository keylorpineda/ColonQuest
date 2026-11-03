import io
from collections.abc import AsyncIterator

import httpx2
import pytest
from PIL import Image
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.api.images import get_image_service
from app.config import Settings
from app.cost_guard import BudgetMode
from app.db import CachedImage, ModelCall
from app.db.session import get_session
from app.images.service import (
    FALLBACK_PNG,
    MAX_IMAGE_SIDE,
    ImageService,
    build_image_prompt,
    shrink_image,
)
from app.main import create_app
from app.providers.base import GeneratedImage, ProviderUnavailableError, Usage

from .fakes import FakeLedger

pytestmark = pytest.mark.anyio

SAMPLE_PNG = b"\x89PNG\r\n\x1a\npixel-art-bytes"


class FakeImageGenerator:
    def __init__(self, data: bytes | None = SAMPLE_PNG) -> None:
        self.data = data
        self.prompts: list[str] = []

    async def generate_image(self, prompt: str, *, size: str = "1024x1024") -> GeneratedImage:
        self.prompts.append(prompt)
        if self.data is None:
            raise ProviderUnavailableError("image model unavailable")
        return GeneratedImage(
            data=self.data,
            media_type="image/png",
            revised_prompt=None,
            usage=Usage(model="muse-image-1.0", images=1),
            latency_ms=420,
        )


def test_build_image_prompt_distinguishes_portraits_and_scenes() -> None:
    portrait_prompt = build_image_prompt("npc_don_rafa")
    scene_prompt = build_image_prompt("scene_taller_mecanico")
    other_prompt = build_image_prompt("evento_aguinaldo")

    assert "character bust portrait" in portrait_prompt
    assert "everyday Costa Rican scene" in scene_prompt
    assert "Costa Rican everyday life" in other_prompt


def test_build_image_prompt_keeps_year_cards_free_of_text() -> None:
    prompt = build_image_prompt("card_year_heavy_debt")

    assert "no text, no numbers, no letters" in prompt
    assert "empty wallet" in prompt


async def test_image_service_falls_back_when_budget_exhausted() -> None:
    settings = Settings(_env_file=None)  # type: ignore[call-arg]
    generator = FakeImageGenerator()
    service = ImageService(generator, settings)
    ledger = FakeLedger(mode=BudgetMode.FALLBACK_ONLY)

    outcome = await service.generate(ledger, "scene_pulperia")

    assert outcome.source == "fallback"
    assert outcome.cacheable is False
    assert outcome.data == FALLBACK_PNG
    assert generator.prompts == []
    assert ledger.records == []


async def test_image_service_records_failed_call_and_returns_fallback() -> None:
    settings = Settings(_env_file=None)  # type: ignore[call-arg]
    generator = FakeImageGenerator(data=None)
    service = ImageService(generator, settings)
    ledger = FakeLedger(mode=BudgetMode.NORMAL)

    outcome = await service.generate(ledger, "npc_kevin")

    assert outcome.source == "fallback"
    assert outcome.cacheable is False
    assert outcome.data == FALLBACK_PNG
    assert len(ledger.records) == 1
    assert ledger.records[0]["ok"] is False
    assert ledger.records[0]["endpoint"] == "image_generation"


@pytest.fixture
def generator() -> FakeImageGenerator:
    return FakeImageGenerator()


@pytest.fixture
async def client(
    db: AsyncSession, generator: FakeImageGenerator
) -> AsyncIterator[httpx2.AsyncClient]:
    settings = Settings(_env_file=None)  # type: ignore[call-arg]
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_image_service] = lambda: ImageService(generator, settings)
    transport = httpx2.ASGITransport(app=app)
    async with httpx2.AsyncClient(transport=transport, base_url="http://test") as http:
        yield http


async def test_get_image_generates_once_and_serves_second_request_from_cache(
    client: httpx2.AsyncClient, generator: FakeImageGenerator, db: AsyncSession
) -> None:
    first = await client.get("/api/images/scene_soda")

    assert first.status_code == 200
    assert first.headers["x-image-source"] == "model"
    assert first.headers["content-type"] == "image/png"
    assert first.content == SAMPLE_PNG
    assert len(generator.prompts) == 1

    second = await client.get("/api/images/scene_soda")

    assert second.status_code == 200
    assert second.headers["x-image-source"] == "cache"
    assert second.content == SAMPLE_PNG
    assert len(generator.prompts) == 1  # not called a second time

    calls = (
        await db.scalars(select(ModelCall).where(ModelCall.endpoint == "image_generation"))
    ).all()
    assert len(calls) == 1
    assert calls[0].ok is True


async def test_get_image_does_not_cache_fallback_when_provider_fails(
    client: httpx2.AsyncClient, generator: FakeImageGenerator, db: AsyncSession
) -> None:
    generator.data = None

    response = await client.get("/api/images/npc_don_rafa_mecanico")

    assert response.status_code == 200
    assert response.headers["x-image-source"] == "fallback"
    assert response.content == FALLBACK_PNG

    cached = (
        await db.execute(select(CachedImage).where(CachedImage.key == "npc_don_rafa_mecanico"))
    ).scalar_one_or_none()
    assert cached is None


async def test_get_image_never_pays_for_a_key_outside_the_set(
    client: httpx2.AsyncClient, generator: FakeImageGenerator, db: AsyncSession
) -> None:
    response = await client.get("/api/images/soda_elmolle")

    assert response.status_code == 200
    assert response.headers["x-image-source"] == "fallback"
    assert generator.prompts == []


async def test_get_image_still_serves_a_cached_key_outside_the_set(
    client: httpx2.AsyncClient, generator: FakeImageGenerator, db: AsyncSession
) -> None:
    db.add(CachedImage(key="soda_elmolle", media_type="image/png", data=SAMPLE_PNG))
    await db.commit()

    response = await client.get("/api/images/soda_elmolle")

    assert response.headers["x-image-source"] == "cache"
    assert generator.prompts == []


async def test_get_image_rejects_invalid_key(client: httpx2.AsyncClient) -> None:
    response = await client.get("/api/images/INVALID-KEY!")
    assert response.status_code == 422


def _png(side: int) -> bytes:
    output = io.BytesIO()
    Image.new("RGB", (side, side), (200, 120, 40)).save(output, format="PNG")
    return output.getvalue()


def test_shrink_image_downscales_large_model_output() -> None:
    data, media_type = shrink_image(_png(1600), "image/png")

    with Image.open(io.BytesIO(data)) as image:
        assert image.size == (MAX_IMAGE_SIDE, MAX_IMAGE_SIDE)
        assert image.mode == "P"
    assert media_type == "image/png"


def test_shrink_image_keeps_small_paletted_or_unreadable_data() -> None:
    small, _ = shrink_image(_png(64), "image/png")

    assert shrink_image(small, "image/png") == (small, "image/png")
    assert shrink_image(b"not an image", "image/webp") == (b"not an image", "image/webp")
