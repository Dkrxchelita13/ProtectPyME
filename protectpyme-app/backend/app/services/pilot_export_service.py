import csv
import io
from datetime import datetime
from statistics import mean

from sqlalchemy.orm import Session

from app import models
from app.services import pilot_assessment_service, pilot_service


SUMMARY_COLUMNS = [
    "participant_code",
    "pre_completed",
    "post_completed",
    "pre_total",
    "post_total",
    "gain_total",
    "pre_phishing",
    "post_phishing",
    "gain_phishing",
    "pre_passwords",
    "post_passwords",
    "gain_passwords",
    "pre_malware",
    "post_malware",
    "gain_malware",
    "pre_wifi",
    "post_wifi",
    "gain_wifi",
    "scenario_total_decisions",
    "scenario_correct_decisions",
    "scenario_accuracy",
    "mean_scenario_response_time",
    "minigame_attempts",
    "minigame_correct_attempts",
    "minigame_accuracy",
    "mean_minigame_response_time_ms",
    "training_sessions_started",
    "training_sessions_completed",
    "recommendations_emitted",
]

EVENT_COLUMNS = [
    "participant_code",
    "timestamp",
    "event_type",
    "topic",
    "activity_id",
    "concept_id",
    "correct",
    "response_time_ms",
]

FORBIDDEN_EXPORT_KEYS = {
    "user_id",
    "name",
    "email",
    "password",
    "auth_provider",
    "google_sub",
    "ip_address",
    "access_token",
    "token",
}


def build_summary_dataset(db: Session) -> list[dict]:
    rows = []

    for consent in _active_consents(db):
        user_id = consent.user_id
        pre = _completed_assessment(db, user_id, "PRE")
        post = _completed_assessment(db, user_id, "POST")
        scenario_metrics = _scenario_metrics(db, user_id)
        minigame_metrics = _minigame_metrics(db, user_id)

        row = {
            "participant_code": consent.participant_code,
            "pre_completed": pre is not None,
            "post_completed": post is not None,
            "pre_total": _score_value(pre, "total_score"),
            "post_total": _score_value(post, "total_score"),
            "gain_total": _gain(pre, post, "total_score"),
            "pre_phishing": _score_value(pre, "phishing_score"),
            "post_phishing": _score_value(post, "phishing_score"),
            "gain_phishing": _gain(pre, post, "phishing_score"),
            "pre_passwords": _score_value(pre, "passwords_score"),
            "post_passwords": _score_value(post, "passwords_score"),
            "gain_passwords": _gain(pre, post, "passwords_score"),
            "pre_malware": _score_value(pre, "malware_score"),
            "post_malware": _score_value(post, "malware_score"),
            "gain_malware": _gain(pre, post, "malware_score"),
            "pre_wifi": _score_value(pre, "wifi_score"),
            "post_wifi": _score_value(post, "wifi_score"),
            "gain_wifi": _gain(pre, post, "wifi_score"),
            **scenario_metrics,
            **minigame_metrics,
            "recommendations_emitted": _recommendation_count(db, user_id),
        }
        rows.append(row)

    rows.sort(key=lambda row: row["participant_code"])
    assert_no_forbidden_keys(rows)
    return rows


def build_events_dataset(db: Session) -> list[dict]:
    rows = []

    for consent in _active_consents(db):
        rows.extend(_scenario_decision_events(db, consent))
        rows.extend(_recommendation_events(db, consent))
        rows.extend(_assessment_answer_events(db, consent))
        rows.extend(_assessment_completed_events(db, consent))
        rows.extend(_minigame_session_events(db, consent))
        rows.extend(_minigame_attempt_events(db, consent))

    rows.sort(key=_event_sort_key)
    assert_no_forbidden_keys(rows)
    return rows


def serialize_summary_csv(rows: list[dict]) -> str:
    return serialize_csv(rows, SUMMARY_COLUMNS)


def serialize_events_csv(rows: list[dict]) -> str:
    return serialize_csv(rows, EVENT_COLUMNS)


def serialize_csv(rows: list[dict], columns: list[str]) -> str:
    output = io.StringIO(newline="")
    writer = csv.DictWriter(
        output,
        fieldnames=columns,
        extrasaction="ignore",
        lineterminator="\n",
    )
    writer.writeheader()

    for row in rows:
        writer.writerow({
            column: _csv_value(row.get(column))
            for column in columns
        })

    return output.getvalue()


def assert_no_forbidden_keys(data):
    if isinstance(data, dict):
        forbidden = FORBIDDEN_EXPORT_KEYS.intersection(data.keys())

        if forbidden:
            raise ValueError(
                "Forbidden export keys found: "
                + ", ".join(sorted(forbidden))
            )

        for value in data.values():
            assert_no_forbidden_keys(value)
        return

    if isinstance(data, list):
        for item in data:
            assert_no_forbidden_keys(item)


def _active_consents(db: Session):
    return (
        db.query(models.PilotConsent)
        .filter(
            models.PilotConsent.consent_version
            == pilot_service.PILOT_CONSENT_VERSION,
            models.PilotConsent.accepted.is_(True),
            models.PilotConsent.revoked_at.is_(None),
        )
        .order_by(models.PilotConsent.participant_code.asc())
        .all()
    )


def _completed_assessment(db: Session, user_id: int, phase: str):
    return (
        db.query(models.PilotAssessment)
        .filter(
            models.PilotAssessment.user_id == user_id,
            models.PilotAssessment.phase == phase,
            models.PilotAssessment.instrument_version
            == pilot_assessment_service.INSTRUMENT_VERSION,
            models.PilotAssessment.status == "completed",
        )
        .first()
    )


def _score_value(assessment, attribute: str):
    if assessment is None:
        return None

    return getattr(assessment, attribute)


def _gain(pre, post, attribute: str):
    if pre is None or post is None:
        return None

    return round(getattr(post, attribute) - getattr(pre, attribute), 2)


def _scenario_metrics(db: Session, user_id: int) -> dict:
    decisions = (
        db.query(models.Decision)
        .filter(models.Decision.user_id == user_id)
        .all()
    )
    total = len(decisions)
    correct = sum(1 for decision in decisions if decision.is_correct == 1)
    response_times = [
        decision.response_time
        for decision in decisions
        if decision.response_time is not None
    ]

    return {
        "scenario_total_decisions": total,
        "scenario_correct_decisions": correct,
        "scenario_accuracy": _percentage(correct, total),
        "mean_scenario_response_time": _mean(response_times),
    }


def _minigame_metrics(db: Session, user_id: int) -> dict:
    attempts = (
        db.query(models.MinigameAttempt)
        .filter(models.MinigameAttempt.user_id == user_id)
        .all()
    )
    sessions = (
        db.query(models.MinigameSessionRecord)
        .filter(models.MinigameSessionRecord.user_id == user_id)
        .all()
    )
    total_attempts = len(attempts)
    correct_attempts = sum(1 for attempt in attempts if attempt.correct)
    response_times = [
        attempt.response_time_ms
        for attempt in attempts
        if attempt.response_time_ms is not None
    ]

    return {
        "minigame_attempts": total_attempts,
        "minigame_correct_attempts": correct_attempts,
        "minigame_accuracy": _percentage(correct_attempts, total_attempts),
        "mean_minigame_response_time_ms": _mean(response_times),
        "training_sessions_started": len(sessions),
        "training_sessions_completed": sum(
            1 for session in sessions if session.status == "completed"
        ),
    }


def _recommendation_count(db: Session, user_id: int) -> int:
    return (
        db.query(models.RecommendationEvent)
        .filter(models.RecommendationEvent.user_id == user_id)
        .count()
    )


def _scenario_decision_events(db: Session, consent) -> list[dict]:
    records = (
        db.query(models.Decision, models.Scenario)
        .outerjoin(
            models.Scenario,
            models.Decision.scenario_id == models.Scenario.id,
        )
        .filter(models.Decision.user_id == consent.user_id)
        .all()
    )

    return [
        _event_row(
            consent.participant_code,
            decision.created_at,
            "scenario_decision",
            scenario.category if scenario else None,
            decision.scenario_id,
            None,
            decision.is_correct == 1,
            decision.response_time,
        )
        for decision, scenario in records
    ]


def _recommendation_events(db: Session, consent) -> list[dict]:
    records = (
        db.query(models.RecommendationEvent)
        .filter(models.RecommendationEvent.user_id == consent.user_id)
        .all()
    )

    return [
        _event_row(
            consent.participant_code,
            recommendation.created_at,
            "recommendation",
            recommendation.recommended_training,
            recommendation.recommended_scenario,
            None,
            None,
            None,
        )
        for recommendation in records
    ]


def _assessment_answer_events(db: Session, consent) -> list[dict]:
    records = (
        db.query(models.PilotAssessmentAnswer)
        .join(
            models.PilotAssessment,
            (
                models.PilotAssessmentAnswer.assessment_id
                == models.PilotAssessment.id
            ),
        )
        .filter(models.PilotAssessment.user_id == consent.user_id)
        .all()
    )

    return [
        _event_row(
            consent.participant_code,
            answer.created_at,
            "assessment_answer",
            answer.topic,
            answer.question_id,
            None,
            answer.is_correct,
            answer.response_time_ms,
        )
        for answer in records
    ]


def _assessment_completed_events(db: Session, consent) -> list[dict]:
    records = (
        db.query(models.PilotAssessment)
        .filter(
            models.PilotAssessment.user_id == consent.user_id,
            models.PilotAssessment.status == "completed",
            models.PilotAssessment.completed_at.isnot(None),
        )
        .all()
    )

    return [
        _event_row(
            consent.participant_code,
            assessment.completed_at,
            "assessment_completed",
            None,
            assessment.phase,
            None,
            None,
            None,
        )
        for assessment in records
    ]


def _minigame_session_events(db: Session, consent) -> list[dict]:
    records = (
        db.query(models.MinigameSessionRecord)
        .filter(models.MinigameSessionRecord.user_id == consent.user_id)
        .all()
    )

    return [
        _event_row(
            consent.participant_code,
            session.started_at,
            "minigame_session",
            session.topic,
            session.id,
            _single_concept_id(session.concept_ids),
            None,
            None,
        )
        for session in records
    ]


def _minigame_attempt_events(db: Session, consent) -> list[dict]:
    records = (
        db.query(models.MinigameAttempt, models.MinigameSessionRecord)
        .join(
            models.MinigameSessionRecord,
            (
                models.MinigameAttempt.session_id
                == models.MinigameSessionRecord.id
            ),
        )
        .filter(models.MinigameAttempt.user_id == consent.user_id)
        .all()
    )

    return [
        _event_row(
            consent.participant_code,
            attempt.created_at,
            "minigame_attempt",
            session.topic,
            attempt.item_id,
            _single_concept_id(attempt.concept_ids),
            attempt.correct,
            attempt.response_time_ms,
        )
        for attempt, session in records
    ]


def _event_row(
    participant_code: str,
    timestamp,
    event_type: str,
    topic,
    activity_id,
    concept_id,
    correct,
    response_time_ms,
) -> dict:
    return {
        "participant_code": participant_code,
        "timestamp": _iso_datetime(timestamp),
        "event_type": event_type,
        "topic": topic,
        "activity_id": None if activity_id is None else str(activity_id),
        "concept_id": concept_id,
        "correct": correct,
        "response_time_ms": response_time_ms,
    }


def _single_concept_id(concept_ids):
    if not isinstance(concept_ids, list):
        return None

    if len(concept_ids) != 1:
        return None

    return str(concept_ids[0])


def _percentage(numerator: int, denominator: int):
    if denominator == 0:
        return None

    return round((numerator / denominator) * 100, 2)


def _mean(values: list):
    if not values:
        return None

    return round(mean(values), 2)


def _event_sort_key(row: dict):
    return (
        row["participant_code"],
        row["timestamp"] or "",
        row["event_type"],
        row["activity_id"] or "",
        row["concept_id"] or "",
    )


def _csv_value(value):
    if value is None:
        return ""

    if isinstance(value, bool):
        return "true" if value else "false"

    if isinstance(value, datetime):
        return _iso_datetime(value)

    if isinstance(value, str):
        return _sanitize_csv_text(value)

    return value


def _iso_datetime(value):
    if value is None:
        return None

    return value.isoformat()


def _sanitize_csv_text(value: str) -> str:
    if value.startswith(("=", "+", "-", "@")):
        return "'" + value

    return value
