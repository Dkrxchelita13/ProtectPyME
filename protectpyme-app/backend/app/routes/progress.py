from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from app import models, schemas
from app.auth import get_current_user
from app.database import get_db
from app.services import progress_service


router = APIRouter(prefix="/progress", tags=["progress"])


@router.get("/me", response_model=schemas.UserProgressResponse)
def get_my_progress(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    progress = progress_service.get_or_create_user_progress(
        db,
        current_user.id
    )
    db.commit()
    db.refresh(progress)
    return progress
