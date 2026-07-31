from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from app import models, schemas
from app.auth import get_current_user
from app.database import get_db
from app.services import survey_service


router = APIRouter(
    prefix="/survey",
    tags=["Survey"]
)


@router.post(
    "/submit",
    response_model=schemas.SurveySubmitResponse,
    status_code=201
)
def submit_survey(
    request: schemas.SurveySubmitRequest,
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    return survey_service.submit_survey(
        db=db,
        user_id=current_user.id,
        request=request
    )


@router.get(
    "/status",
    response_model=schemas.SurveyStatusResponse
)
def get_survey_status(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    submission = survey_service.get_submission(
        db=db,
        user_id=current_user.id
    )

    return survey_service.build_status_response(submission)


@router.get(
    "/me",
    response_model=schemas.SurveySubmissionOut
)
def get_my_survey(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    submission = survey_service.get_submission(
        db=db,
        user_id=current_user.id
    )

    if submission is None:
        raise HTTPException(
            status_code=404,
            detail="Diagnostic survey submission not found"
        )

    return survey_service.build_submission_response(submission)
