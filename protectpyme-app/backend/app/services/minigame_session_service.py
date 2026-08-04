import copy
import uuid

from app.services import learning_content_service
from app.services import minigame_service
from app.services.concept_catalog import get_concepts


def create_minigame_session(topic: str, risk: str, minigame: str) -> dict:
    normalized_topic = minigame_service.normalize_topic(topic)
    normalized_risk = minigame_service.normalize_risk(risk)
    normalized_minigame = learning_content_service.normalize_minigame(minigame)

    items = _get_bank_items(
        normalized_topic,
        normalized_risk,
        normalized_minigame,
    )
    concept_ids = _extract_concept_ids(items)
    get_concepts(concept_ids)

    lesson = learning_content_service.get_learning_content_for_concepts(
        normalized_topic,
        normalized_risk,
        normalized_minigame,
        concept_ids,
    )

    return {
        "session_id": str(uuid.uuid4()),
        "topic": normalized_topic,
        "risk": normalized_risk,
        "minigame": normalized_minigame,
        "lesson": lesson,
        "items": [_normalize_session_item(item) for item in items],
    }


def _get_bank_items(topic: str, risk: str, minigame: str) -> list:
    if minigame == "quiz":
        items = minigame_service.get_quiz(topic, risk)
    elif minigame == "wordsearch":
        items = minigame_service.get_wordsearch(topic, risk)
    elif minigame == "crossword":
        items = minigame_service.get_crossword(topic, risk)
    else:
        raise ValueError("Minigame must be quiz, wordsearch or crossword.")

    return copy.deepcopy(items)


def _extract_concept_ids(items: list) -> list:
    concept_ids = []
    seen = set()

    for item in items:
        for concept_id in _item_concept_ids(item):
            if concept_id not in seen:
                concept_ids.append(concept_id)
                seen.add(concept_id)

    return concept_ids


def _item_concept_ids(item: dict) -> list:
    concept_ids = item.get("concept_ids")

    if concept_ids is None:
        concept_id = item.get("concept_id")
        concept_ids = [concept_id] if concept_id else []

    if not concept_ids:
        raise ValueError(f"Minigame item has no concept ids: {item.get('item_id')}")

    return list(concept_ids)


def _normalize_session_item(item: dict) -> dict:
    normalized = copy.deepcopy(item)
    normalized["concept_ids"] = _item_concept_ids(item)
    normalized.pop("concept_id", None)
    return normalized
