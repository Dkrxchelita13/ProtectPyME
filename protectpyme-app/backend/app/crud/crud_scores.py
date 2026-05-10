from sqlalchemy.orm import Session
from app import models, schemas

def create_score(db: Session, score: schemas.ScoreCreate):
    db_score = models.Score(**score.model_dump())
    db.add(db_score)
    db.commit()
    db.refresh(db_score)
    return db_score

def get_scores(db: Session):
    return db.query(models.Score).all()

def get_user_scores(db: Session, user_id: int):
    return db.query(models.Score).filter(models.Score.user_id == user_id).all()
