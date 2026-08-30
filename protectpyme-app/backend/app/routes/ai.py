from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from app.auth import get_current_user
from app.database import get_db
from app.services.ai_service import AIService
from app.services import pilot_service

router = APIRouter(
    prefix="/ai",
    tags=["Inteligencia Artificial"]
)

@router.get("/risk/me")
async def get_my_risk(
    current_user = Depends(get_current_user),
    db: Session = Depends(get_db)
):
    result = await AIService.get_user_risk_prediction(
        db,
        current_user.id
    )

    pilot_service.persist_recommendation_event(
        db,
        current_user.id,
        result
    )

    return result
