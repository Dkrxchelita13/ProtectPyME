from fastapi import APIRouter, Depends
from app.services import minigame_service
from app.routes.auth import get_current_user

router = APIRouter(prefix="/minigames", tags=["Minigames"])


@router.get("/words")
def get_words(current_user = Depends(get_current_user)):
    return minigame_service.get_words()


@router.get("/quiz")
def get_quiz(current_user = Depends(get_current_user)):
    return minigame_service.get_quiz()


@router.get("/crossword")
def get_crossword(current_user = Depends(get_current_user)):
    return minigame_service.get_crossword()