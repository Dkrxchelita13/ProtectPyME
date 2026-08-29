from sqlalchemy.orm import Session
from sqlalchemy import func
from datetime import datetime, timedelta
from app import models
from app.services.topic_taxonomy import CANONICAL_TOPICS, normalize_topic

import logging
logger = logging.getLogger("protectpyme")


def calculate_awareness_score(user):

    if user.total_decisions == 0:
        return 0

    accuracy = user.correct_decisions / user.total_decisions

    score = (
        (accuracy * 70) +
        (user.total_points / 1000 * 20) +
        (max(0, 100 - user.risk_score) * 0.1)
    )

    return round(min(score, 100), 2)


def calculate_risk_index(db: Session, user_id: int):

    user = db.query(models.User).filter(
        models.User.id == user_id
    ).first()

    if not user:
        logger.warning(f"Risk index requested for non-existing user {user_id}")
        return 0

    if user.total_decisions == 0:
        return 0

    error_rate = (
        (user.total_decisions - user.correct_decisions)
        / user.total_decisions
    )

    risk_factor = user.risk_score * 0.5

    risk_index = (error_rate * 100) + risk_factor

    return round(risk_index, 2)


def get_user_analytics(db: Session, user_id: int):

    user = db.query(models.User).filter(
        models.User.id == user_id
    ).first()

    if not user:
        logger.warning(f"Analytics requested for non-existing user {user_id}")
        return {}

    accuracy = 0

    if user.total_decisions > 0:
        accuracy = round(
            (user.correct_decisions / user.total_decisions) * 100,
            2
        )

    # detectar usuario vulnerable
    high_risk_user = False

    if user.total_decisions > 10 and accuracy < 40:
        high_risk_user = True
        logger.warning(f"User {user_id} flagged as high phishing risk")

    # awareness score
    awareness_score = calculate_awareness_score(user)

    logger.info("Analytics summary generated")

    risk_index = calculate_risk_index(db, user_id)

    # Últimos 7 días
    seven_days_ago = datetime.utcnow() - timedelta(days=7)

    decisions_last_7_days = (
        db.query(models.Decision)
        .filter(
            models.Decision.user_id == user_id,
            models.Decision.created_at >= seven_days_ago
        )
        .count()
    )

    # Categoría con más errores. Se normaliza al leer para conservar
    # decisiones historicas como network/password sin mutar la base.
    failed_categories = (
        db.query(
            models.Scenario.category,
            func.count(models.Decision.id).label("fail_count")
        )
        .join(models.Scenario)
        .filter(
            models.Decision.user_id == user_id,
            models.Decision.is_correct == 0
        )
        .group_by(models.Scenario.category)
        .all()
    )

    category_counts = {
        category: 0
        for category in CANONICAL_TOPICS
    }

    for category, fail_count in failed_categories:
        canonical_category = normalize_topic(category)

        if canonical_category in category_counts:
            category_counts[canonical_category] += fail_count

    most_failed_category = None

    if any(category_counts.values()):
        category_order = {
            category: index
            for index, category in enumerate(CANONICAL_TOPICS)
        }

        most_failed_category = min(
            CANONICAL_TOPICS,
            key=lambda category: (
                -category_counts[category],
                category_order[category],
            )
        )

    logger.info(f"Analytics generated for user {user_id}")
    
    return {
        "level": user.level,
        "total_points": user.total_points,
        "accuracy": accuracy,
        "risk_index": risk_index,
        "awareness_score": awareness_score,
        "high_risk_user": high_risk_user,
        "most_failed_category": most_failed_category,
        "decisions_last_7_days": decisions_last_7_days
    }

"""     return {
        "level": user.level,
        "accuracy": accuracy,
        "risk_index": risk_index,
        "awareness_score": awareness_score,
        "high_risk_user": high_risk_user,
        "most_failed_category": most_failed_category,
        "decisions_last_7_days": decisions_last_7_days
    } """
    

    
    
    
    
