from fastapi import APIRouter, Depends, Query
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