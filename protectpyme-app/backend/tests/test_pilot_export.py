import csv
import importlib
import os
import sys
import types
from datetime import datetime
from io import StringIO
from types import SimpleNamespace

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool


os.environ.setdefault("SECRET_KEY", "test-secret")


def install_fake_database():
    test_base = declarative_base()
    engine = create_engine(
        "sqlite://",
        connect_args={"check_same_thread": False},
        poolclass=StaticPool,
    )
    testing_session_local = sessionmaker(
        autocommit=False,
        autoflush=False,
        bind=engine,
    )

    def get_test_db():
        db = testing_session_local()

        try:
            yield db
        finally:
            db.close()

    fake_database = types.ModuleType("app.database")
    fake_database.Base = test_base
    fake_database.engine = engine
    fake_database.SessionLocal = testing_session_local
    fake_database.get_db = get_test_db
    sys.modules["app.database"] = fake_database

    return SimpleNamespace(
        engine=engine,
        TestingSessionLocal=testing_session_local,
    )


@pytest.fixture()
def app_modules():
    managed_modules = (
        "app.database",
        "app.models",
        "app.schemas",
        "app.auth",
        "app.routes.pilot",
        "app.services.pilot_assessment_service",
        "app.services.pilot_export_service",
        "app.services.pilot_service",
    )
    previous_modules = {
        name: sys.modules.get(name)
        for name in managed_modules
    }

    for name in managed_modules:
        sys.modules.pop(name, None)

    for package_name, attribute_names in (
        ("app", ("auth", "database", "models", "schemas")),
        ("app.routes", ("pilot",)),
        (
            "app.services",
            (
                "pilot_assessment_service",
                "pilot_export_service",
                "pilot_service",
            ),
        ),
    ):
        package = sys.modules.get(package_name)

        if package is None:
            continue

        for attribute_name in attribute_names:
            if hasattr(package, attribute_name):
                delattr(package, attribute_name)

    database = install_fake_database()

    models = importlib.import_module("app.models")
    auth = importlib.import_module("app.auth")
    app_database = importlib.import_module("app.database")
    pilot_route = importlib.import_module("app.routes.pilot")
    pilot_export_service = importlib.import_module(
        "app.services.pilot_export_service"
    )
    pilot_service = importlib.import_module("app.services.pilot_service")

    try:
        yield SimpleNamespace(
            engine=database.engine,
            get_current_user=auth.get_current_user,
            get_db=app_database.get_db,
            models=models,
            pilot_export_service=pilot_export_service,
            pilot_route=pilot_route,
            pilot_service=pilot_service,
            TestingSessionLocal=database.TestingSessionLocal,
        )
    finally:
        for name, module in previous_modules.items():
            if module is None:
                sys.modules.pop(name, None)
            else:
                sys.modules[name] = module


@pytest.fixture()
def db_session(app_modules):
    app_modules.models.Base.metadata.create_all(bind=app_modules.engine)
    db = app_modules.TestingSessionLocal()

    try:
        yield db
    finally:
        db.close()
        app_modules.models.Base.metadata.drop_all(bind=app_modules.engine)


@pytest.fixture()
def users(app_modules, db_session):
    models = app_modules.models
    records = SimpleNamespace(
        admin=models.User(
            name="Admin",
            email="admin@example.com",
            password="not-used",
            role="admin",
        ),
        normal=models.User(
            name="Normal",
            email="normal@example.com",
            password="not-used",
            role="user",
        ),
        active=models.User(
            name="Active",
            email="active@example.com",
            password="not-used",
        ),
        partial=models.User(
            name="Partial",
            email="partial@example.com",
            password="not-used",
        ),
        revoked=models.User(
            name="Revoked",
            email="revoked@example.com",
            password="not-used",
        ),
        no_consent=models.User(
            name="No Consent",
            email="no-consent@example.com",
            password="not-used",
        ),
    )
    db_session.add_all(vars(records).values())
    db_session.commit()

    for record in vars(records).values():
        db_session.refresh(record)

    return records


def pilot_client(app_modules, db_session, current_user):
    app = FastAPI()
    app.include_router(app_modules.pilot_route.router)

    def override_get_db():
        yield db_session

    def override_current_user():
        return current_user

    app.dependency_overrides[app_modules.get_db] = override_get_db
    app.dependency_overrides[app_modules.get_current_user] = (
        override_current_user
    )

    return TestClient(app)


def seed_export_data(app_modules, db_session, users):
    models = app_modules.models
    version = app_modules.pilot_service.PILOT_CONSENT_VERSION
    times = fixed_times()

    db_session.add_all([
        models.PilotConsent(
            user_id=users.active.id,
            participant_code="pp_active",
            consent_version=version,
            accepted=True,
            accepted_at=times.t0,
            revoked_at=None,
        ),
        models.PilotConsent(
            user_id=users.partial.id,
            participant_code="pp_partial",
            consent_version=version,
            accepted=True,
            accepted_at=times.t0,
            revoked_at=None,
        ),
        models.PilotConsent(
            user_id=users.revoked.id,
            participant_code="pp_revoked",
            consent_version=version,
            accepted=False,
            accepted_at=times.t0,
            revoked_at=times.t9,
        ),
    ])
    db_session.add_all([
        models.Scenario(
            id=101,
            title="Correo",
            description="Phishing",
            difficulty="medium",
            category="phishing",
            correct_choice="report",
        ),
        models.Scenario(
            id=102,
            title="WiFi",
            description="Red",
            difficulty="medium",
            category="wifi",
            correct_choice="block",
        ),
        models.Scenario(
            id=103,
            title="USB",
            description="Malware",
            difficulty="medium",
            category="malware",
            correct_choice="avoid",
        ),
    ])
    db_session.add_all([
        models.Decision(
            user_id=users.active.id,
            scenario_id=101,
            choice="report",
            is_correct=1,
            points_awarded=20,
            risk_level="low",
            response_time=1000,
            created_at=times.t1,
        ),
        models.Decision(
            user_id=users.active.id,
            scenario_id=102,
            choice="ignore",
            is_correct=0,
            points_awarded=0,
            risk_level="medium",
            response_time=None,
            created_at=times.t2,
        ),
        models.Decision(
            user_id=users.active.id,
            scenario_id=103,
            choice="avoid",
            is_correct=1,
            points_awarded=20,
            risk_level="low",
            response_time=3000,
            created_at=times.t3,
        ),
        models.Decision(
            user_id=users.revoked.id,
            scenario_id=101,
            choice="report",
            is_correct=1,
            points_awarded=20,
            risk_level="low",
            response_time=1000,
            created_at=times.t1,
        ),
        models.Decision(
            user_id=users.no_consent.id,
            scenario_id=101,
            choice="report",
            is_correct=1,
            points_awarded=20,
            risk_level="low",
            response_time=1000,
            created_at=times.t1,
        ),
    ])
    db_session.add_all([
        assessment(
            models,
            users.active.id,
            "11111111-1111-1111-1111-111111111111",
            "PRE",
            times.t4,
            50.0,
            100.0,
            100.0,
            0.0,
            0.0,
        ),
        assessment(
            models,
            users.active.id,
            "22222222-2222-2222-2222-222222222222",
            "POST",
            times.t8,
            75.0,
            100.0,
            100.0,
            50.0,
            50.0,
        ),
        assessment(
            models,
            users.partial.id,
            "33333333-3333-3333-3333-333333333333",
            "PRE",
            times.t4,
            25.0,
            0.0,
            0.0,
            50.0,
            50.0,
        ),
    ])
    db_session.add_all([
        models.PilotAssessmentAnswer(
            assessment_id="11111111-1111-1111-1111-111111111111",
            question_id="pre_phishing_01",
            topic="phishing",
            selected_option="B",
            is_correct=True,
            response_time_ms=1200,
            created_at=times.t5,
        ),
        models.PilotAssessmentAnswer(
            assessment_id="22222222-2222-2222-2222-222222222222",
            question_id="post_wifi_03",
            topic="wifi",
            selected_option="B",
            is_correct=True,
            response_time_ms=1800,
            created_at=times.t7,
        ),
    ])
    db_session.add_all([
        models.MinigameSessionRecord(
            id="session-active-1",
            user_id=users.active.id,
            topic="phishing",
            risk="medio",
            minigame="quiz",
            item_ids=["item-1"],
            concept_ids=["phishing.signals"],
            status="completed",
            started_at=times.t4,
            completed_at=times.t6,
        ),
        models.MinigameSessionRecord(
            id="session-active-2",
            user_id=users.active.id,
            topic="wifi",
            risk="medio",
            minigame="wordsearch",
            item_ids=["item-2"],
            concept_ids=["wifi.suspicious_traffic", "wifi.data_exfiltration"],
            status="started",
            started_at=times.t5,
            completed_at=None,
        ),
    ])
    db_session.add_all([
        models.MinigameAttempt(
            session_id="session-active-1",
            user_id=users.active.id,
            item_id="quiz-1",
            concept_ids=["phishing.signals"],
            difficulty="medio",
            correct=True,
            response_time_ms=500,
            attempt_number=1,
            points_delta=10,
            created_at=times.t5,
        ),
        models.MinigameAttempt(
            session_id="session-active-1",
            user_id=users.active.id,
            item_id="quiz-2",
            concept_ids=["phishing.domain"],
            difficulty="medio",
            correct=False,
            response_time_ms=1000,
            attempt_number=1,
            points_delta=0,
            created_at=times.t6,
        ),
        models.MinigameAttempt(
            session_id="session-active-2",
            user_id=users.active.id,
            item_id="word-1",
            concept_ids=["wifi.data_exfiltration"],
            difficulty="medio",
            correct=True,
            response_time_ms=1500,
            attempt_number=1,
            points_delta=10,
            created_at=times.t7,
        ),
    ])
    db_session.add_all([
        models.RecommendationEvent(
            user_id=users.active.id,
            risk_level="MEDIO",
            recommended_training="phishing",
            recommended_scenario=5,
            source="behavioral",
            evidence_count=5,
            created_at=times.t2,
        ),
        models.RecommendationEvent(
            user_id=users.active.id,
            risk_level="ALTO",
            recommended_training="wifi",
            recommended_scenario=7,
            source="survey",
            evidence_count=0,
            created_at=times.t6,
        ),
    ])
    db_session.commit()


def fixed_times():
    return SimpleNamespace(
        t0=datetime(2026, 1, 1, 8, 0, 0),
        t1=datetime(2026, 1, 1, 8, 1, 0),
        t2=datetime(2026, 1, 1, 8, 2, 0),
        t3=datetime(2026, 1, 1, 8, 3, 0),
        t4=datetime(2026, 1, 1, 8, 4, 0),
        t5=datetime(2026, 1, 1, 8, 5, 0),
        t6=datetime(2026, 1, 1, 8, 6, 0),
        t7=datetime(2026, 1, 1, 8, 7, 0),
        t8=datetime(2026, 1, 1, 8, 8, 0),
        t9=datetime(2026, 1, 1, 8, 9, 0),
    )


def assessment(
    models,
    user_id,
    assessment_id,
    phase,
    completed_at,
    total,
    phishing,
    passwords,
    malware,
    wifi,
):
    return models.PilotAssessment(
        id=assessment_id,
        user_id=user_id,
        phase=phase,
        form="A" if phase == "PRE" else "B",
        instrument_version="pilot_assessment_v1",
        status="completed",
        started_at=completed_at,
        completed_at=completed_at,
        total_score=total,
        phishing_score=phishing,
        passwords_score=passwords,
        malware_score=malware,
        wifi_score=wifi,
    )


def parse_csv(text):
    return list(csv.DictReader(StringIO(text)))


def test_summary_dataset_calculates_persisted_scores_and_metrics(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)

    rows = app_modules.pilot_export_service.build_summary_dataset(db_session)
    active = rows[0]

    assert [row["participant_code"] for row in rows] == [
        "pp_active",
        "pp_partial",
    ]
    assert active["pre_completed"] is True
    assert active["post_completed"] is True
    assert active["pre_total"] == 50.0
    assert active["post_total"] == 75.0
    assert active["gain_total"] == 25.0
    assert active["pre_phishing"] == 100.0
    assert active["post_phishing"] == 100.0
    assert active["gain_phishing"] == 0.0
    assert active["post_malware"] == 50.0
    assert active["gain_malware"] == 50.0
    assert active["scenario_total_decisions"] == 3
    assert active["scenario_correct_decisions"] == 2
    assert active["scenario_accuracy"] == 66.67
    assert active["mean_scenario_response_time"] == 2000
    assert active["minigame_attempts"] == 3
    assert active["minigame_correct_attempts"] == 2
    assert active["minigame_accuracy"] == 66.67
    assert active["mean_minigame_response_time_ms"] == 1000
    assert active["training_sessions_started"] == 2
    assert active["training_sessions_completed"] == 1
    assert active["recommendations_emitted"] == 2


def test_summary_dataset_uses_nulls_for_missing_evidence(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)

    partial = app_modules.pilot_export_service.build_summary_dataset(
        db_session
    )[1]

    assert partial["participant_code"] == "pp_partial"
    assert partial["pre_completed"] is True
    assert partial["post_completed"] is False
    assert partial["post_total"] is None
    assert partial["gain_total"] is None
    assert partial["scenario_total_decisions"] == 0
    assert partial["scenario_accuracy"] is None
    assert partial["mean_scenario_response_time"] is None
    assert partial["minigame_attempts"] == 0
    assert partial["minigame_accuracy"] is None
    assert partial["mean_minigame_response_time_ms"] is None


def test_summary_excludes_revoked_and_nonconsented_users(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)

    rows = app_modules.pilot_export_service.build_summary_dataset(db_session)
    participant_codes = [row["participant_code"] for row in rows]

    assert "pp_active" in participant_codes
    assert "pp_partial" in participant_codes
    assert "pp_revoked" not in participant_codes
    assert len(rows) == 2


def test_events_dataset_is_longitudinal_and_deterministically_ordered(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)

    rows = app_modules.pilot_export_service.build_events_dataset(db_session)
    active_rows = [
        row
        for row in rows
        if row["participant_code"] == "pp_active"
    ]

    assert [row["participant_code"] for row in rows] == sorted(
        row["participant_code"]
        for row in rows
    )
    assert [row["timestamp"] for row in active_rows] == sorted(
        row["timestamp"]
        for row in active_rows
    )
    assert {
        row["event_type"]
        for row in active_rows
    } == {
        "scenario_decision",
        "recommendation",
        "assessment_answer",
        "assessment_completed",
        "minigame_session",
        "minigame_attempt",
    }


def test_events_dataset_contains_expected_event_fields(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)

    rows = app_modules.pilot_export_service.build_events_dataset(db_session)
    scenario_event = next(
        row
        for row in rows
        if row["event_type"] == "scenario_decision"
        and row["activity_id"] == "101"
    )
    assessment_answer_event = next(
        row
        for row in rows
        if row["event_type"] == "assessment_answer"
    )
    minigame_attempt_event = next(
        row
        for row in rows
        if row["event_type"] == "minigame_attempt"
        and row["activity_id"] == "word-1"
    )
    multi_concept_session = next(
        row
        for row in rows
        if row["event_type"] == "minigame_session"
        and row["activity_id"] == "session-active-2"
    )

    assert scenario_event["topic"] == "phishing"
    assert scenario_event["correct"] is True
    assert scenario_event["response_time_ms"] == 1000
    assert assessment_answer_event["activity_id"] == "pre_phishing_01"
    assert assessment_answer_event["correct"] is True
    assert "selected_option" not in assessment_answer_event
    assert "correct_option" not in assessment_answer_event
    assert minigame_attempt_event["concept_id"] == "wifi.data_exfiltration"
    assert multi_concept_session["concept_id"] is None


def test_events_exclude_revoked_and_nonconsented_users(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)

    rows = app_modules.pilot_export_service.build_events_dataset(db_session)

    assert {row["participant_code"] for row in rows} == {
        "pp_active",
        "pp_partial",
    }


def test_structured_exports_do_not_contain_forbidden_pii_keys(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)
    service = app_modules.pilot_export_service

    summary = service.build_summary_dataset(db_session)
    events = service.build_events_dataset(db_session)

    assert_forbidden_keys_absent(summary, service.FORBIDDEN_EXPORT_KEYS)
    assert_forbidden_keys_absent(events, service.FORBIDDEN_EXPORT_KEYS)


def test_forbidden_key_guard_is_recursive(app_modules):
    service = app_modules.pilot_export_service

    with pytest.raises(ValueError):
        service.assert_no_forbidden_keys([
            {
                "participant_code": "pp_safe",
                "nested": {"email": "hidden@example.com"},
            }
        ])


def test_csv_serialization_has_stable_headers_and_escapes_formulas(
    app_modules,
):
    service = app_modules.pilot_export_service

    csv_text = service.serialize_csv(
        [
            {
                "participant_code": "=cmd",
                "timestamp": None,
                "event_type": "@bad",
            }
        ],
        ["participant_code", "timestamp", "event_type"],
    )

    assert csv_text.splitlines()[0] == "participant_code,timestamp,event_type"
    assert parse_csv(csv_text)[0] == {
        "participant_code": "'=cmd",
        "timestamp": "",
        "event_type": "'@bad",
    }


def test_normal_user_cannot_access_export_endpoint(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)
    client = pilot_client(app_modules, db_session, users.normal)

    response = client.get("/pilot/export/summary.csv")

    assert response.status_code == 403


def test_admin_can_export_summary_csv_without_pii(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)
    client = pilot_client(app_modules, db_session, users.admin)

    response = client.get("/pilot/export/summary.csv")
    csv_rows = parse_csv(response.text)

    assert response.status_code == 200
    assert response.headers["content-type"].startswith("text/csv")
    assert response.text.splitlines()[0].split(",") == (
        app_modules.pilot_export_service.SUMMARY_COLUMNS
    )
    assert csv_rows[0]["participant_code"] == "pp_active"
    assert "user_id" not in csv_rows[0]
    assert "email" not in csv_rows[0]
    assert "google_sub" not in csv_rows[0]


def test_admin_can_export_events_csv_without_answer_key(
    app_modules,
    db_session,
    users,
):
    seed_export_data(app_modules, db_session, users)
    client = pilot_client(app_modules, db_session, users.admin)

    response = client.get("/pilot/export/events.csv")
    csv_rows = parse_csv(response.text)

    assert response.status_code == 200
    assert response.text.splitlines()[0].split(",") == (
        app_modules.pilot_export_service.EVENT_COLUMNS
    )
    assert csv_rows[0]["participant_code"] == "pp_active"
    assert "selected_option" not in csv_rows[0]
    assert "correct_option" not in csv_rows[0]
    assert "user_id" not in csv_rows[0]


def assert_forbidden_keys_absent(value, forbidden_keys):
    if isinstance(value, dict):
        assert not forbidden_keys.intersection(value.keys())

        for child in value.values():
            assert_forbidden_keys_absent(child, forbidden_keys)

    if isinstance(value, list):
        for child in value:
            assert_forbidden_keys_absent(child, forbidden_keys)
