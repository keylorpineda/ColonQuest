"""An event never charges twice nor says one figure and applies another."""

from app.game_master.facts import charges_fixed_costs, is_dishonest, label_lies_about_money
from app.game_master.limits import clamp_event
from app.schemas import EffectKind, GameEvent, MoneySource


def event(*choices: tuple[str, list[dict[str, object]]]) -> GameEvent:
    return GameEvent.model_validate(
        {
            "eventId": "evt_test",
            "type": "expense",
            "title": "Test",
            "narrative": "Test",
            "teachingConcept": "budget",
            "choices": [
                {"id": f"c{i}", "label": label, "effects": effects}
                for i, (label, effects) in enumerate(choices)
            ],
        }
    )


def expense(amount: int, source: str = "cash") -> dict[str, object]:
    return {"kind": "expense", "amount": amount, "source": source}


SKIP: tuple[str, list[dict[str, object]]] = ("Say no", [])


def test_paying_the_rent_or_the_bills_would_charge_them_twice() -> None:
    assert charges_fixed_costs(event(("Pay room + bus pass first", [expense(90000)]), SKIP))
    assert charges_fixed_costs(event(("Pagar el alquiler", [expense(90000)]), SKIP))
    assert not charges_fixed_costs(event(("Concert with friends", [expense(45000)]), SKIP))


def test_a_label_has_to_say_what_the_choice_moves() -> None:
    assert label_lies_about_money(event(("Pay ₡80 000 now", [expense(45000)]), SKIP))
    assert not label_lies_about_money(event(("Pay ₡80 000 now", [expense(80000)]), SKIP))
    both = ("New shoes ₡30 000 and a gift ₡15 000", [expense(30000), expense(15000)])
    assert not label_lies_about_money(event(both, SKIP))
    assert not is_dishonest(event(("Concert with friends", [expense(45000)]), SKIP))


def test_what_is_paid_toward_the_card_lowers_the_card() -> None:
    repaired, _ = clamp_event(event(("Pay ₡80 000 to cut interest", [expense(80000)]), SKIP))
    effects = repaired.choices[0].effects
    assert len(effects) == 2
    assert effects[1].kind is EffectKind.INCOME
    assert effects[1].source is MoneySource.CREDIT_CARD
    assert effects[1].amount == 80000


def test_buying_with_the_card_is_not_paying_it() -> None:
    repaired, _ = clamp_event(event(("Pay with the card", [expense(80000, "credit_card")]), SKIP))
    assert len(repaired.choices[0].effects) == 1
