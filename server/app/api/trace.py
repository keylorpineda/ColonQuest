"""What the models and agents did in a session, so the judges' demo can show its work."""

import uuid

from fastapi import APIRouter, HTTPException, status
from pydantic import ValidationError
from sqlalchemy import select

from app.agents.trace import AGENT_TRACES
from app.api.deps import DbSession
from app.db import GameSession, ModelCall
from app.game_master.season import SeasonPlan
from app.schemas.api import ModelCallInfo, PlanMonthInfo, SessionTrace, ToolStepInfo

router = APIRouter(prefix="/sessions", tags=["trace"])

MAX_CALLS = 40


def plan_months(stored: dict[str, object] | None) -> list[PlanMonthInfo]:
    if stored is None:
        return []
    try:
        plan = SeasonPlan.model_validate(stored)
    except ValidationError:
        return []
    return [
        PlanMonthInfo(
            month=month.month,
            teaching_concept=month.teaching_concept.value,
            event_type=month.event_type.value,
            theme=month.theme,
        )
        for month in plan.months
    ]


@router.get("/{session_id}/trace", response_model=SessionTrace)
async def session_trace(session_id: uuid.UUID, db: DbSession) -> SessionTrace:
    """The session's model calls, tool steps and season plan. Finished sessions still answer."""
    game_session = await db.get(GameSession, session_id)
    if game_session is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "session not found")

    rows = (
        await db.scalars(
            select(ModelCall)
            .where(ModelCall.session_id == session_id)
            .order_by(ModelCall.id.desc())
            .limit(MAX_CALLS)
        )
    ).all()
    calls = [
        ModelCallInfo(
            endpoint=row.endpoint,
            provider=row.provider,
            model=row.model,
            input_tokens=row.input_tokens,
            output_tokens=row.output_tokens,
            cost_usd=float(row.estimated_cost_usd),
            latency_ms=row.latency_ms,
            ok=row.ok,
        )
        for row in reversed(rows)
    ]
    steps = [
        ToolStepInfo(
            endpoint=step.endpoint,
            step=step.step,
            tool=step.tool,
            arguments=step.arguments,
            result=step.result,
        )
        for step in AGENT_TRACES.for_session(session_id)
    ]
    return SessionTrace(
        calls=calls,
        tool_steps=steps,
        season_plan=plan_months(game_session.season_plan),
        total_cost_usd=sum(call.cost_usd for call in calls),
    )
