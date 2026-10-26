"""Class Mode endpoints."""

from fastapi import APIRouter, HTTPException, status

from app.api.deps import DbSession
from app.classes.codes import generate_code
from app.db import SchoolClass
from app.schemas.api import ClassCreated, CreateClassRequest

router = APIRouter(prefix="/classes", tags=["classes"])

MAX_CODE_ATTEMPTS = 5


@router.post("", status_code=status.HTTP_201_CREATED, response_model=ClassCreated)
async def create_class(body: CreateClassRequest, db: DbSession) -> ClassCreated:
    """Create a class and return the short code students type when they start a game."""
    for _ in range(MAX_CODE_ATTEMPTS):
        code = generate_code()
        if await db.get(SchoolClass, code) is None:
            db.add(SchoolClass(code=code, name=body.name))
            await db.commit()
            return ClassCreated(code=code, name=body.name)
    raise HTTPException(status.HTTP_503_SERVICE_UNAVAILABLE, "could not allocate a class code")
