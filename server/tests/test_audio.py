import io
import math
import wave
from array import array

import pytest

from app.negotiation.audio import AudioError, normalize_wav


def make_wav(rate: int, channels: int = 1, seconds: float = 1.0, width: int = 2) -> bytes:
    count = int(rate * seconds)
    tone = [int(8000 * math.sin(2 * math.pi * 440 * i / rate)) for i in range(count)]
    interleaved = [value for value in tone for _ in range(channels)]
    output = io.BytesIO()
    with wave.open(output, "wb") as clip:
        clip.setnchannels(channels)
        clip.setsampwidth(width)
        clip.setframerate(rate)
        if width == 2:
            clip.writeframes(array("h", interleaved).tobytes())
        else:
            clip.writeframes(bytes(count * channels))
    return output.getvalue()


def read(data: bytes) -> tuple[int, int, int, int]:
    with wave.open(io.BytesIO(data)) as clip:
        return clip.getnchannels(), clip.getsampwidth(), clip.getframerate(), clip.getnframes()


@pytest.mark.parametrize("rate", [16_000, 24_000])
def test_accepted_format_is_returned_untouched(rate: int) -> None:
    data = make_wav(rate)
    normalized, duration = normalize_wav(data)
    assert normalized is data
    assert duration == pytest.approx(1.0)


def test_browser_stereo_48k_becomes_mono_16k() -> None:
    normalized, duration = normalize_wav(make_wav(48_000, channels=2, seconds=2))
    channels, width, rate, frames = read(normalized)
    assert (channels, width, rate) == (1, 2, 16_000)
    assert frames == 32_000
    assert duration == pytest.approx(2.0)


def test_44100_is_resampled_to_16k() -> None:
    _, _, rate, frames = read(normalize_wav(make_wav(44_100))[0])
    assert rate == 16_000
    assert abs(frames - 16_000) <= 1


@pytest.mark.parametrize(
    ("data", "message"),
    [
        (b"OggS\x00\x00 not wav", "must be WAV"),
        (make_wav(16_000, seconds=16), "longer than 15"),
        (make_wav(16_000, width=1), "16-bit"),
        (make_wav(16_000, seconds=0), "empty"),
        (b"RIFF\x00\x00\x00\x00WAVEjunk", "valid WAV"),
    ],
    ids=["ogg", "too_long", "8_bit", "empty", "corrupt"],
)
def test_rejects_unusable_audio(data: bytes, message: str) -> None:
    with pytest.raises(AudioError, match=message):
        normalize_wav(data)
