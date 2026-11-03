"""Write extra fallback events offline with Muse Wonder (event bank, ADR 0004).

Usage (from server/, with MODEL_API_KEY in .env):
    uv run python scripts/generate_event_bank.py              # 3 events per teaching concept
    uv run python scripts/generate_event_bank.py --per-concept 1 --dry-run

Makes real, billed calls (about $0.02 per event, well under $1 in total). Each event uses the game
master's SYSTEM_PROMPT and is kept only if it validates as a GameEvent, survives clamp_event
unchanged, has a new id, quotes no amount that is not in its effects and is not a December event
(the fallback picker would show it in any month). Accepted events are appended to
app/game_master/fallback_events.json; the hand-written events are never removed.
"""

import argparse
import asyncio
import json
import re
import sys
from decimal import Decimal
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from app.config import get_settings
from app.cost_guard import estimate_cost
from app.game_master.fallback import FALLBACK_EVENTS_PATH
from app.game_master.limits import clamp_event
from app.game_master.prompt import SYSTEM_PROMPT
from app.game_master.season import CONCEPT_EVENT_TYPE
from app.negotiation.questions import quoted_amounts
from app.providers.base import ProviderError
from app.providers.meta import MetaProvider
from app.schemas import EventType, GameEvent, TeachingConcept

MODEL = "muse-wonder-1.0"
TIMEOUT_S = 120.0
CONCURRENCY = 4
ID_PREFIX = "evt_bank_"
# Only scenes that scripts/pregenerate_images.py already cached, so no event waits on Muse Image.
SCENE_KEYS = (
    "scene_consultorio",
    "scene_cooperativa",
    "scene_esquina_barrio",
    "scene_estado_cuenta",
    "scene_gasolinera",
    "scene_ins",
    "scene_oficina",
    "scene_soda",
    "scene_taller_mecanico",
    "scene_tienda_celulares",
    "scene_tienda_electro",
)
DECEMBER_WORDS = re.compile(r"aguinaldo|marchamo|diciembre|navidad|navideñ", re.IGNORECASE)
# Event types that fit each concept besides the planner's default, for variety across the bank.
EXTRA_TYPES: dict[TeachingConcept, tuple[EventType, ...]] = {
    TeachingConcept.BUDGET: (EventType.INCOME, EventType.OFFER),
    TeachingConcept.EMERGENCY_FUND: (EventType.EXPENSE, EventType.BONUS),
    TeachingConcept.COMPOUND_INTEREST: (EventType.EXPENSE, EventType.BONUS),
    TeachingConcept.MINIMUM_PAYMENT: (EventType.OFFER, EventType.EMERGENCY),
    TeachingConcept.INSTALLMENTS_TOTAL_COST: (EventType.EXPENSE, EventType.OFFER),
    TeachingConcept.INFORMAL_LENDING: (EventType.EMERGENCY, EventType.OFFER),
    TeachingConcept.PYRAMID_SCHEME: (EventType.OFFER, EventType.SCAM),
    TeachingConcept.INFLATION: (EventType.EXPENSE, EventType.MACRO_NEWS),
    TeachingConcept.EXCHANGE_RATE: (EventType.EXPENSE, EventType.OFFER),
    TeachingConcept.PAYROLL_DEDUCTIONS: (EventType.BONUS, EventType.EXPENSE),
}


def request_prompt(concept: TeachingConcept, event_type: EventType, avoid: list[str]) -> str:
    payload = {
        "task": "Write one reusable event for the offline fallback catalog.",
        "teachingConcept": concept.value,
        "eventType": event_type.value,
        "eventIdPrefix": ID_PREFIX,
        "allowedImageKeys": list(SCENE_KEYS),
        "rules": [
            "The event must make sense in any month from January to November.",
            "Do not mention aguinaldo, marchamo, December or Christmas.",
            "Every amount quoted in the narrative must be the amount of one of the effects.",
            "imageKey must be one of allowedImageKeys.",
        ],
        "avoid": avoid,
    }
    return json.dumps(payload, ensure_ascii=False)


def rejection(event: GameEvent, known_ids: set[str]) -> str | None:
    """Why an event cannot join the catalog, or None if it can."""
    clamped, notes = clamp_event(event)
    if notes or clamped != event:
        return f"clamped: {'; '.join(notes)}"
    if event.event_id in known_ids or not event.event_id.startswith(ID_PREFIX):
        return f"bad or repeated id {event.event_id}"
    if event.image_key not in SCENE_KEYS:
        return f"imageKey {event.image_key} is not pre-generated"
    texts = [event.title, event.narrative, *(choice.label for choice in event.choices)]
    if any(DECEMBER_WORDS.search(text) for text in texts):
        return "December event"
    effect_amounts = {
        effect.amount for choice in event.choices for effect in choice.effects if effect.amount
    }
    invented = [
        amount for amount in quoted_amounts(event.narrative) if amount not in effect_amounts
    ]
    if invented:
        return f"narrative quotes amounts outside the effects: {invented}"
    return None


async def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--per-concept", type=int, default=3, help="events requested per concept")
    parser.add_argument("--dry-run", action="store_true", help="print, do not write the catalog")
    args = parser.parse_args()

    settings = get_settings()
    if not settings.model_api_key.get_secret_value():
        print("MODEL_API_KEY is not set in server/.env")
        return 1
    catalog = json.loads(FALLBACK_EVENTS_PATH.read_text(encoding="utf-8"))
    known_ids = {event["eventId"] for event in catalog}
    avoid = [f"{event['eventId']}: {event['title']}" for event in catalog]
    requests = [
        (concept, (CONCEPT_EVENT_TYPE[concept], *EXTRA_TYPES[concept])[n % 3])
        for concept in TeachingConcept
        for n in range(args.per_concept)
    ]
    provider = MetaProvider(settings)
    semaphore = asyncio.Semaphore(CONCURRENCY)
    costs: list[Decimal] = []
    accepted: list[GameEvent] = []

    async def generate(concept: TeachingConcept, event_type: EventType) -> None:
        async with semaphore:
            try:
                result = await provider.structured(
                    model=MODEL,
                    system=SYSTEM_PROMPT,
                    user=request_prompt(concept, event_type, avoid),
                    output=GameEvent,
                    reasoning_effort="low",  # offline: quality over speed
                    max_tokens=4000,
                    timeout_s=TIMEOUT_S,
                )
            except ProviderError as error:
                print(f"FAIL  {concept.value:<24} {type(error).__name__}: {error}")
                return
        costs.append(estimate_cost(result.usage))
        event = result.value
        reason = rejection(event, known_ids)
        label = f"{concept.value:<24} {result.latency_ms:>6} ms  {event.event_id}"
        if reason is not None:
            print(f"DROP  {label}  ({reason})")
            return
        known_ids.add(event.event_id)
        accepted.append(event)
        print(f"OK    {label}  {event.title}")

    try:
        await asyncio.gather(*(generate(concept, kind) for concept, kind in requests))
    finally:
        await provider.aclose()

    accepted.sort(key=lambda event: list(TeachingConcept).index(event.teaching_concept))
    if accepted and not args.dry_run:
        append_to_catalog(accepted)
    print(
        f"\n{len(accepted)} of {len(requests)} events accepted, "
        f"catalog now {len(catalog) + (0 if args.dry_run else len(accepted))}; "
        f"estimated cost ${sum(costs, Decimal(0))}"
    )
    return 0


def format_event(event: GameEvent) -> str:
    """Same layout as the hand-written catalog: one key per line, one choice per line."""
    data = event.model_dump(mode="json", by_alias=True, exclude_none=True)
    lines = []
    for key, value in data.items():
        if key == "choices":
            choices = ",\n".join(f"      {json.dumps(c, ensure_ascii=False)}" for c in value)
            lines.append(f'    "choices": [\n{choices}\n    ]')
        else:
            lines.append(f"    {json.dumps(key)}: {json.dumps(value, ensure_ascii=False)}")
    return "  {\n" + ",\n".join(lines) + "\n  }"


def append_to_catalog(events: list[GameEvent]) -> None:
    """Append after the last event without reformatting the hand-written ones."""
    text = FALLBACK_EVENTS_PATH.read_text(encoding="utf-8").rstrip()
    if not text.endswith("]"):
        raise ValueError("fallback_events.json does not end with a JSON array")
    blocks = ",\n".join(format_event(event) for event in events)
    FALLBACK_EVENTS_PATH.write_text(text[:-1].rstrip() + ",\n" + blocks + "\n]\n", encoding="utf-8")


if __name__ == "__main__":
    if sys.platform == "win32":
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())  # type: ignore[attr-defined]
    sys.exit(asyncio.run(main()))
