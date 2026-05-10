from sqlalchemy.orm import Session
from fastapi import HTTPException
from app import models, schemas
from app.ai.rules import evaluate_decision
from datetime import datetime, timedelta
import logging

logger = logging.getLogger("protectpyme")



def calculate_level(points: int) -> str:
    if points >= 1000:
        return "Gold"
    elif points >= 500:
        return "Silver"
    else:
        return "Bronze"


def create_decision(db: Session, decision: schemas.DecisionCreate, user: models.User):

    scenario = db.query(models.Scenario).filter(
        models.Scenario.id == decision.scenario_id
    ).first()

    # if not scenario:
    #     raise HTTPException(
    #         logger.warning(f"User {user.id} attempted decision on invalid scenario {decision.scenario_id}")
    #         status_code=404,
    #         detail="Scenario not found"
    #     )
    if not scenario:
        logger.warning(
            f"User {user.id} attempted decision on invalid scenario {decision.scenario_id}"
        )
        raise HTTPException(
            status_code=404,
            detail="Scenario not found"
        )
    #  Anti-spam
    check_rate_limit(db, user.id)
    # Validación de tiempo de respuesta
    MAX_RESPONSE_TIME = 120

    if decision.response_time is not None and decision.response_time < 0:
        raise HTTPException(
            status_code=400,
            detail="Invalid response time"
        )

    if decision.response_time is not None and decision.response_time > MAX_RESPONSE_TIME:
        raise HTTPException(
            status_code=400,
            detail="Response time unrealistic"
        )

    evaluation = evaluate_decision(scenario, decision.choice)

    try:
        db_decision = models.Decision(
            user_id=user.id,
            scenario_id=decision.scenario_id,
            choice=decision.choice,
            is_correct=1 if evaluation["is_correct"] else 0,
            points_awarded=evaluation["points_awarded"],
            risk_level=evaluation["risk_level"],
            feedback=evaluation["feedback"],
            response_time=decision.response_time
        )

        db.add(db_decision)

        #  Actualizar métricas
        user.total_points += evaluation["points_awarded"]
        user.total_decisions += 1

        if evaluation["is_correct"]:
            user.correct_decisions += 1

        #  Risk score acumulado
        if evaluation["risk_level"] == "high":
            user.risk_score += 3
        elif evaluation["risk_level"] == "medium":
            user.risk_score += 2
        elif evaluation["risk_level"] == "low":
            user.risk_score += 1

        #  Nivel automático
        user.level = calculate_level(user.total_points)

        #  Categorías
        update_user_category_points(
            db,
            user.id,
            scenario.category,
            evaluation["points_awarded"]
        )

        db.commit()
        db.refresh(db_decision)

        logger.info(f"User {user.id} created decision {db_decision.id}")

        return db_decision

    except Exception as e:
        db.rollback()
        logger.error(f"Error creating decision: {str(e)}")
        raise HTTPException(
            status_code=500,
            detail="Internal server error"
        )
"""     except Exception as e:
        db.rollback()
        print(" ERROR REAL:", str(e))
        raise HTTPException(
            status_code=500,
            detail=str(e)
        ) """
"""     except SQLAlchemyError:
        db.rollback()
        raise HTTPException(
            status_code=500,
            detail="Error creating decision"
        ) """

def update_user_category_points(db: Session, user_id: int, category: str, points: int):

    record = db.query(models.UserCategoryPoints).filter(
        models.UserCategoryPoints.user_id == user_id,
        models.UserCategoryPoints.category == category
    ).first()

    if record:
        record.total_points += points
    else:
        record = models.UserCategoryPoints(
            user_id=user_id,
            category=category,
            total_points=points
        )
        db.add(record)
    if points < 0:
        raise ValueError("Points cannot be negative")

def get_user_decisions(db: Session, user_id: int, skip: int = 0, limit: int = 20):
    logger.info(f"User {user_id} fetched decision history")
    return (
        db.query(models.Decision)
        .filter(models.Decision.user_id == user_id)
        .order_by(models.Decision.created_at.desc())
        .offset(skip)
        .limit(limit) #limit = min(limit, 100)
        .all()
        
    )

def check_rate_limit(db: Session, user_id: int, limit: int = 5):

    one_minute_ago = datetime.utcnow() - timedelta(minutes=1)
#eficiencia ya que count puede ser pesado en tablas grandes, seleccionar solo id reduce carga
    recent_decisions = (
    db.query(models.Decision.id)
    .filter(
        models.Decision.user_id == user_id,
        models.Decision.created_at >= one_minute_ago
    )
    .count()
)
    # recent_decisions = (
    #     db.query(models.Decision)
    #     .filter(
    #         models.Decision.user_id == user_id,
    #         models.Decision.created_at >= one_minute_ago
    #     )
    #     .count()
    # )

    #logging de intento de abusos, para detectr comportamiento sospechosos
    if recent_decisions >= limit:
        logger.warning(f"Rate limit exceeded by user {user_id}")
        raise HTTPException(
            status_code=429,
            detail="Too many decisions. Please wait a minute."
    )

"""     if recent_decisions >= limit:
        raise HTTPException(
            status_code=429,
            detail="Too many decisions. Please wait a minute."
        ) """
""" def get_user_decisions(db: Session, user_id: int):
    return (
        db.query(models.Decision)
        .filter(models.Decision.user_id == user_id)
        .order_by(models.Decision.created_at.desc())
        .all()
    )
 """

""" from sqlalchemy.orm import Session
from app import models, schemas
from app.ai.rules import evaluate_decision


def create_decision(db: Session, decision: schemas.DecisionCreate, user: models.User):

    scenario = db.query(models.Scenario).filter(
        models.Scenario.id == decision.scenario_id
    ).first()

    if not scenario:
        return None

    evaluation = evaluate_decision(scenario, decision.choice)

    db_decision = models.Decision(
        user_id=user.id,
        scenario_id=decision.scenario_id,
        choice=decision.choice,
        is_correct=1 if evaluation["is_correct"] else 0,
        points_awarded=evaluation["points_awarded"],
        risk_level=evaluation["risk_level"],
        feedback=evaluation["feedback"],
        response_time=decision.response_time
    )

    user.total_points += evaluation["points_awarded"]

    db.add(db_decision)
    db.commit()
    db.refresh(db_decision)

    return db_decision


def get_decisions(db: Session):
    return db.query(models.Decision).all()


def get_user_decisions(db: Session, user_id: int):
    return db.query(models.Decision).filter(
        models.Decision.user_id == user_id
    ).all() """