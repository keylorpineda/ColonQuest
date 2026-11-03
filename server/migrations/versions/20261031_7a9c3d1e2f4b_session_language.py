"""session language

Revision ID: 7a9c3d1e2f4b
Revises: 53dc403e9bfc
Create Date: 2026-10-31 07:30:00.000000

"""

from collections.abc import Sequence

import sqlalchemy as sa
from alembic import op

# revision identifiers, used by Alembic.
revision: str = "7a9c3d1e2f4b"
down_revision: str | Sequence[str] | None = "53dc403e9bfc"
branch_labels: str | Sequence[str] | None = None
depends_on: str | Sequence[str] | None = None


def upgrade() -> None:
    """Upgrade schema."""
    op.add_column(
        "sessions",
        sa.Column("language", sa.String(length=5), server_default="es", nullable=False),
    )


def downgrade() -> None:
    """Downgrade schema."""
    op.drop_column("sessions", "language")
