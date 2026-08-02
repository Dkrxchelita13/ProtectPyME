from fastapi import APIRouter, Depends, HTTPException, Query
from app import schemas
from app.services import learning_content_service
from app.services import minigame_service
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
