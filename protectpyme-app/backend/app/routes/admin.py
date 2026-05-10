from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from app.database import get_db
from app.auth import require_admin
from app.schemas import AuditLogOut
from app.crud import crud_audit

import logging
logger = logging.getLogger("protectpyme")

router = APIRouter(
    prefix="/admin",
    tags=["admin"]
)

@router.get("/audit-logs", response_model=list[AuditLogOut])
def get_audit_logs(
    skip: int = 0,
    limit: int = 50,
    db: Session = Depends(get_db),
    current_user = Depends(require_admin)
):
    logger.info(f"Admin {current_user.id} accessed audit logs")
    return crud_audit.get_audit_logs(db, skip, limit)