from collections.abc import AsyncIterator
from typing import Any

import httpx2
import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.api.negotiation import get_negotiator, get_transcriber
from app.config import Settings
from app.db import ModelCall
from app.db.session import get_session
from app.main import create_app
from app.negotiation.service import Negotiator, SellerTurn
from app.providers.base import (
    ProviderUnavailableError,
    StructuredResult,
    Transcription,
    Usage,
)

from .fakes import FakeProvider
from .test_audio import make_wav

pytestmark = pytest.mark.anyio


class FakeTranscriber:
    def __init__(self, text: str | None = "¿Cuánto pago en total?") -> None:
        self.text = text
        self.received: list[bytes] = []

    async def transcribe(
        self, wav: bytes, *, language_bias: list[str] | None = None
    ) -> Transcription:
        self.received.append(wav)
        if self.text is None:
            raise ProviderUnavailableError("asr down")
        usage = Usage(model="muse-voice-transcribe-1.0", audio_seconds=2.0)
        return Transcription(text=self.text, usage=usage, latency_ms=300)


def seller_step() -> Any:
    turn = SellerTurn(
        npc_reply="Diay, en total son ₡569 400.", revealed_facts=["total_cost"], deal_status="open"
    )
    return lambda: StructuredResult(value=turn, usage=Usage(model="m"), latency_ms=1)


@pytest.fixture
def transcriber() -> FakeTranscriber:
    return FakeTranscriber()


@pytest.fixture
async def client(
    db: AsyncSession, transcriber: FakeTranscriber
) -> AsyncIterator[httpx2.AsyncClient]:
    settings = Settings(_env_file=None, game_master_model="muse-wonder-1.0")  # type: ignore[call-arg]
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_negotiator] = lambda: Negotiator(
        FakeProvider(seller_step()), settings
    )
    app.dependency_overrides[get_transcriber] = lambda: transcriber
    transport = httpx2.ASGITransport(app=app)
    async with httpx2.AsyncClient(transport=transport, base_url="http://test") as http:
        yield http


async def post_voice(
    client: httpx2.AsyncClient, session_id: str, audio: bytes, offer_id: str = "celular_gama_alta"
) -> httpx2.Response:
    return await client.post(
        f"/api/sessions/{session_id}/negotiation/voice",
        data={"offerId": offer_id},
        files={"audio": ("clip.wav", audio, "audio/wav")},
    )


async def new_session(client: httpx2.AsyncClient) -> str:
    session_id: str = (await client.post("/api/sessions", json={})).json()["sessionId"]
    return session_id


async def test_voice_turn_transcribes_and_answers(
    client: httpx2.AsyncClient, transcriber: FakeTranscriber, db: AsyncSession
) -> None:
    session_id = await new_session(client)

    response = await post_voice(client, session_id, make_wav(48_000, channels=2, seconds=2))

    assert response.status_code == 200
    body = response.json()
    assert body["transcript"] == "¿Cuánto pago en total?"
    assert body["keyQuestionsAsked"] == ["total_cost"]
    assert transcriber.received[0][:4] == b"RIFF"
    endpoints = (await db.scalars(select(ModelCall.endpoint))).all()
    assert "transcribe" in endpoints


async def test_voice_rejects_bad_audio_before_paying(
    client: httpx2.AsyncClient, transcriber: FakeTranscriber
) -> None:
    session_id = await new_session(client)

    response = await post_voice(client, session_id, b"OggS not a wav")

    assert response.status_code == 422
    assert transcriber.received == []


async def test_transcriber_down_returns_503(
    client: httpx2.AsyncClient, transcriber: FakeTranscriber
) -> None:
    transcriber.text = None
    session_id = await new_session(client)

    response = await post_voice(client, session_id, make_wav(16_000))

    assert response.status_code == 503
    assert "text" in response.json()["detail"]


async def test_silence_is_reported(
    client: httpx2.AsyncClient, transcriber: FakeTranscriber
) -> None:
    transcriber.text = ""
    session_id = await new_session(client)

    response = await post_voice(client, session_id, make_wav(16_000))

    assert response.status_code == 422
    assert response.json()["detail"] == "no speech detected"


async def test_unknown_offer(client: httpx2.AsyncClient) -> None:
    session_id = await new_session(client)
    response = await post_voice(client, session_id, make_wav(16_000), offer_id="lancha")
    assert response.status_code == 404
