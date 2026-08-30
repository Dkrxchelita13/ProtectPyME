from fastapi import APIRouter, Depends, HTTPException
from fastapi.responses import Response
from sqlalchemy.orm import Session

from app import models, schemas
from app.auth import get_current_user, require_admin
from app.database import get_db
from app.services import (
    pilot_assessment_service,
    pilot_export_service,
    pilot_service,
)


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


@router.get(
    "/assessment/status",
    response_model=schemas.PilotAssessmentStatusResponse
)
def get_assessment_status(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    return pilot_assessment_service.get_assessment_status(db, current_user.id)


@router.post(
    "/assessment/start",
    response_model=schemas.PilotAssessmentStartResponse,
    status_code=201
)
def start_assessment(
    request: schemas.PilotAssessmentStartRequest,
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    try:
        return pilot_assessment_service.start_assessment(
            db,
            current_user.id,
            request
        )
    except pilot_assessment_service.PilotAssessmentPermissionError as exc:
        raise HTTPException(status_code=403, detail=str(exc)) from exc
    except pilot_assessment_service.PilotAssessmentConflictError as exc:
        raise HTTPException(status_code=409, detail=str(exc)) from exc


@router.post(
    "/assessment/{assessment_id}/answer",
    response_model=schemas.PilotAssessmentAnswerResponse
)
def record_assessment_answer(
    assessment_id: str,
    request: schemas.PilotAssessmentAnswerRequest,
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    try:
        return pilot_assessment_service.record_answer(
            db,
            current_user.id,
            assessment_id,
            request
        )
    except pilot_assessment_service.PilotAssessmentPermissionError as exc:
        raise HTTPException(status_code=403, detail=str(exc)) from exc
    except pilot_assessment_service.PilotAssessmentNotFoundError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except pilot_assessment_service.PilotAssessmentValidationError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except pilot_assessment_service.PilotAssessmentConflictError as exc:
        raise HTTPException(status_code=409, detail=str(exc)) from exc


@router.post(
    "/assessment/{assessment_id}/complete",
    response_model=schemas.PilotAssessmentResultItem
)
def complete_assessment(
    assessment_id: str,
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    try:
        return pilot_assessment_service.complete_assessment(
            db,
            current_user.id,
            assessment_id
        )
    except pilot_assessment_service.PilotAssessmentPermissionError as exc:
        raise HTTPException(status_code=403, detail=str(exc)) from exc
    except pilot_assessment_service.PilotAssessmentNotFoundError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except pilot_assessment_service.PilotAssessmentValidationError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except pilot_assessment_service.PilotAssessmentConflictError as exc:
        raise HTTPException(status_code=409, detail=str(exc)) from exc


@router.get(
    "/assessment/results",
    response_model=schemas.PilotAssessmentResultsResponse
)
def get_assessment_results(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    try:
        return pilot_assessment_service.get_assessment_results(
            db,
            current_user.id
        )
    except pilot_assessment_service.PilotAssessmentPermissionError as exc:
        raise HTTPException(status_code=403, detail=str(exc)) from exc


@router.get("/export/summary.csv")
def export_pilot_summary_csv(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(require_admin)
):
    rows = pilot_export_service.build_summary_dataset(db)
    csv_body = pilot_export_service.serialize_summary_csv(rows)

    return Response(
        content=csv_body,
        media_type="text/csv; charset=utf-8",
        headers={
            "Content-Disposition": (
                "attachment; filename=pilot_summary.csv"
            )
        },
    )


@router.get("/export/events.csv")
def export_pilot_events_csv(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(require_admin)
):
    rows = pilot_export_service.build_events_dataset(db)
    csv_body = pilot_export_service.serialize_events_csv(rows)

    return Response(
        content=csv_body,
        media_type="text/csv; charset=utf-8",
        headers={
            "Content-Disposition": (
                "attachment; filename=pilot_events.csv"
            )
        },
    )
