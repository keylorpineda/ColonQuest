"""Teacher dashboard for a Class Mode class.

The class code is the only credential (hackathon scope): anyone with the code can read the
aggregated, anonymous figures. No per-student data is returned.
"""

from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, Request, status
from sqlalchemy import func, select

from app.api.deps import AppSettings, DbSession
from app.classes.explainer import ClassExplainer, ClassFigures
from app.classes.metrics import DecisionRow, average_final_debt, concept_metrics, trap_detection_pct
from app.db import Decision, GameSession, Negotiation, SchoolClass
from app.game_master.ledger import DbLedger
from app.schemas.api import ClassCode, ClassDashboard, ConceptStat

router = APIRouter(prefix="/classes", tags=["classes"])


def get_class_explainer(request: Request) -> ClassExplainer:
    explainer: ClassExplainer = request.app.state.class_explainer
    return explainer


ClassExplainerDep = Annotated[ClassExplainer, Depends(get_class_explainer)]


@router.get("/{code}/dashboard", response_model=ClassDashboard)
async def class_dashboard(
    code: ClassCode, db: DbSession, settings: AppSettings, explainer: ClassExplainerDep
) -> ClassDashboard:
    school_class = await db.get(SchoolClass, code)
    if school_class is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, "class not found")

    in_class = GameSession.class_code == code
    students = await db.scalar(select(func.count()).select_from(GameSession).where(in_class))
    final_states = (
        await db.scalars(
            select(GameSession.final_state).where(in_class, GameSession.final_state.is_not(None))
        )
    ).all()
    decision_rows = (
        await db.execute(
            select(Decision.session_id, Decision.teaching_concept, Decision.state_after)
            .join(GameSession, GameSession.id == Decision.session_id)
            .where(in_class)
            .order_by(Decision.session_id, Decision.id)
        )
    ).all()
    key_questions = (
        await db.scalars(
            select(Negotiation.key_questions_asked)
            .join(GameSession, GameSession.id == Negotiation.session_id)
            .where(in_class)
        )
    ).all()

    figures = ClassFigures(
        students=int(students or 0),
        finished_games=len(final_states),
        negotiations=len(key_questions),
        trap_detection_pct=trap_detection_pct(key_questions),
        average_final_debt=average_final_debt(state for state in final_states if state),
        concepts=concept_metrics(DecisionRow(*row) for row in decision_rows),
    )
    explanation = await explainer.explain(DbLedger(db, settings, None), figures)
    await db.commit()

    return ClassDashboard(
        code=code,
        name=school_class.name,
        students=figures.students,
        finished_games=figures.finished_games,
        negotiations=figures.negotiations,
        trap_detection_pct=figures.trap_detection_pct,
        average_final_debt=figures.average_final_debt,
        concepts=[
            ConceptStat(
                concept=metric.concept,
                decisions=metric.decisions,
                risky_choice_pct=metric.risky_choice_pct,
            )
            for metric in figures.concepts
        ],
        explanation=explanation.text,
        source="model" if explanation.source == "model" else "fallback",
    )
