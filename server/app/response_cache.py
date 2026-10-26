"""Small in-process TTL cache so identical model requests are paid for only once."""

import hashlib
import json
import time
from collections import OrderedDict
from collections.abc import Callable


def cache_key(*parts: object) -> str:
    """Stable SHA-256 key for any JSON-serializable request parts."""
    payload = json.dumps(parts, sort_keys=True, ensure_ascii=False, default=str)
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


class TTLCache[V]:
    """LRU cache with per-entry expiry. Not shared across processes, which is fine for one dyno."""

    def __init__(
        self, max_entries: int, ttl_s: float, clock: Callable[[], float] = time.monotonic
    ) -> None:
        self._entries: OrderedDict[str, tuple[float, V]] = OrderedDict()
        self._max_entries = max_entries
        self._ttl_s = ttl_s
        self._clock = clock
        self.hits = 0
        self.misses = 0

    def get(self, key: str) -> V | None:
        entry = self._entries.get(key)
        if entry is None or entry[0] <= self._clock():
            self._entries.pop(key, None)
            self.misses += 1
            return None
        self._entries.move_to_end(key)
        self.hits += 1
        return entry[1]

    def set(self, key: str, value: V) -> None:
        if self._ttl_s <= 0:
            return
        self._entries[key] = (self._clock() + self._ttl_s, value)
        self._entries.move_to_end(key)
        while len(self._entries) > self._max_entries:
            self._entries.popitem(last=False)

    def __len__(self) -> int:
        return len(self._entries)
