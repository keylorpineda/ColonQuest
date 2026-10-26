# ADR 0001: Meta Model API contracts

- **Status:** Accepted
- **Date:** 2026-10-26
- **Ticket:** SCRUM-19
- **Source:** https://dev.meta.ai/docs (structured output, reasoning, speech to text, images)

## Context

Before writing code against the Meta Model API we verified the four open questions from
`CLAUDE.md` in the official documentation.

## Decisions

### 1. Structured output: Chat Completions with `response_format`

- Base URL `https://api.meta.ai/v1`, Bearer auth with `MODEL_API_KEY`. The OpenAI Python SDK works as-is.
- We use **Chat Completions** (`/v1/chat/completions`), not the Responses API, because its
  structured-output shape is fully documented:

  ```json
  "response_format": {
    "type": "json_schema",
    "json_schema": { "name": "GameEvent", "schema": { ... }, "strict": true }
  }
  ```

  The JSON string arrives in `choices[0].message.content`. The Responses API uses `text.format`
  instead; the two parameters are not interchangeable.
- Output is always schema-constrained; `strict: true` additionally validates that the schema
  follows the strict subset (root object, no `allOf`/`oneOf`, `additionalProperties: false`,
  every property in `required`). Recursive schemas return HTTP 400.
- We still validate every response with Pydantic (rule 3 in `CLAUDE.md`).

### 2. Reasoning effort

- Chat Completions: top-level `reasoning_effort`. Responses API: `reasoning.effort`.
- Values: `minimal`, `low`, `medium`, `high`, `xhigh`, `max`. `none` returns HTTP 400 on Wonder and Spark.
  `muse-spark-1.3-contributor` does not support `max`.
- Reasoning tokens count toward `max_tokens` and are billed as output tokens; reported in
  `usage.completion_tokens_details.reasoning_tokens`. The game master uses `low`.

### 3. Speech to text: `POST /v1/asr/transcribe`

- `multipart/form-data` with two parts:
  - `request` (`application/json`): `{"model": "muse-voice-transcribe-1.0", "audioEncoding": "WAV", "mode": "PUSH_TO_TALK", "languageBias": ["Spanish"]}`
  - `audio` (`audio/wav`)
- **WAV only**: mono, 16-bit integer PCM at 16 kHz or 24 kHz. Max 10 minutes and 32 MB.
- Response (`Accept: application/json`): `sessionId`, `transcript`, `audioDurationMs`, `turns`.
- Spanish is supported; auto-detect is the default, `languageBias` is only a hint.
- Browser `MediaRecorder` produces webm/ogg, so the backend converts to WAV
  (`ffmpeg -ac 1 -ar 16000 -c:a pcm_s16le`) unless the Unity `.jslib` already encodes WAV.
- Price: $0.18 per hour of audio; failed and 429 requests are not billed. 429 means back off and retry.
- The OpenAI SDK has no matching method, so this call uses `httpx` directly.

### 4. Image generation: `POST /v1/images/generations`

- Body: `model` (`muse-image-1.0`), `prompt`, `n` (1-10), `size` (sets aspect ratio only),
  `response_format` (`b64_json` default or `url`), `output_format` (`png`/`jpeg`/`webp`, default `webp`).
- Response: `data[].b64_json` or `data[].url`, plus `revised_prompt` and `usage`.
- We request `b64_json` + `png` and store the bytes ourselves, so cached images never expire.

## Consequences

- `app/providers/meta.py` wraps the OpenAI SDK for chat and images and `httpx` for transcription.
- Unity must send WAV, or the backend needs ffmpeg in the Render image for conversion.
- Open items to confirm with a real key (SCRUM-20): exact `usage` fields for images and the allowed `size` values.
