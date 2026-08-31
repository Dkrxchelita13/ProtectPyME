from datetime import datetime

from app.services.concept_catalog import CONCEPT_CATALOG, VALID_TOPICS, get_concept


INITIAL_ALPHA = 2.0
INITIAL_BETA = 2.0
INITIAL_MASTERY_SCORE = 50.0

DIFFICULTY_WEIGHTS = {
    "bajo": 1.0,
    "medio": 1.25,
    "alto": 1.5,
}

MASTERY_LEVEL_PRIORITY = {
    "necesita_refuerzo": 0,
    "en_desarrollo": 1,
    "dominado": 2,
    "sin_datos": 3,
}


def get_difficulty_weight(difficulty: str) -> float:
    normalized = str(difficulty or "").strip().lower()

    if normalized not in DIFFICULTY_WEIGHTS:
        raise ValueError("difficulty must be bajo, medio or alto.")

    return DIFFICULTY_WEIGHTS[normalized]


def get_mastery_level(score: float, attempt_count: int) -> str:
    if attempt_count == 0:
        return "sin_datos"
    if score < 50:
        return "necesita_refuerzo"
    if score < 75:
        return "en_desarrollo"
    return "dominado"


def update_user_mastery_from_attempts(db, user_id: int, attempts: list) -> list:
    if not attempts:
        return []

    updated_records = []
    updated_at = datetime.utcnow()
    records_by_concept = {}

    for attempt in attempts:
        weight = get_difficulty_weight(attempt.difficulty)

        for concept_id in _unique_concept_ids(attempt.concept_ids):
            concept = get_concept(concept_id)
            record = _get_or_create_mastery_record(
                db=db,
                user_id=user_id,
                concept_id=concept_id,
                topic=concept["topic"],
                now=updated_at,
                records_by_concept=records_by_concept,
            )

            if attempt.correct:
                record.alpha += weight
                record.correct_count += 1
            else:
                record.beta += weight
                record.incorrect_count += 1

            record.attempt_count += 1
            record.evidence_weight = round(record.evidence_weight + weight, 2)
            record.mastery_score = _calculate_mastery_score(
                record.alpha,
                record.beta,
            )
            record.last_practiced_at = attempt.created_at
            record.updated_at = updated_at
            updated_records.append(record)

    return updated_records


def get_user_mastery(
    db,
    user_id: int,
    topic: str = None,
    include_unpracticed: bool = True,
) -> dict:
    normalized_topic = _normalize_topic_filter(topic)
    concepts = _catalog_concepts(normalized_topic)
    existing = _get_existing_mastery_by_concept(db, user_id, normalized_topic)

    if include_unpracticed:
        responses = [
            _concept_response(concept, existing.get(concept["concept_id"]))
            for concept in concepts
        ]
    else:
        responses = [
            _concept_response(CONCEPT_CATALOG[concept_id], record)
            for concept_id, record in existing.items()
            if concept_id in CONCEPT_CATALOG
        ]

    responses.sort(
        key=lambda item: (
            MASTERY_LEVEL_PRIORITY[item["mastery_level"]],
            item["mastery_score"],
            item["concept_id"],
        )
    )

    return _mastery_list_response(normalized_topic, responses)


def get_user_concept_mastery(db, user_id: int, concept_id: str) -> dict:
    concept = get_concept(concept_id)
    models = _models()
    record = (
        db.query(models.UserConceptMastery)
        .filter(
            models.UserConceptMastery.user_id == user_id,
            models.UserConceptMastery.concept_id == concept_id,
        )
        .first()
    )

    return _concept_response(concept, record)


def _get_or_create_mastery_record(
    db,
    user_id: int,
    concept_id: str,
    topic: str,
    now: datetime,
    records_by_concept: dict = None,
):
    if records_by_concept is not None and concept_id in records_by_concept:
        return records_by_concept[concept_id]

    models = _models()
    record = (
        db.query(models.UserConceptMastery)
        .filter(
            models.UserConceptMastery.user_id == user_id,
            models.UserConceptMastery.concept_id == concept_id,
        )
        .first()
    )

    if record is not None:
        if records_by_concept is not None:
            records_by_concept[concept_id] = record
        return record

    record = models.UserConceptMastery(
        user_id=user_id,
        concept_id=concept_id,
        topic=topic,
        alpha=INITIAL_ALPHA,
        beta=INITIAL_BETA,
        mastery_score=INITIAL_MASTERY_SCORE,
        attempt_count=0,
        correct_count=0,
        incorrect_count=0,
        evidence_weight=0.0,
        created_at=now,
        updated_at=now,
    )
    db.add(record)
    if records_by_concept is not None:
        records_by_concept[concept_id] = record
    return record


def _get_existing_mastery_by_concept(db, user_id: int, topic: str = None) -> dict:
    models = _models()
    query = db.query(models.UserConceptMastery).filter(
        models.UserConceptMastery.user_id == user_id
    )

    if topic is not None:
        query = query.filter(models.UserConceptMastery.topic == topic)

    return {
        record.concept_id: record
        for record in query.all()
    }


def _unique_concept_ids(concept_ids) -> list:
    unique_ids = []
    seen = set()

    for concept_id in concept_ids or []:
        if concept_id not in seen:
            unique_ids.append(concept_id)
            seen.add(concept_id)

    return unique_ids


def _calculate_mastery_score(alpha: float, beta: float) -> float:
    return round(alpha / (alpha + beta) * 100, 2)


def _normalize_topic_filter(topic: str):
    if topic is None:
        return None

    normalized = topic.strip().lower()

    if normalized not in VALID_TOPICS:
        raise ValueError("topic must be phishing, passwords, malware or wifi.")

    return normalized


def _catalog_concepts(topic: str = None) -> list:
    return [
        concept
        for concept in CONCEPT_CATALOG.values()
        if topic is None or concept["topic"] == topic
    ]


def _concept_response(concept: dict, record) -> dict:
    if record is None:
        return {
            "concept_id": concept["concept_id"],
            "topic": concept["topic"],
            "term": concept["term"],
            "mastery_score": INITIAL_MASTERY_SCORE,
            "mastery_level": get_mastery_level(INITIAL_MASTERY_SCORE, 0),
            "attempt_count": 0,
            "correct_count": 0,
            "incorrect_count": 0,
            "evidence_weight": 0.0,
            "last_practiced_at": None,
            "updated_at": None,
        }

    return {
        "concept_id": record.concept_id,
        "topic": record.topic,
        "term": concept["term"],
        "mastery_score": record.mastery_score,
        "mastery_level": get_mastery_level(
            record.mastery_score,
            record.attempt_count,
        ),
        "attempt_count": record.attempt_count,
        "correct_count": record.correct_count,
        "incorrect_count": record.incorrect_count,
        "evidence_weight": record.evidence_weight,
        "last_practiced_at": record.last_practiced_at,
        "updated_at": record.updated_at,
    }


def _mastery_list_response(topic: str, concepts: list) -> dict:
    return {
        "topic_filter": topic,
        "total_concepts": len(concepts),
        "practiced_concepts": sum(
            1 for concept in concepts if concept["attempt_count"] > 0
        ),
        "needs_reinforcement_count": sum(
            1
            for concept in concepts
            if concept["mastery_level"] == "necesita_refuerzo"
        ),
        "developing_count": sum(
            1
            for concept in concepts
            if concept["mastery_level"] == "en_desarrollo"
        ),
        "mastered_count": sum(
            1 for concept in concepts if concept["mastery_level"] == "dominado"
        ),
        "concepts": concepts,
    }


def _models():
    from app import models

    return models
