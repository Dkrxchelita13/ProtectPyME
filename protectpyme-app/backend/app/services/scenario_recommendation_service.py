from sqlalchemy.orm import Session

from app import models
from app.services.topic_taxonomy import (
    FINAL_FALLBACK_TOPIC,
    PLAYABLE_SCENARIOS_BY_TOPIC,
    get_playable_scenarios,
    normalize_topic,
)


RECOMMENDATION_MESSAGES = {
    "phishing": "Practica detección de correos fraudulentos",
    "passwords": "Refuerza buenas prácticas de contraseñas",
    "malware": (
        "Refuerza tus conocimientos sobre malware y software malicioso"
    ),
    "wifi": "Aprende a protegerte al utilizar redes WiFi públicas",
}

NO_CRITICAL_AREA_MESSAGE = (
    "Excelente desempeño. No se detectaron áreas críticas de mejora."
)


def get_recommendation_for_topic(
    topic: str | None,
    db: Session | None = None,
    user_id: int | None = None,
    survey_primary_weakness: str | None = None,
    no_critical_area: bool = False,
) -> dict:
    canonical_topic = (
        normalize_topic(topic)
        or normalize_topic(survey_primary_weakness)
        or FINAL_FALLBACK_TOPIC
    )

    scenario_id = select_playable_scenario(
        db=db,
        user_id=user_id,
        topic=canonical_topic,
    )

    message = (
        NO_CRITICAL_AREA_MESSAGE
        if no_critical_area
        else RECOMMENDATION_MESSAGES[canonical_topic]
    )

    return {
        "training": canonical_topic,
        "scenario": scenario_id,
        "message": message,
    }


def select_playable_scenario(
    db: Session | None,
    user_id: int | None,
    topic: str | None,
) -> int:
    canonical_topic = normalize_topic(topic) or FINAL_FALLBACK_TOPIC
    candidates = get_playable_scenarios(canonical_topic)

    if not candidates:
        candidates = PLAYABLE_SCENARIOS_BY_TOPIC[FINAL_FALLBACK_TOPIC]

    if db is None or user_id is None:
        return min(candidates)

    return _select_from_user_history(
        db=db,
        user_id=user_id,
        candidates=candidates,
    )


def _select_from_user_history(
    db: Session,
    user_id: int,
    candidates: tuple[int, ...],
) -> int:
    counts = {scenario_id: 0 for scenario_id in candidates}

    decisions = (
        db.query(models.Decision)
        .filter(
            models.Decision.user_id == user_id,
            models.Decision.scenario_id.in_(candidates),
        )
        .order_by(
            models.Decision.created_at.desc(),
            models.Decision.id.desc(),
        )
        .all()
    )

    most_recent_scenario = decisions[0].scenario_id if decisions else None

    for decision in decisions:
        if decision.scenario_id in counts:
            counts[decision.scenario_id] += 1

    selectable_candidates = list(candidates)

    if len(selectable_candidates) > 1 and most_recent_scenario in counts:
        selectable_candidates = [
            scenario_id
            for scenario_id in selectable_candidates
            if scenario_id != most_recent_scenario
        ]

    return min(
        selectable_candidates,
        key=lambda scenario_id: (counts[scenario_id], scenario_id),
    )
