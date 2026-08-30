from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from app import models, schemas
from app.auth import get_current_user
from app.database import get_db
from app.services import pilot_service


router = APIRouter(
    prefix="/pilot",
    tags=["Pilot Readiness"]
)


@router.get(
    "/consent",
    response_model=schemas.PilotConsentResponse
)
def get_consent(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    return pilot_service.get_pilot_consent(db, current_user.id)


@router.post(
    "/consent",
    response_model=schemas.PilotConsentResponse,
    status_code=201
)
def accept_consent(
    request: schemas.PilotConsentAcceptRequest,
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    return pilot_service.accept_pilot_consent(db, current_user.id)


@router.post(
    "/consent/revoke",
    response_model=schemas.PilotConsentResponse
)
def revoke_consent(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    return pilot_service.revoke_pilot_consent(db, current_user.id)
