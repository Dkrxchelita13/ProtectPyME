import copy
import hashlib

from sqlalchemy import desc

from app.services import minigame_service


SESSION_ITEM_LIMITS = {
    "quiz": 3,
    "wordsearch": 3,
    "crossword": 3,
}

UNPRACTICED_WEAKNESS = 0.55
REINFORCEMENT_BONUS = 0.35
EXPLORATION_BONUS = 0.20
RECENT_SESSION_PENALTIES = (0.60, 0.35, 0.20)


def select_adaptive_items(
    db,
    user_id: int,
    topic: str,
    risk: str,
    minigame: str,
    candidates: list,
    session_id: str,
    limit: int,
) -> list:
    if not candidates:
        return []

    mastery_by_concept = _get_mastery_by_concept(db, user_id)
    recent_penalties = _get_recent_item_penalties(
        db=db,
        user_id=user_id,
        topic=topic,
        risk=risk,
        minigame=minigame,
    )

    exact_candidates = [
        item
        for item in candidates
        if _candidate_matches_context(item, topic, risk, minigame)
    ]

    scored_items = [
        _score_candidate(
            item=item,
            user_id=user_id,
            session_id=session_id,
            mastery_by_concept=mastery_by_concept,
            recent_penalties=recent_penalties,
        )
        for item in exact_candidates
    ]
    scored_items.sort(
        key=lambda scored: (
            -scored["selection_score"],
            scored["tie_breaker"],
            scored["item"]["item_id"],
        )
    )

    selected = scored_items
    if len(scored_items) > limit:
        selected = scored_items[:limit]

    return [
        copy.deepcopy(scored["item"])
        for scored in selected
    ]


def get_session_item_limit(minigame: str) -> int:
    try:
        return SESSION_ITEM_LIMITS[minigame]
    except KeyError as exc:
        raise ValueError("Minigame must be quiz, wordsearch or crossword.") from exc


def _score_candidate(
    item: dict,
    user_id: int,
    session_id: str,
    mastery_by_concept: dict,
    recent_penalties: dict,
) -> dict:
    _validate_candidate(item)
    concept_ids = minigame_service.get_item_concept_ids(item)
    weakness_values = [
        _concept_weakness(concept_id, mastery_by_concept)
        for concept_id in concept_ids
    ]
    has_weak_practiced_concept = any(
        _is_practiced_weak_concept(concept_id, mastery_by_concept)
        for concept_id in concept_ids
    )
    has_unpracticed_concept = any(
        concept_id not in mastery_by_concept
        for concept_id in concept_ids
    )
    recent_penalty = recent_penalties.get(item["item_id"], 0)
    selection_score = (
        max(weakness_values)
        + (REINFORCEMENT_BONUS if has_weak_practiced_concept else 0)
        + (EXPLORATION_BONUS if has_unpracticed_concept else 0)
        - recent_penalty
    )

    return {
        "item": copy.deepcopy(item),
        "selection_score": selection_score,
        "tie_breaker": _stable_tie_breaker(session_id, user_id, item["item_id"]),
    }


def _candidate_matches_context(
    item: dict,
    topic: str,
    risk: str,
    minigame: str,
) -> bool:
    return (
        item.get("topic", topic) == topic
        and item.get("risk", risk) == risk
        and item.get("minigame", minigame) == minigame
        and item.get("difficulty") == risk
    )


def _validate_candidate(item: dict):
    if not item.get("item_id"):
        raise ValueError("Minigame candidate has no item_id.")

    if not item.get("difficulty"):
        raise ValueError(f"Minigame candidate has no difficulty: {item['item_id']}")

    minigame_service.get_item_concept_ids(item)


def _concept_weakness(concept_id: str, mastery_by_concept: dict) -> float:
    mastery = mastery_by_concept.get(concept_id)

    if mastery is None:
        return UNPRACTICED_WEAKNESS

    return 1 - (mastery.mastery_score / 100)


def _is_practiced_weak_concept(concept_id: str, mastery_by_concept: dict) -> bool:
    mastery = mastery_by_concept.get(concept_id)
    return mastery is not None and mastery.mastery_score < 50


def _get_mastery_by_concept(db, user_id: int) -> dict:
    if db is None or user_id is None:
        return {}

    models = _models()
    records = (
        db.query(models.UserConceptMastery)
        .filter(models.UserConceptMastery.user_id == user_id)
        .all()
    )

    return {
        record.concept_id: record
        for record in records
    }


def _get_recent_item_penalties(
    db,
    user_id: int,
    topic: str,
    risk: str,
    minigame: str,
) -> dict:
    if db is None or user_id is None:
        return {}

    models = _models()
    recent_sessions = (
        db.query(models.MinigameSessionRecord)
        .filter(
            models.MinigameSessionRecord.user_id == user_id,
            models.MinigameSessionRecord.topic == topic,
            models.MinigameSessionRecord.risk == risk,
            models.MinigameSessionRecord.minigame == minigame,
            models.MinigameSessionRecord.status == "completed",
        )
        .order_by(
            desc(models.MinigameSessionRecord.completed_at),
            desc(models.MinigameSessionRecord.started_at),
        )
        .limit(len(RECENT_SESSION_PENALTIES))
        .all()
    )

    penalties = {}

    for index, session in enumerate(recent_sessions):
        penalty = RECENT_SESSION_PENALTIES[index]

        for item_id in session.item_ids or []:
            penalties.setdefault(item_id, penalty)

    return penalties


def _stable_tie_breaker(session_id: str, user_id: int, item_id: str) -> str:
    raw = f"{session_id}{user_id}{item_id}".encode("utf-8")
    return hashlib.sha256(raw).hexdigest()


def _models():
    from app import models

    return models
