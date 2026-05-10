from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session
from sqlalchemy import func
from datetime import datetime, timedelta

from app.database import get_db
from app.crud.crud_leaderboard import get_leaderboard
from app.schemas import LeaderboardUser
from app import models   


from app.auth import get_current_user



from fastapi import Depends
from sqlalchemy.orm import Session
from app.database import get_db
from app.models import User
from app.routes.auth import get_current_user


import logging
logger = logging.getLogger("protectpyme")

router = APIRouter(
    prefix="/leaderboard",
    tags=["leaderboard"]
)

#  GLOBAL
@router.get("/", response_model=list[LeaderboardUser])
def read_leaderboard(db: Session = Depends(get_db)):
    logger.info("Global leaderboard accessed")
    return get_leaderboard(db)


# POR CATEGORÍA
@router.get("/category/{category}", response_model=list[LeaderboardUser])
def leaderboard_by_category(category: str, db: Session = Depends(get_db)):

    results = (
        db.query(
            models.User.id,
            models.User.name,
            models.UserCategoryPoints.total_points
        )
        .join(models.UserCategoryPoints,
              models.User.id == models.UserCategoryPoints.user_id)
        .filter(models.UserCategoryPoints.category == category)
        .order_by(models.UserCategoryPoints.total_points.desc())
        .all()
    )

    leaderboard = []

    for index, row in enumerate(results, start=1):
        leaderboard.append(
            LeaderboardUser(
                rank=index,
                id=row.id,
                name=row.name,
                total_points=row.total_points
            )
        )
    logger.info(f"Leaderboard by category accessed: {category}")
    return leaderboard
""" @router.get("/category/{category}", response_model=list[LeaderboardUser])
def leaderboard_by_category(category: str, db: Session = Depends(get_db)):

    results = (
        db.query(
            models.User.id,
            models.User.name,
            func.sum(models.Decision.points_awarded).label("points")
        )
        .join(models.Decision)
        .join(models.Scenario)
        .filter(models.Scenario.category == category)
        .group_by(models.User.id)
        .order_by(func.sum(models.Decision.points_awarded).desc())
        .all()
    )

    leaderboard = []

    for index, row in enumerate(results, start=1):
        leaderboard.append(
            LeaderboardUser(
                rank=index,
                id=row.id,
                name=row.name,
                total_points=row.points or 0
            )
        )

    return leaderboard """


""" @router.get("/category/{category}")
def leaderboard_by_category(category: str, db: Session = Depends(get_db)):

    results = (
        db.query(
            models.User.id,
            models.User.name,
            func.sum(models.Decision.points_awarded).label("points")
        )
        .join(models.Decision)
        .join(models.Scenario)
        .filter(models.Scenario.category == category)
        .group_by(models.User.id)
        .order_by(func.sum(models.Decision.points_awarded).desc())
        .all()
    )

    return results """


#  SEMANAL
@router.get("/weekly", response_model=list[LeaderboardUser])
def weekly_leaderboard(db: Session = Depends(get_db)):

    week_ago = datetime.utcnow() - timedelta(days=7)

    results = (
        db.query(
            models.User.id,
            models.User.name,
            func.sum(models.Decision.points_awarded).label("points")
        )
        .join(models.Decision)
        .filter(models.Decision.created_at >= week_ago)
        .group_by(models.User.id)
        .order_by(func.sum(models.Decision.points_awarded).desc())
        .all()
    )

    leaderboard = []

    for index, row in enumerate(results, start=1):
        leaderboard.append(
            LeaderboardUser(
                rank=index,
                id=row.id,
                name=row.name,
                total_points=row.points or 0
            )
        )
    logger.info("Weekly leaderboard accessed")
    return leaderboard
""" @router.get("/weekly")
def weekly_leaderboard(db: Session = Depends(get_db)):

    week_ago = datetime.utcnow() - timedelta(days=7)

    results = (
        db.query(
            models.User.id,
            models.User.name,
            func.sum(models.Decision.points_awarded).label("points")
        )
        .join(models.Decision)
        .filter(models.Decision.created_at >= week_ago)
        .group_by(models.User.id)
        .order_by(func.sum(models.Decision.points_awarded).desc())
        .all()
    )

    return results
 """

@router.get("/me")
def my_rank(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
    
):
    # 🔹 Ranking global
    users = db.query(models.User).order_by(
        models.User.total_points.desc()
    ).all()

    rank_global = next(
        (i+1 for i, u in enumerate(users) if u.id == current_user.id),
        None
    )

    # 🔹 Ranking semanal
    week_ago = datetime.utcnow() - timedelta(days=7)

    weekly_results = (
        db.query(
            models.User.id,
            func.sum(models.Decision.points_awarded).label("points")
        )
        .join(models.Decision)
        .filter(models.Decision.created_at >= week_ago)
        .group_by(models.User.id)
        .order_by(func.sum(models.Decision.points_awarded).desc())
        .all()
    )

    rank_weekly = next(
        (i+1 for i, row in enumerate(weekly_results) if row.id == current_user.id),
        None
    )
    logger.info(f"User {current_user.id} requested rank info")
    return {
        "rank_global": rank_global,
        "rank_weekly": rank_weekly,
        "total_points": current_user.total_points
    }


@router.post("/score")
def submit_score(score: int, db: Session = Depends(get_db), current_user: User = Depends(get_current_user)):
    
    current_user.total_points += score

    db.commit()
    db.refresh(current_user)

    return {
        "message": "Score actualizado",
        "total_points": current_user.total_points
    }