from datetime import datetime, timezone
import uuid

from app.services import concept_mastery_service
from app.services import minigame_service
from app.services.concept_catalog import get_concept


PERFORMANCE_MESSAGES = {
    "sin_evidencia": {
        "title": "Sesión completada",
        "message": "No se registraron respuestas suficientes para evaluar tu desempeño.",
        "next_step": (
            "Realiza nuevamente la actividad para obtener una recomendación "
            "personalizada."
        ),
    },
    "excelente": {
        "title": "Excelente trabajo",
        "message": "Mostraste un desempeño sólido en los conceptos practicados.",
    },
    "buen_progreso": {
        "title": "Buen progreso",
        "message": (
            "Comprendes la mayoría de los conceptos, aunque todavía puedes "
            "reforzar algunos puntos."
        ),
    },
    "en_desarrollo": {
        "title": "Continúa practicando",
        "message": (
            "Estás desarrollando estos conocimientos y la práctica adicional "
            "te ayudará a consolidarlos."
        ),
    },
    "necesita_refuerzo": {
        "title": "Refuerza los conceptos clave",
        "message": (
            "Algunos conceptos todavía presentan dificultad. Repásalos antes "
            "de la siguiente actividad."
        ),
    },
}

CONCEPT_STATUS_PRIORITY = {
    "refuerzo": 0,
    "dificultad_puntual": 1,
    "avance": 2,
    "fortaleza": 3,
}

ROTATION_ORDER = {
    "quiz": ("wordsearch", "crossword"),
    "wordsearch": ("crossword", "quiz"),
    "crossword": ("quiz", "wordsearch"),
}


class FeedbackSessionNotFoundError(Exception):
    pass


class FeedbackSessionConflictError(Exception):
    pass


class FeedbackValidationError(Exception):
    pass


def get_performance_level(accuracy, total_attempts):
    if total_attempts == 0:
        return "sin_evidencia"
    if accuracy >= 85:
        return "excelente"
    if accuracy >= 70:
        return "buen_progreso"
    if accuracy >= 50:
        return "en_desarrollo"
    return "necesita_refuerzo"


def get_minigame_feedback(db, user_id: int, session_id: str) -> dict:
    _validate_uuid(session_id)
    session = _get_completed_owned_session(db, user_id, session_id)
    attempts = _get_session_attempts(db, user_id, session.id)
    summary = _build_attempt_summary(attempts)
    performance_level = get_performance_level(
        summary["accuracy"],
        summary["total_attempts"],
    )
    concept_feedback = _build_concept_feedback(db, user_id, attempts)
    strengths = [
        feedback
        for feedback in concept_feedback
        if feedback["status"] in ("fortaleza", "avance")
    ]
    reinforcement = [
        feedback
        for feedback in concept_feedback
        if feedback["status"] in ("refuerzo", "dificultad_puntual")
    ]
    recommended_concept_ids = _select_recommended_concept_ids(
        concept_feedback,
        summary["total_attempts"],
    )

    return {
        "session_id": session.id,
        "topic": session.topic,
        "risk": session.risk,
        "minigame": session.minigame,
        **summary,
        "performance_level": performance_level,
        "title": PERFORMANCE_MESSAGES[performance_level]["title"],
        "message": PERFORMANCE_MESSAGES[performance_level]["message"],
        "next_step": _build_next_step(performance_level, concept_feedback),
        "strengths": strengths,
        "reinforcement": reinforcement,
        "recommended_concept_ids": recommended_concept_ids,
        "recommended_topic": session.topic,
        "recommended_minigame": get_recommended_minigame(
            topic=session.topic,
            risk=session.risk,
            current_minigame=session.minigame,
            recommended_concept_ids=recommended_concept_ids,
        ),
        "generated_at": _generated_at(),
    }


def classify_concept_feedback(mastery_score, session_correct, session_incorrect):
    if mastery_score < 50 or session_incorrect > session_correct:
        return "refuerzo"
    if mastery_score >= 50 and session_incorrect > 0:
        return "dificultad_puntual"
    if session_correct > session_incorrect and mastery_score < 75:
        return "avance"
    return "fortaleza"


def get_recommended_minigame(
    topic,
    risk,
    current_minigame,
    recommended_concept_ids,
):
    candidates = ROTATION_ORDER[current_minigame]

    if not recommended_concept_ids:
        return candidates[0]

    for minigame in candidates:
        if _minigame_has_relevant_content(
            topic,
            risk,
            minigame,
            recommended_concept_ids,
        ):
            return minigame

    return current_minigame


def _get_completed_owned_session(db, user_id: int, session_id: str):
    models = _models()
    session = (
        db.query(models.MinigameSessionRecord)
        .filter(
            models.MinigameSessionRecord.id == session_id,
            models.MinigameSessionRecord.user_id == user_id,
        )
        .first()
    )

    if session is None:
        raise FeedbackSessionNotFoundError("Minigame session not found.")

    if session.status != "completed":
        raise FeedbackSessionConflictError(
            "Minigame session must be completed before feedback is available."
        )

    return session


def _get_session_attempts(db, user_id: int, session_id: str) -> list:
    models = _models()
    return (
        db.query(models.MinigameAttempt)
        .filter(
            models.MinigameAttempt.session_id == session_id,
            models.MinigameAttempt.user_id == user_id,
        )
        .order_by(
            models.MinigameAttempt.created_at,
            models.MinigameAttempt.id,
        )
        .all()
    )


def _build_attempt_summary(attempts: list) -> dict:
    total_attempts = len(attempts)
    correct_attempts = sum(1 for attempt in attempts if attempt.correct)
    incorrect_attempts = total_attempts - correct_attempts

    return {
        "accuracy": 0 if total_attempts == 0 else round(
            correct_attempts / total_attempts * 100,
            2,
        ),
        "points_earned": sum(attempt.points_delta for attempt in attempts),
        "total_attempts": total_attempts,
        "correct_attempts": correct_attempts,
        "incorrect_attempts": incorrect_attempts,
    }


def _build_concept_feedback(db, user_id: int, attempts: list) -> list:
    grouped = _group_attempts_by_concept(attempts)
    feedback = []

    for concept_id, stats in grouped.items():
        concept = get_concept(concept_id)
        mastery = concept_mastery_service.get_user_concept_mastery(
            db,
            user_id,
            concept_id,
        )
        status = classify_concept_feedback(
            mastery["mastery_score"],
            stats["session_correct"],
            stats["session_incorrect"],
        )
        feedback.append(
            {
                "concept_id": concept_id,
                "term": concept["term"],
                "mastery_score": mastery["mastery_score"],
                "mastery_level": mastery["mastery_level"],
                "session_attempts": stats["session_attempts"],
                "session_correct": stats["session_correct"],
                "session_incorrect": stats["session_incorrect"],
                "status": status,
                "message": _concept_message(
                    status,
                    concept["term"],
                ),
                "recommendation": _concept_recommendation(concept),
            }
        )

    feedback.sort(
        key=lambda item: (
            CONCEPT_STATUS_PRIORITY[item["status"]],
            item["mastery_score"],
            -item["session_incorrect"],
            item["concept_id"],
        )
    )
    return feedback


def _group_attempts_by_concept(attempts: list) -> dict:
    grouped = {}

    for attempt in attempts:
        for concept_id in _unique_preserving_order(attempt.concept_ids):
            get_concept(concept_id)
            stats = grouped.setdefault(
                concept_id,
                {
                    "session_attempts": 0,
                    "session_correct": 0,
                    "session_incorrect": 0,
                },
            )
            stats["session_attempts"] += 1

            if attempt.correct:
                stats["session_correct"] += 1
            else:
                stats["session_incorrect"] += 1

    return grouped


def _select_recommended_concept_ids(
    concept_feedback: list,
    total_attempts: int,
) -> list:
    if total_attempts == 0:
        return []

    reinforcement = [
        item
        for item in concept_feedback
        if item["status"] in ("refuerzo", "dificultad_puntual")
    ]

    if reinforcement:
        return _unique_limited_concept_ids(
            sorted(
                reinforcement,
                key=lambda item: (
                    item["mastery_score"],
                    -item["session_incorrect"],
                    item["concept_id"],
                ),
            ),
            limit=3,
        )

    progress = [
        item
        for item in concept_feedback
        if item["status"] == "avance"
    ]
    return _unique_limited_concept_ids(
        sorted(
            progress,
            key=lambda item: (
                item["mastery_score"],
                item["concept_id"],
            ),
        ),
        limit=2,
    )


def _build_next_step(performance_level: str, concept_feedback: list) -> str:
    if performance_level == "sin_evidencia":
        return PERFORMANCE_MESSAGES[performance_level]["next_step"]

    recommended = _select_recommended_concept_ids(concept_feedback, 1)

    if not recommended:
        return "Continúa practicando para mantener y fortalecer tu aprendizaje."

    concept = get_concept(recommended[0])
    return f"Practica nuevamente {concept['term']}: {concept['recognition_clue']}"


def _concept_message(status: str, term: str) -> str:
    if status == "fortaleza":
        return f"Demostraste un dominio consistente de {term}."
    if status == "avance":
        return (
            f"Respondiste correctamente sobre {term}; continúa practicándolo "
            "para consolidar el aprendizaje."
        )
    if status == "refuerzo":
        return f"{term} necesita refuerzo según tus resultados recientes."
    return (
        f"Tu dominio general de {term} es favorable, pero esta sesión mostró "
        "una dificultad puntual."
    )


def _concept_recommendation(concept: dict) -> str:
    return concept["recognition_clue"]


def _minigame_has_relevant_content(
    topic,
    risk,
    minigame,
    recommended_concept_ids,
) -> bool:
    expected = set(recommended_concept_ids)

    for item in _get_bank(topic, risk, minigame):
        if expected.intersection(minigame_service.get_item_concept_ids(item)):
            return True

    return False


def _get_bank(topic, risk, minigame):
    if minigame == "quiz":
        return minigame_service.get_quiz(topic, risk)
    if minigame == "wordsearch":
        return minigame_service.get_wordsearch(topic, risk)
    return minigame_service.get_crossword(topic, risk)


def _unique_preserving_order(values) -> list:
    unique_values = []
    seen = set()

    for value in values or []:
        if value in seen:
            continue

        unique_values.append(value)
        seen.add(value)

    return unique_values


def _unique_limited_concept_ids(items: list, limit: int) -> list:
    concept_ids = []
    seen = set()

    for item in items:
        concept_id = item["concept_id"]

        if concept_id in seen:
            continue

        concept_ids.append(concept_id)
        seen.add(concept_id)

        if len(concept_ids) == limit:
            break

    return concept_ids


def _generated_at() -> str:
    return (
        datetime.now(timezone.utc)
        .replace(microsecond=0)
        .isoformat()
        .replace("+00:00", "Z")
    )


def _validate_uuid(value: str):
    try:
        uuid.UUID(str(value))
    except ValueError as exc:
        raise FeedbackValidationError(
            "session_id must be a valid UUID."
        ) from exc


def _models():
    from app import models

    return models
