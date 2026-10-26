"""Short class codes that students can type on a phone or read off a whiteboard."""

import secrets

# No 0/O, 1/I or similar look-alikes.
ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"
CODE_LENGTH = 6


def generate_code(length: int = CODE_LENGTH) -> str:
    return "".join(secrets.choice(ALPHABET) for _ in range(length))
