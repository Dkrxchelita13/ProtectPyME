import copy
from datetime import datetime
import logging
import uuid

from sqlalchemy.exc import IntegrityError

from app.services import adaptive_selection_service
from app.services import learning_content_service
from app.services import concept_mastery_service
from app.services import minigame_service
from app.services.concept_catalog import get_concepts


logger = logging.getLogger("protectpyme")


class MinigameSessionNotFoundError(Exception):
    pass


class MinigameSessionConflictError(Exception):
    pass


class MinigameSessionValidationError(Exception):
    pass


def create_minigame_session(
    topic: str,
    risk: str,
    minigame: str,
    db=None,
    user_id=None,
) -> dict:
    normalized_topic = minigame_service.normalize_topic(topic)
    normalized_risk = minigame_service.normalize_risk(risk)
    normalized_minigame = learning_content_service.normalize_minigame(minigame)

    items = _get_bank_items(
        normalized_topic,
        normalized_risk,
        normalized_minigame,
    )
    session_id = str(uuid.uuid4())
    selected_items = _select_session_items(
        db=db,
        user_id=user_id,
        topic=normalized_topic,
        risk=normalized_risk,
        minigame=normalized_minigame,
        candidates=items,
        session_id=session_id,
    )
    concept_ids = _extract_concept_ids(selected_items)
    get_concepts(concept_ids)

    lesson = learning_content_service.get_learning_content_for_concepts(
        normalized_topic,
        normalized_risk,
        normalized_minigame,
        concept_ids,
    )
    session_items = [
        _normalize_session_item(item, normalized_minigame)
        for item in selected_items
    ]
    response = {
        "session_id": session_id,
        "topic": normalized_topic,
        "risk": normalized_risk,
        "minigame": normalized_minigame,
        "lesson": lesson,
        "items": session_items,
    }

    if db is not None and user_id is not None:
        _persist_session_record(
            db=db,
            user_id=user_id,
            session_id=session_id,
            topic=normalized_topic,
            risk=normalized_risk,
            minigame=normalized_minigame,
            item_ids=[
                item["item_id"]
                for item in session_items
            ],
            concept_ids=concept_ids,
        )

    return response


def _select_session_items(
    db,
    user_id,
    topic: str,
    risk: str,
    minigame: str,
    candidates: list,
    session_id: str,
) -> list:
    if db is None or user_id is None:
        return candidates

    limit = adaptive_selection_service.get_session_item_limit(minigame)
    selected_items = adaptive_selection_service.select_adaptive_items(
        db=db,
        user_id=user_id,
        topic=topic,
        risk=risk,
        minigame=minigame,
        candidates=candidates,
        session_id=session_id,
        limit=limit,
    )
    logger.info(
        "Adaptive selection: user=%s topic=%s risk=%s minigame=%s "
        "candidates=%s selected=%s",
        user_id,
        topic,
        risk,
        minigame,
        len(candidates),
        len(selected_items),
    )
    return selected_items


def record_minigame_attempt(db, user_id: int, request) -> dict:
    models = _models()
    session = _get_owned_session(db, user_id, request.session_id)

    if session.status != "started":
        raise MinigameSessionConflictError("Minigame session is not started.")

    item_metadata = _get_backend_item_metadata(request.item_id)

    if request.item_id not in (session.item_ids or []):
        raise MinigameSessionValidationError(
            "Item does not belong to this session."
        )

    _validate_item_matches_session(item_metadata, session)

    item = item_metadata["item"]
    attempt = models.MinigameAttempt(
        session_id=session.id,
        user_id=user_id,
        item_id=request.item_id,
        concept_ids=minigame_service.get_item_concept_ids(item),
        difficulty=item["difficulty"],
        correct=request.correct,
        response_time_ms=request.response_time_ms,
        attempt_number=request.attempt_number,
        points_delta=request.points_delta,
    )

    db.add(attempt)

    try:
        db.commit()
    except IntegrityError as exc:
        db.rollback()
        raise MinigameSessionConflictError(
            "Attempt already exists for this item and attempt number."
        ) from exc

    db.refresh(attempt)
    return _attempt_to_response(attempt)


def complete_minigame_session(db, user_id: int, session_id: str) -> dict:
    models = _models()
    _validate_uuid(session_id)
    session = _get_owned_session(db, user_id, session_id)

    if session.status == "completed":
        attempts = (
            db.query(models.MinigameAttempt)
            .filter(
                models.MinigameAttempt.session_id == session.id,
                models.MinigameAttempt.user_id == user_id,
            )
            .all()
        )
        return _build_session_summary(session, attempts)

    if session.status != "started":
        raise MinigameSessionConflictError("Minigame session is not started.")

    attempts = (
        db.query(models.MinigameAttempt)
        .filter(
            models.MinigameAttempt.session_id == session.id,
            models.MinigameAttempt.user_id == user_id,
        )
        .all()
    )

    session.status = "completed"
    session.completed_at = datetime.utcnow()

    try:
        if attempts:
            concept_mastery_service.update_user_mastery_from_attempts(
                db=db,
                user_id=user_id,
                attempts=attempts,
            )
        db.commit()
    except (KeyError, ValueError) as exc:
        db.rollback()
        raise MinigameSessionValidationError(str(exc)) from exc
    except IntegrityError as exc:
        error_details = _extract_integrity_error_details(exc)
        logger.warning(
            "[MINIGAME COMPLETE REJECTED] session=%s user_id=%s "
            "status=%s attempts=%s reason=integrity_error "
            "sqlstate=%s constraint=%s orig_exception=%s",
            session.id,
            user_id,
            session.status,
            len(attempts),
            error_details["sqlstate"],
            error_details["constraint"],
            error_details["orig_exception"],
        )
        db.rollback()
        raise MinigameSessionConflictError(
            "Minigame session could not be completed."
        ) from exc

    db.refresh(session)
    return _build_session_summary(session, attempts)


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
    return minigame_service.get_item_concept_ids(item)


def _normalize_session_item(item: dict, minigame: str) -> dict:
    normalized = {
        "item_id": item["item_id"],
        "concept_ids": _item_concept_ids(item),
        "difficulty": item["difficulty"],
        "question": None,
        "options": None,
        "clue": None,
        "answer_text": "",
        "correct_option": -1,
    }

    if minigame == "quiz":
        normalized["question"] = item["question"]
        normalized["options"] = list(item["options"])
        normalized["correct_option"] = int(item["answer"])
    else:
        normalized["clue"] = item["clue"]
        normalized["answer_text"] = str(item["answer"])

    return normalized


def _persist_session_record(
    db,
    user_id: int,
    session_id: str,
    topic: str,
    risk: str,
    minigame: str,
    item_ids: list,
    concept_ids: list,
):
    models = _models()
    existing = (
        db.query(models.MinigameSessionRecord)
        .filter(models.MinigameSessionRecord.id == session_id)
        .first()
    )

    if existing:
        return existing

    record = models.MinigameSessionRecord(
        id=session_id,
        user_id=user_id,
        topic=topic,
        risk=risk,
        minigame=minigame,
        item_ids=list(item_ids),
        concept_ids=list(concept_ids),
        status="started",
    )

    db.add(record)

    try:
        db.commit()
    except IntegrityError:
        db.rollback()
        raise

    db.refresh(record)
    return record


def _get_owned_session(db, user_id: int, session_id: str):
    models = _models()
    _validate_uuid(session_id)
    session = (
        db.query(models.MinigameSessionRecord)
        .filter(
            models.MinigameSessionRecord.id == session_id,
            models.MinigameSessionRecord.user_id == user_id,
        )
        .first()
    )

    if session is None:
        raise MinigameSessionNotFoundError("Minigame session not found.")

    return session


def _validate_uuid(value: str):
    try:
        uuid.UUID(str(value))
    except ValueError as exc:
        raise MinigameSessionValidationError(
            "session_id must be a valid UUID."
        ) from exc


def _get_backend_item_metadata(item_id: str):
    try:
        return minigame_service.get_item_by_id(item_id)
    except KeyError as exc:
        raise MinigameSessionNotFoundError("Minigame item not found.") from exc


def _validate_item_matches_session(item_metadata: dict, session):
    item = item_metadata["item"]
    concept_ids = minigame_service.get_item_concept_ids(item)

    if item_metadata["topic"] != session.topic:
        raise MinigameSessionConflictError("Item topic does not match session.")

    if item_metadata["risk"] != session.risk:
        raise MinigameSessionConflictError("Item risk does not match session.")

    if item_metadata["minigame"] != session.minigame:
        raise MinigameSessionConflictError("Item minigame does not match session.")

    if item["difficulty"] != session.risk:
        raise MinigameSessionConflictError("Item difficulty does not match session.")

    for concept_id in concept_ids:
        if concept_id not in (session.concept_ids or []):
            raise MinigameSessionConflictError(
                "Item concepts do not match session."
            )


def _attempt_to_response(attempt) -> dict:
    return {
        "id": attempt.id,
        "session_id": attempt.session_id,
        "item_id": attempt.item_id,
        "concept_ids": list(attempt.concept_ids or []),
        "difficulty": attempt.difficulty,
        "correct": attempt.correct,
        "response_time_ms": attempt.response_time_ms,
        "attempt_number": attempt.attempt_number,
        "points_delta": attempt.points_delta,
        "created_at": attempt.created_at,
    }


def _build_session_summary(session, attempts: list) -> dict:
    total_attempts = len(attempts)
    correct_attempts = sum(1 for attempt in attempts if attempt.correct)
    incorrect_attempts = total_attempts - correct_attempts
    points_earned = sum(attempt.points_delta for attempt in attempts)
    total_response_time_ms = sum(attempt.response_time_ms for attempt in attempts)
    attempted_items = len({attempt.item_id for attempt in attempts})
    accuracy = 0 if total_attempts == 0 else round(
        correct_attempts / total_attempts * 100,
        2
    )

    return {
        "session_id": session.id,
        "status": session.status,
        "topic": session.topic,
        "risk": session.risk,
        "minigame": session.minigame,
        "total_items": len(session.item_ids or []),
        "attempted_items": attempted_items,
        "total_attempts": total_attempts,
        "correct_attempts": correct_attempts,
        "incorrect_attempts": incorrect_attempts,
        "points_earned": points_earned,
        "accuracy": accuracy,
        "total_response_time_ms": total_response_time_ms,
        "started_at": session.started_at,
        "completed_at": session.completed_at,
    }


def _extract_integrity_error_details(exc: IntegrityError) -> dict:
    orig = getattr(exc, "orig", None)
    diag = getattr(orig, "diag", None)

    sqlstate = (
        getattr(orig, "pgcode", None)
        or getattr(orig, "sqlstate", None)
        or getattr(diag, "sqlstate", None)
        or "unknown"
    )
    constraint = (
        getattr(diag, "constraint_name", None)
        or getattr(orig, "constraint_name", None)
        or "unknown"
    )
    orig_exception = type(orig).__name__ if orig is not None else "unknown"

    return {
        "sqlstate": str(sqlstate or "unknown"),
        "constraint": str(constraint or "unknown"),
        "orig_exception": orig_exception,
    }


def _models():
    from app import models

    return models
