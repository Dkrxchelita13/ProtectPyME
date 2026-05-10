from sqlalchemy.orm import Session
from app.models import AuditLog

import logging
logger = logging.getLogger("protectpyme")

def log_event(
    db: Session,
    actor_user_id: int | None,
    target_user_id: int | None,
    action: str,
    description: str,
    ip_address: str | None = None
):
    logger.info(f"Audit event: {action} by user {actor_user_id}")
    log = AuditLog(
        actor_user_id=actor_user_id,
        target_user_id=target_user_id,
        action=action,
        description=description,
        ip_address=ip_address
    )
    db.add(log)
    db.commit()