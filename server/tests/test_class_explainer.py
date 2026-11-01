from typing import Any

import pytest

from app.classes.explainer import (
    CONCEPT_NAMES,
    CONCEPT_NAMES_EN,
    ClassExplainer,
    ClassFigures,
    ExplanationText,
    fallback_explanation,
    figures_payload,
)
from app.classes.metrics import ConceptMetric
from app.config import Settings
from app.providers.base import ProviderUnavailableError, StructuredResult, Usage
from app.schemas import TeachingConcept

from .fakes import FakeLedger, FakeProvider, Step, fail

pytestmark = pytest.mark.anyio

FIGURES = ClassFigures(
    students=28,
    finished_games=25,
    negotiations=40,
    trap_detection_pct=35,
    average_final_debt=212000,
    concepts=[
        ConceptMetric(TeachingConcept.MINIMUM_PAYMENT, 30, 62),
        ConceptMetric(TeachingConcept.BUDGET, 20, 15),
    ],
)


def text(explanation: str) -> Step:
    def step() -> StructuredResult[Any]:
        value = ExplanationText(explanation=explanation)
        return StructuredResult(value=value, usage=Usage(model="m"), latency_ms=1)

    return step


def make_explainer(provider: FakeProvider) -> ClassExplainer:
    return ClassExplainer(provider, Settings(_env_file=None))  # type: ignore[call-arg]


async def test_model_explanation() -> None:
    provider = FakeProvider(text("El 62% pagó solo el mínimo; la deuda promedio fue ₡212 000."))
    explanation = await make_explainer(provider).explain(FakeLedger(), FIGURES)

    assert explanation.source == "model"
    assert '"concept": "pago mínimo"' in provider.calls[0]["user"]


async def test_invented_amount_falls_back() -> None:
    provider = FakeProvider(text("La deuda promedio fue ₡999 999."))
    explanation = await make_explainer(provider).explain(FakeLedger(), FIGURES)
    assert explanation.source == "fallback"


async def test_provider_failure_falls_back() -> None:
    provider = FakeProvider(fail(ProviderUnavailableError("down")))
    explanation = await make_explainer(provider).explain(FakeLedger(), FIGURES)
    assert explanation.source == "fallback"
    assert "pago mínimo (62%)" in explanation.text


async def test_empty_class_skips_the_model() -> None:
    provider = FakeProvider()
    empty = ClassFigures(0, 0, 0, None, None, [])
    explanation = await make_explainer(provider).explain(FakeLedger(), empty)

    assert provider.calls == []
    assert "Compartí el código" in explanation.text


def test_fallback_and_payload_mention_trap_detection() -> None:
    assert "35%" in fallback_explanation(FIGURES)
    assert '"trapDetectionPct": 35' in figures_payload(FIGURES)


def test_every_concept_has_a_name_in_both_languages() -> None:
    assert {concept.value for concept in TeachingConcept} == set(CONCEPT_NAMES)
    assert set(CONCEPT_NAMES) == set(CONCEPT_NAMES_EN)


def test_fallback_explanation_in_english() -> None:
    written = fallback_explanation(FIGURES, "en")

    assert "35%" in written
    assert "students played" in written


async def test_the_model_is_asked_in_english() -> None:
    provider = FakeProvider(text("Half of the class struggled."))

    explanation = await make_explainer(provider).explain(FakeLedger(), FIGURES, language="en")

    assert explanation.source == "model"
    assert "- English, clear" in provider.calls[0]["system"]
    assert '"concept": "minimum payment"' in provider.calls[0]["user"]
