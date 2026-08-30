import os
import sys
import types
import importlib
from datetime import datetime, timedelta
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
        "app.auth",
        "app.routes.auth",
        "app.routes.ai",
        "app.routes.pilot",
        "app.services.ai_service",
        "app.services.pilot_service",
    )
    previous_modules = {
        name: sys.modules.get(name)
        for name in managed_modules
    }

    for name in managed_modules:
        sys.modules.pop(name, None)

    for package_name, attribute_names in (
        ("app", ("auth", "database", "models")),
        ("app.routes", ("ai", "auth", "pilot")),
        ("app.services", ("ai_service", "pilot_service")),
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
    ai_route = importlib.import_module("app.routes.ai")
    pilot_route = importlib.import_module("app.routes.pilot")
    pilot_service = importlib.import_module("app.services.pilot_service")

    try:
        yield SimpleNamespace(
            ai_route=ai_route,
            engine=database.engine,
            get_current_user=auth.get_current_user,
            get_db=app_database.get_db,
            models=models,
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
def user(app_modules, db_session):
    user = app_modules.models.User(
        name="Pilot User",
        email="pilot@example.com",
        password="not-used",
        total_points=30,
        total_decisions=3,
        correct_decisions=2,
        risk_score=1,
    )
    db_session.add(user)
    db_session.commit()
    db_session.refresh(user)
    return user


@pytest.fixture()
def other_user(app_modules, db_session):
    user = app_modules.models.User(
        name="Other Pilot User",
        email="other-pilot@example.com",
        password="not-used",
    )
    db_session.add(user)
    db_session.commit()
    db_session.refresh(user)
    return user


@pytest.fixture()
def pilot_client(app_modules, db_session, user):
    app = FastAPI()
    app.include_router(app_modules.pilot_route.router)

    def override_get_db():
        yield db_session

    def override_current_user():
        return user

    app.dependency_overrides[app_modules.get_db] = override_get_db
    app.dependency_overrides[app_modules.get_current_user] = (
        override_current_user
    )

    with TestClient(app) as client:
        yield client


def test_pilot_consent_requires_authentication(app_modules, db_session):
    app = FastAPI()
    app.include_router(app_modules.pilot_route.router)

    def override_get_db():
        yield db_session

    app.dependency_overrides[app_modules.get_db] = override_get_db

    response = TestClient(app).get("/pilot/consent")

    assert response.status_code == 401


def test_get_pilot_consent_returns_initial_state_without_pii(pilot_client):
    response = pilot_client.get("/pilot/consent")

    assert response.status_code == 200
    data = response.json()
    assert data == {
        "consent_version": "pilot-v1",
        "accepted": False,
        "participant_code": None,
        "accepted_at": None,
        "revoked_at": None,
    }
    assert "name" not in data
    assert "email" not in data
    assert "google_sub" not in data


def test_user_can_accept_consent_and_receives_stable_participant_code(
    pilot_client,
):
    response = pilot_client.post(
        "/pilot/consent",
        json={"accepted": True},
    )
    second_response = pilot_client.post(
        "/pilot/consent",
        json={"accepted": True},
    )

    assert response.status_code == 201
    assert second_response.status_code == 201
    data = response.json()
    second_data = second_response.json()
    assert data["accepted"] is True
    assert data["consent_version"] == "pilot-v1"
    assert data["participant_code"].startswith("pp_")
    assert second_data["participant_code"] == data["participant_code"]
    assert second_data["accepted_at"] == data["accepted_at"]
    assert "email" not in data


def test_second_user_receives_different_participant_code(
    app_modules,
    db_session,
    user,
    other_user,
):
    first = app_modules.pilot_service.accept_pilot_consent(
        db_session,
        user.id,
    )
    second = app_modules.pilot_service.accept_pilot_consent(
        db_session,
        other_user.id,
    )

    assert first["participant_code"] != second["participant_code"]


def test_get_pilot_consent_returns_accepted_state(pilot_client):
    accepted = pilot_client.post(
        "/pilot/consent",
        json={"accepted": True},
    ).json()

    response = pilot_client.get("/pilot/consent")

    assert response.status_code == 200
    data = response.json()
    assert data["accepted"] is True
    assert data["participant_code"] == accepted["participant_code"]
    assert data["accepted_at"] == accepted["accepted_at"]


def test_revoke_pilot_consent_preserves_participant_code(pilot_client):
    accepted = pilot_client.post(
        "/pilot/consent",
        json={"accepted": True},
    ).json()

    response = pilot_client.post("/pilot/consent/revoke")

    assert response.status_code == 200
    data = response.json()
    assert data["accepted"] is False
    assert data["participant_code"] == accepted["participant_code"]
    assert data["accepted_at"] == accepted["accepted_at"]
    assert data["revoked_at"] is not None


def test_accept_pilot_consent_requires_explicit_true(pilot_client):
    response = pilot_client.post(
        "/pilot/consent",
        json={"accepted": False},
    )

    assert response.status_code == 422


def ai_client(app_modules, db_session, user, monkeypatch, result):
    app = FastAPI()
    app.include_router(app_modules.ai_route.router)

    def override_get_db():
        yield db_session

    def override_current_user():
        return user

    async def fake_prediction(db, user_id):
        return dict(result)

    monkeypatch.setattr(
        app_modules.ai_route.AIService,
        "get_user_risk_prediction",
        fake_prediction,
    )

    app.dependency_overrides[app_modules.get_db] = override_get_db
    app.dependency_overrides[app_modules.get_current_user] = (
        override_current_user
    )

    return TestClient(app)


def behavioral_recommendation():
    return {
        "user_id": 1,
        "risk_level": "MEDIO",
        "probability": 0.66,
        "recommended_training": "phishing",
        "recommended_scenario": 5,
        "message": "Practica deteccion de correos fraudulentos",
        "risk_source": "random_forest",
        "behavioral_decisions": 3,
        "min_behavioral_decisions": 3,
        "sufficient_behavioral_data": True,
    }


def survey_recommendation():
    return {
        "user_id": 1,
        "risk_level": "ALTO",
        "probability": 0.0,
        "recommended_training": "passwords",
        "recommended_scenario": 2,
        "message": "Diagnostico inicial listo",
        "risk_source": "survey",
        "behavioral_decisions": 0,
        "min_behavioral_decisions": 3,
        "sufficient_behavioral_data": False,
    }


def recommendation_events(app_modules, db_session):
    return db_session.query(app_modules.models.RecommendationEvent).all()


def accept_consent(app_modules, db_session, user):
    return app_modules.pilot_service.accept_pilot_consent(
        db_session,
        user.id,
    )


def test_user_without_consent_keeps_ai_risk_response_without_event(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    result = behavioral_recommendation()
    client = ai_client(app_modules, db_session, user, monkeypatch, result)

    response = client.get("/ai/risk/me")

    assert response.status_code == 200
    assert response.json() == result
    assert recommendation_events(app_modules, db_session) == []


def test_ai_risk_contract_is_preserved_when_event_is_persisted(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    accept_consent(app_modules, db_session, user)
    result = behavioral_recommendation()
    client = ai_client(app_modules, db_session, user, monkeypatch, result)

    response = client.get("/ai/risk/me")

    assert response.status_code == 200
    assert response.json() == result


def test_behavioral_recommendation_creates_event_with_expected_fields(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    accept_consent(app_modules, db_session, user)
    client = ai_client(
        app_modules,
        db_session,
        user,
        monkeypatch,
        behavioral_recommendation(),
    )

    response = client.get("/ai/risk/me")

    assert response.status_code == 200
    events = recommendation_events(app_modules, db_session)
    assert len(events) == 1
    event = events[0]
    assert event.user_id == user.id
    assert event.risk_level == "MEDIO"
    assert event.recommended_training == "phishing"
    assert event.recommended_scenario == 5
    assert event.source == "behavioral"
    assert event.evidence_count == 3


def test_survey_recommendation_creates_event_with_survey_source(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    accept_consent(app_modules, db_session, user)
    client = ai_client(
        app_modules,
        db_session,
        user,
        monkeypatch,
        survey_recommendation(),
    )

    response = client.get("/ai/risk/me")

    assert response.status_code == 200
    events = recommendation_events(app_modules, db_session)
    assert len(events) == 1
    event = events[0]
    assert event.user_id == user.id
    assert event.risk_level == "ALTO"
    assert event.recommended_training == "passwords"
    assert event.recommended_scenario == 2
    assert event.source == "survey"
    assert event.evidence_count == 0


def test_repeated_identical_recommendation_is_deduplicated_in_short_window(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    accept_consent(app_modules, db_session, user)
    client = ai_client(
        app_modules,
        db_session,
        user,
        monkeypatch,
        behavioral_recommendation(),
    )

    first_response = client.get("/ai/risk/me")
    second_response = client.get("/ai/risk/me")

    assert first_response.status_code == 200
    assert second_response.status_code == 200
    assert len(recommendation_events(app_modules, db_session)) == 1


def test_recommendation_event_changes_when_evidence_count_changes(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    accept_consent(app_modules, db_session, user)
    result = behavioral_recommendation()
    client = ai_client(app_modules, db_session, user, monkeypatch, result)
    client.get("/ai/risk/me")

    result["behavioral_decisions"] = 4
    client.get("/ai/risk/me")

    assert len(recommendation_events(app_modules, db_session)) == 2


def test_duplicate_policy_allows_same_event_after_window(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    result = behavioral_recommendation()
    first = app_modules.pilot_service.persist_recommendation_event(
        db_session,
        user.id,
        result,
    )
    first.created_at = (
        datetime.utcnow()
        - timedelta(
            seconds=(
                app_modules.pilot_service
                .RECOMMENDATION_DEDUPLICATION_WINDOW_SECONDS
            )
            + 1
        )
    )
    db_session.commit()

    second = app_modules.pilot_service.persist_recommendation_event(
        db_session,
        user.id,
        result,
    )

    assert first.id != second.id
    assert len(recommendation_events(app_modules, db_session)) == 2


def test_ai_risk_keeps_recommendation_algorithm_result(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    expected = behavioral_recommendation()
    client = ai_client(app_modules, db_session, user, monkeypatch, expected)

    actual = client.get("/ai/risk/me").json()

    assert actual["recommended_training"] == expected["recommended_training"]
    assert actual["recommended_scenario"] == expected["recommended_scenario"]
    assert recommendation_events(app_modules, db_session) == []


def test_revoked_consent_keeps_ai_risk_response_without_new_event(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    accept_consent(app_modules, db_session, user)
    app_modules.pilot_service.persist_recommendation_event(
        db_session,
        user.id,
        behavioral_recommendation(),
    )
    app_modules.pilot_service.revoke_pilot_consent(db_session, user.id)
    result = behavioral_recommendation()
    result["behavioral_decisions"] = 4
    client = ai_client(app_modules, db_session, user, monkeypatch, result)

    response = client.get("/ai/risk/me")

    assert response.status_code == 200
    assert response.json() == result
    assert len(recommendation_events(app_modules, db_session)) == 1


def test_reaccept_preserves_participant_code_and_reactivates_events(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    accepted = accept_consent(app_modules, db_session, user)
    app_modules.pilot_service.revoke_pilot_consent(db_session, user.id)
    reaccepted = accept_consent(app_modules, db_session, user)
    client = ai_client(
        app_modules,
        db_session,
        user,
        monkeypatch,
        behavioral_recommendation(),
    )

    response = client.get("/ai/risk/me")

    assert response.status_code == 200
    assert reaccepted["participant_code"] == accepted["participant_code"]
    assert reaccepted["accepted"] is True
    assert reaccepted["revoked_at"] is None
    assert len(recommendation_events(app_modules, db_session)) == 1


def test_survey_source_does_not_persist_without_consent(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    client = ai_client(
        app_modules,
        db_session,
        user,
        monkeypatch,
        survey_recommendation(),
    )

    response = client.get("/ai/risk/me")

    assert response.status_code == 200
    assert recommendation_events(app_modules, db_session) == []


def test_behavioral_source_does_not_persist_without_consent(
    app_modules,
    db_session,
    user,
    monkeypatch,
):
    client = ai_client(
        app_modules,
        db_session,
        user,
        monkeypatch,
        behavioral_recommendation(),
    )

    response = client.get("/ai/risk/me")

    assert response.status_code == 200
    assert recommendation_events(app_modules, db_session) == []


def test_different_users_have_isolated_recommendation_events(
    app_modules,
    db_session,
    user,
    other_user,
    monkeypatch,
):
    accept_consent(app_modules, db_session, user)
    accept_consent(app_modules, db_session, other_user)
    first_client = ai_client(
        app_modules,
        db_session,
        user,
        monkeypatch,
        behavioral_recommendation(),
    )
    second_client = ai_client(
        app_modules,
        db_session,
        other_user,
        monkeypatch,
        behavioral_recommendation(),
    )

    first_response = first_client.get("/ai/risk/me")
    second_response = second_client.get("/ai/risk/me")

    assert first_response.status_code == 200
    assert second_response.status_code == 200
    assert sorted(
        event.user_id
        for event in recommendation_events(app_modules, db_session)
    ) == [user.id, other_user.id]


def test_recommendation_event_does_not_store_pii(app_modules, db_session, user):
    accept_consent(app_modules, db_session, user)
    result = behavioral_recommendation()

    event = app_modules.pilot_service.persist_recommendation_event(
        db_session,
        user.id,
        result,
    )

    assert not hasattr(event, "name")
    assert not hasattr(event, "email")
    assert not hasattr(event, "password")
    assert not hasattr(event, "google_sub")
    assert not hasattr(event, "ip_address")
