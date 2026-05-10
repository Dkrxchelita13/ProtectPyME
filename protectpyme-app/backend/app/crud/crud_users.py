#Tabla users
""" CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    email VARCHAR(100) UNIQUE,
    password VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
); """
from sqlalchemy.orm import Session
from sqlalchemy.exc import IntegrityError
from fastapi import HTTPException

from app import models, schemas
from app.auth import hash_password


def create_user(db: Session, user: schemas.UserCreate):

    # 🔍 Validación previa (mejor UX)
    existing_user = db.query(models.User).filter(
        models.User.email == user.email
    ).first()

    if existing_user:
        raise HTTPException(
            status_code=400,
            detail="Email already registered"
        )

    db_user = models.User(
        name=user.name,
        email=user.email,
        password=hash_password(user.password)
    )

    db.add(db_user)

    try:
        db.commit()
    except IntegrityError:
        db.rollback()
        raise HTTPException(
            status_code=400,
            detail="Email already registered"
        )

    db.refresh(db_user)
    return db_user


def get_user(db: Session, user_id: int):
    return db.query(models.User).filter(
        models.User.id == user_id
    ).first()


def get_users(db: Session):
    return db.query(models.User).all()


def delete_user(db: Session, user_id: int):
    user = db.query(models.User).filter(
        models.User.id == user_id
    ).first()

    if user:
        db.delete(user)
        db.commit()

    return user


#Tabla Scenarios
""" CREATE TABLE scenarios (
    id SERIAL PRIMARY KEY,
    title VARCHAR(150),
    description TEXT,
    risk_level VARCHAR(50)
); """

#Tabla decisions
""" CREATE TABLE decisions (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id),
    scenario_id INT REFERENCES scenarios(id),
    choice VARCHAR(100),
    risk_result VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
); """
#Tabla scores
""" CREATE TABLE scores (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id),
    total_points INT DEFAULT 0,
    level VARCHAR(50)
);
 """