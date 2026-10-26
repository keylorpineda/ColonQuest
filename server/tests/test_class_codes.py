import re

from app.classes.codes import ALPHABET, generate_code


def test_codes_are_short_unambiguous_and_valid_class_codes() -> None:
    codes = {generate_code() for _ in range(200)}
    assert len(codes) > 190
    for code in codes:
        assert re.fullmatch(r"[A-Z0-9]{4,8}", code)
        assert not set(code) & set("01IO")
        assert set(code) <= set(ALPHABET)
