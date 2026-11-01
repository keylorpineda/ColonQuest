from typing import Any

from app.game_master.fallback import load_fallback_events
from app.game_master.limits import clamp_event
from app.game_master.prompt import SYSTEM_PROMPT, SYSTEM_PROMPT_EN
from app.images.keys import CARD_KEYS, SCENE_BY_CONCEPT, SCENE_KEYS
from app.images.service import CARD_SUBJECTS
from app.schemas import GameEvent, TeachingConcept


def event(image_key: str | None) -> GameEvent:
    data: dict[str, Any] = {
        "eventId": "evt_test",
        "type": "expense",
        "title": "Almuerzo",
        "narrative": "Se te antojó almorzar en la soda.",
        "imageKey": image_key,
        "choices": [
            {"id": "a", "label": "Pagar", "effects": []},
            {"id": "b", "label": "No pagar", "effects": []},
        ],
        "teachingConcept": "budget",
    }
    return GameEvent.model_validate(data)


def test_a_key_outside_the_set_becomes_the_scene_of_the_concept() -> None:
    safe, notes = clamp_event(event("soda_elmolle"))

    assert safe.image_key == "scene_soda"
    assert notes == ["imageKey: soda_elmolle -> scene_soda"]


def test_a_key_of_the_set_or_no_key_is_left_alone() -> None:
    assert clamp_event(event("scene_ins")) == (event("scene_ins"), [])
    assert clamp_event(event(None)) == (event(None), [])


def test_every_concept_has_a_pregenerated_scene() -> None:
    assert set(SCENE_BY_CONCEPT) == set(TeachingConcept)
    assert set(SCENE_BY_CONCEPT.values()) <= set(SCENE_KEYS)


def test_every_fallback_event_uses_a_pregenerated_scene() -> None:
    keys = {item.image_key for item in load_fallback_events()} - {None}
    assert keys <= set(SCENE_KEYS)


def test_the_set_and_the_cards_agree() -> None:
    assert set(CARD_KEYS) == set(CARD_SUBJECTS)


def test_the_game_master_is_told_the_keys() -> None:
    for key in SCENE_KEYS:
        assert key in SYSTEM_PROMPT
        assert key in SYSTEM_PROMPT_EN
