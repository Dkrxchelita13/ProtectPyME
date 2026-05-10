from sqlalchemy.orm import Session
from app import models

def get_leaderboard(db: Session, limit: int = 10):
    users = (
        db.query(models.User)
        .order_by(models.User.total_points.desc())
        .limit(limit)
        .all()
    )

    leaderboard = []
    for i, user in enumerate(users, start=1):
        leaderboard.append({
            "rank": i,
            "id": user.id,
            "name": user.name,
            "total_points": user.total_points
        })

    return leaderboard
