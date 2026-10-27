"""Agentic game master: reads the player's history and computes real figures before writing.

It wraps the single-call GameMaster. When the flag is off, the budget or limits forbid model
calls, or the agent fails, the base game master handles the month exactly as before. Tools only
read data or compute; the event still goes through the same clamp as any model event.
"""

import logging
import uuid

from app.agents.loop import AgentRequest, run_tool_loop
from app.agents.tools import ToolContext
from app.config import Settings
from app.cost_guard import select_text_model
from app.game_master.ledger import Ledger
from app.game_master.limits import clamp_event
from app.game_master.prompt import SYSTEM_PROMPT, PromptContext, build_user_prompt
from app.game_master.service import EventOutcome, EventSource, FallbackReason, GameMaster
from app.providers.base import ProviderError, ToolCallingProvider
from app.schemas import GameEvent

logger = logging.getLogger(__name__)

ENDPOINT = "event_agent"
GAME_MASTER_TOOLS = (
    "get_player_history",
    "get_concepts_not_yet_taught",
    "simulate_purchase",
    "project_debt",
)

AGENT_SYSTEM_PROMPT = (
    SYSTEM_PROMPT
    + """

TOOLS
- Before answering you may call: get_player_history, get_concepts_not_yet_taught, \
simulate_purchase, project_debt. They only read data or compute; they never change the player.
- Use get_player_history to build on what the player already did and to avoid repeating events.
- Use simulate_purchase or project_debt to pick amounts that make the consequence concrete (for \
example how paying only the minimum keeps the card balance growing). Quote their results exactly.
- Use at most 4 tool calls, then answer with the event JSON only."""
)


class AgenticGameMaster:
    def __init__(self, base: GameMaster, provider: ToolCallingProvider, settings: Settings) -> None:
        self._base = base
        self._provider = provider
        self._settings = settings

    async def next_event(
        self, ledger: Ledger, session_id: uuid.UUID, context: PromptContext
    ) -> EventOutcome:
        if not self._settings.agentic_game_master:
            return await self._base.next_event(ledger, session_id, context)
        model = select_text_model(
            self._settings.game_master_model, await ledger.budget_mode(), self._settings
        )
        if model is None or await ledger.limit_exceeded(session_id) is not None:
            # The base game master applies the same checks and picks the right fallback reason.
            return await self._base.next_event(ledger, session_id, context)

        request = AgentRequest(
            session_id=session_id,
            endpoint=ENDPOINT,
            model=model,
            system=AGENT_SYSTEM_PROMPT,
            user=build_user_prompt(context),
            output=GameEvent,
            tools=GAME_MASTER_TOOLS,
            context=ToolContext(
                history=list(context.history), taught=list(context.taught_concepts)
            ),
            max_steps=self._settings.agent_max_steps,
            timeout_s=self._settings.agent_timeout_s,
        )
        try:
            result = await run_tool_loop(self._provider, ledger, request)
        except TimeoutError:
            # The player already waited the whole agent budget: serve a hand-written event now
            # instead of making them wait for another model call.
            logger.warning("agentic game master timed out; serving a fallback event")
            return self._base.fallback_event(context, FallbackReason.TIMEOUT)
        except ProviderError as error:
            logger.warning("agentic game master failed (%s); using the base game master", error)
            return await self._base.next_event(ledger, session_id, context)

        event, adjustments = clamp_event(result.value)
        logger.info("agentic event after %s steps, tools=%s", result.steps, result.tools_used)
        return EventOutcome(
            event=event, source=EventSource.AGENT, model=model, adjustments=adjustments
        )
