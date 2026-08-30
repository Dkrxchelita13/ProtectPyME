from datetime import datetime, timedelta
import secrets

from sqlalchemy.exc import IntegrityError

from app import models


PILOT_CONSENT_VERSION = "pilot-v1"
PARTICIPANT_CODE_BYTES = 16
RECOMMENDATION_DEDUPLICATION_WINDOW_SECONDS = 300


def get_pilot_consent(db, user_id: int) -> dict:
    consent = _get_consent_record(db, user_id)

    if consent is None:
        return {
            "consent_version": PILOT_CONSENT_VERSION,
            "accepted": False,
            "participant_code": None,
            "accepted_at": None,
            "revoked_at": None,
        }

    return _consent_response(consent)


def accept_pilot_consent(db, user_id: int) -> dict:
    consent = _get_consent_record(db, user_id)
    now = datetime.utcnow()

    if consent is None:
        consent = models.PilotConsent(
            user_id=user_id,
            participant_code=_generate_unique_participant_code(db),
            consent_version=PILOT_CONSENT_VERSION,
        )
        db.add(consent)

    consent.accepted = True
    consent.accepted_at = consent.accepted_at or now
    consent.revoked_at = None

    _commit_and_refresh(db, consent)
    return _consent_response(consent)


def revoke_pilot_consent(db, user_id: int) -> dict:
    consent = _get_consent_record(db, user_id)

    if consent is None:
        return get_pilot_consent(db, user_id)

    consent.accepted = False
    consent.revoked_at = datetime.utcnow()

    _commit_and_refresh(db, consent)
    return _consent_response(consent)


def persist_recommendation_event(db, user_id: int, recommendation: dict):
    if not has_active_pilot_consent(db, user_id):
        return None

    event_data = _build_recommendation_event_data(user_id, recommendation)
    existing = _find_recent_duplicate_event(db, event_data)

    if existing is not None:
        return existing

    event = models.RecommendationEvent(**event_data)
    db.add(event)
    _commit_and_refresh(db, event)

    return event


def has_active_pilot_consent(db, user_id: int) -> bool:
    consent = _get_consent_record(db, user_id)

    if consent is None:
        return False

    return consent.accepted is True and consent.revoked_at is None


def _get_consent_record(db, user_id: int):
    return (
        db.query(models.PilotConsent)
        .filter(
            models.PilotConsent.user_id == user_id,
            models.PilotConsent.consent_version == PILOT_CONSENT_VERSION,
        )
        .first()
    )


def _generate_unique_participant_code(db) -> str:
    for _ in range(5):
        participant_code = (
            "pp_" + secrets.token_urlsafe(PARTICIPANT_CODE_BYTES)
        )
        exists = (
            db.query(models.PilotConsent)
            .filter(models.PilotConsent.participant_code == participant_code)
            .first()
        )

        if not exists:
            return participant_code

    raise RuntimeError("Unable to generate unique participant_code")


def _build_recommendation_event_data(user_id: int, recommendation: dict) -> dict:
    return {
        "user_id": user_id,
        "risk_level": str(recommendation["risk_level"]),
        "recommended_training": str(recommendation["recommended_training"]),
        "recommended_scenario": int(recommendation["recommended_scenario"]),
        "source": _recommendation_source(recommendation.get("risk_source")),
        "evidence_count": int(recommendation.get("behavioral_decisions") or 0),
    }


def _recommendation_source(risk_source: str | None) -> str:
    if risk_source == "survey":
        return "survey"

    return "behavioral"


def _find_recent_duplicate_event(db, event_data: dict):
    cutoff = (
        datetime.utcnow()
        - timedelta(seconds=RECOMMENDATION_DEDUPLICATION_WINDOW_SECONDS)
    )

    return (
        db.query(models.RecommendationEvent)
        .filter(
            models.RecommendationEvent.user_id == event_data["user_id"],
            models.RecommendationEvent.risk_level == event_data["risk_level"],
            models.RecommendationEvent.recommended_training == event_data["recommended_training"],
            models.RecommendationEvent.recommended_scenario == event_data["recommended_scenario"],
            models.RecommendationEvent.source == event_data["source"],
            models.RecommendationEvent.evidence_count == event_data["evidence_count"],
            models.RecommendationEvent.created_at >= cutoff,
        )
        .order_by(models.RecommendationEvent.created_at.desc())
        .first()
    )


def _commit_and_refresh(db, record):
    try:
        db.commit()
    except IntegrityError:
        db.rollback()
        raise

    db.refresh(record)


def _consent_response(consent) -> dict:
    return {
        "consent_version": consent.consent_version,
        "accepted": consent.accepted,
        "participant_code": consent.participant_code,
        "accepted_at": consent.accepted_at,
        "revoked_at": consent.revoked_at,
    }
