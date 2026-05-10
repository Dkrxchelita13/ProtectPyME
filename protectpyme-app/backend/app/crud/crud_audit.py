from sqlalchemy.orm import Session
from app import models

def get_audit_logs(db: Session, skip: int = 0, limit: int = 50):
    return (
        db.query(models.AuditLog)
        .order_by(models.AuditLog.created_at.desc())
        .offset(skip)
        .limit(limit)
        .all()
    )