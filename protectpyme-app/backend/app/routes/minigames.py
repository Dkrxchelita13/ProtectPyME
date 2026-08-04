from typing import Optional

from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session
from app import schemas
from app.database import get_db
from app.services import concept_mastery_service
from app.services import learning_content_service
from app.services import minigame_service
from app.services import minigame_session_service
from app.routes.auth import get_current_user

router = APIRouter(prefix="/minigames", tags=["Minigames"])


@router.get("/quiz")
def get_quiz(
    topic: str = Query("phishing"),
    risk: str = Query("alto"),
    current_user=Depends(get_current_user)
):
    return minigame_service.get_quiz(topic, risk)


@router.get(
    "/lesson",
    response_model=schemas.MinigameLessonResponse,
    summary="Obtener contenido pedagógico previo al minijuego",
    description=(
        "Entrega una explicación educativa previa según el área de "
        "vulnerabilidad, el nivel de riesgo y el tipo de minijuego del "
        "usuario autenticado."
    ),
)
def get_lesson(
    topic: str = Query(...),
    risk: str = Query(...),
    minigame: str = Query(...),
    current_user=Depends(get_current_user)
):
    normalized_topic = minigame_service.normalize_topic(topic)
    normalized_risk = minigame_service.normalize_risk(risk)

    try:
        normalized_minigame = learning_content_service.normalize_minigame(minigame)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    return learning_content_service.get_learning_content(
        normalized_topic,
        normalized_risk,
        normalized_minigame
    )


@router.post(
    "/session",
    response_model=schemas.MinigameSessionResponse,
    summary="Crear sesion pedagogica de minijuego",
    description=(
        "Selecciona los items del minijuego y construye la microleccion "
        "con los concept_id exactos evaluados en esa sesion."
    ),
)
def create_minigame_session(
    request: schemas.MinigameSessionRequest,
    current_user=Depends(get_current_user),
    db: Session = Depends(get_db),
):
    try:
        return minigame_session_service.create_minigame_session(
            topic=request.topic,
            risk=request.risk,
            minigame=request.minigame,
            db=db,
            user_id=current_user.id,
        )
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except KeyError as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc
    except IntegrityError as exc:
        raise HTTPException(
            status_code=409,
            detail="Minigame session could not be persisted."
        ) from exc


@router.post(
    "/attempts",
    response_model=schemas.MinigameAttemptResponse,
    summary="Registrar intento individual de un item de minijuego",
    description=(
        "Guarda el resultado de un item contestado y deriva conceptos y "
        "dificultad desde los bancos del backend."
    ),
)
def record_minigame_attempt(
    request: schemas.MinigameAttemptRequest,
    current_user=Depends(get_current_user),
    db: Session = Depends(get_db),
):
    try:
        return minigame_session_service.record_minigame_attempt(
            db=db,
            user_id=current_user.id,
            request=request,
        )
    except minigame_session_service.MinigameSessionNotFoundError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except minigame_session_service.MinigameSessionValidationError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except minigame_session_service.MinigameSessionConflictError as exc:
        raise HTTPException(status_code=409, detail=str(exc)) from exc


@router.post(
    "/session/{session_id}/complete",
    response_model=schemas.MinigameSessionSummaryResponse,
    summary="Cerrar sesion pedagogica de minijuego",
    description=(
        "Marca la sesion como completada y devuelve un resumen calculado "
        "desde los intentos persistidos."
    ),
)
def complete_minigame_session(
    session_id: str,
    current_user=Depends(get_current_user),
    db: Session = Depends(get_db),
):
    try:
        return minigame_session_service.complete_minigame_session(
            db=db,
            user_id=current_user.id,
            session_id=session_id,
        )
    except minigame_session_service.MinigameSessionNotFoundError as exc:
        raise HTTPException(status_code=404, detail=str(exc)) from exc
    except minigame_session_service.MinigameSessionValidationError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except minigame_session_service.MinigameSessionConflictError as exc:
        raise HTTPException(status_code=409, detail=str(exc)) from exc


@router.get(
    "/mastery",
    response_model=schemas.ConceptMasteryListResponse,
    summary="Consultar dominio pedagogico por concepto",
    description=(
        "Devuelve el dominio del usuario autenticado por concepto evaluado "
        "en minijuegos."
    ),
)
def get_concept_mastery(
    topic: Optional[str] = Query(None),
    include_unpracticed: bool = Query(True),
    current_user=Depends(get_current_user),
    db: Session = Depends(get_db),
):
    try:
        return concept_mastery_service.get_user_mastery(
            db=db,
            user_id=current_user.id,
            topic=topic,
            include_unpracticed=include_unpracticed,
        )
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/crossword")
def get_crossword(
    topic: str = Query("phishing"),
    risk: str = Query("alto"),
    current_user=Depends(get_current_user)
):
    return minigame_service.get_crossword(topic, risk)


@router.get("/wordsearch")
def get_wordsearch(
    topic: str = Query("phishing"),
    risk: str = Query("alto"),
    current_user=Depends(get_current_user)
):
    return minigame_service.get_wordsearch(topic, risk)
