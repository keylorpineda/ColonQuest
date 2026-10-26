"""WAV normalization for Muse Voice Transcribe without external tools.

The API accepts only mono, 16-bit PCM WAV at 16 or 24 kHz. Browsers usually record at 44.1 or
48 kHz, often in stereo, so the clip is down-mixed and resampled here with the standard library
(no ffmpeg needed on Render).
"""

import io
import wave
from array import array

TARGET_RATE = 16_000
ACCEPTED_RATES = frozenset({16_000, 24_000})
MAX_SECONDS = 15.5  # Unity caps recordings at 15 s; allow a little slack
MAX_BYTES = 4 * 1024 * 1024


class AudioError(ValueError):
    """The upload is not a usable clip; the message is safe to show to the client."""


def _resample(samples: array, source_rate: int, target_rate: int) -> array:  # type: ignore[type-arg]
    """Linear interpolation resampling; good enough for speech recognition."""
    if source_rate == target_rate or not samples:
        return samples
    length = max(1, int(len(samples) * target_rate / source_rate))
    step = source_rate / target_rate
    out = array("h", bytes(2 * length))
    last = len(samples) - 1
    for index in range(length):
        position = index * step
        left = int(position)
        right = min(left + 1, last)
        fraction = position - left
        out[index] = int(samples[left] + (samples[right] - samples[left]) * fraction)
    return out


def normalize_wav(data: bytes) -> tuple[bytes, float]:
    """Return a mono 16-bit WAV at 16 or 24 kHz and its duration in seconds."""
    if len(data) > MAX_BYTES:
        raise AudioError("audio is too large")
    if data[:4] != b"RIFF" or data[8:12] != b"WAVE":
        raise AudioError("audio must be WAV (RIFF/WAVE)")
    try:
        with wave.open(io.BytesIO(data)) as clip:
            channels = clip.getnchannels()
            width = clip.getsampwidth()
            rate = clip.getframerate()
            frames = clip.readframes(clip.getnframes())
    except (wave.Error, EOFError) as error:
        raise AudioError("audio is not a valid WAV file") from error

    if width != 2:
        raise AudioError("audio must be 16-bit PCM")
    if channels not in (1, 2):
        raise AudioError("audio must be mono or stereo")
    duration = len(frames) / (width * channels * rate)
    if duration > MAX_SECONDS:
        raise AudioError("audio is longer than 15 seconds")
    if duration == 0:
        raise AudioError("audio is empty")
    if channels == 1 and rate in ACCEPTED_RATES:
        return data, duration

    samples = array("h")
    samples.frombytes(frames)
    if channels == 2:
        samples = array(
            "h", ((samples[i] + samples[i + 1]) // 2 for i in range(0, len(samples), 2))
        )
    target = rate if rate in ACCEPTED_RATES else TARGET_RATE
    samples = _resample(samples, rate, target)

    output = io.BytesIO()
    with wave.open(output, "wb") as clip:
        clip.setnchannels(1)
        clip.setsampwidth(2)
        clip.setframerate(target)
        clip.writeframes(samples.tobytes())
    return output.getvalue(), duration
