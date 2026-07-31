import asyncio
import os
import sys
import types
from types import SimpleNamespace

import pytest
from fastapi import HTTPException
from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base, sessionmaker
from sqlalchemy.pool import StaticPool


os.environ.setdefault("SECRET_KEY", "test-secret")


@pytest.fixture(scope="module")
def app_modules():
    if "app.models" not in sys.modules:
        test_base = declarative_base()
        fake_engine = create_engine(
            "sqlite://",
            connect_args={"check_same_thread": False},
            poolclass=StaticPool,
        )
        testing_session_local = sessionmaker(
            autocommit=False,
            autoflush=False,
            bind=fake_engine,
        )

        def get_test_db():
            db = testing_session_local()

            try:
                yield db
            finally:
                db.close()

        fake_database = types.ModuleType("app.database")
        fake_database.Base = test_base
        fake_database.engine = fake_engine
        fake_database.SessionLocal = testing_session_local
        fake_database.get_db = get_test_db
        sys.modules["app.database"] = fake_database

    from app import models
    from app.services import ai_service
    from app.services.ai_service import AIService, MIN_BEHAVIORAL_DECISIONS
    from app.services.survey_service import DIAGNOSTIC_SURVEY_VERSION

    return SimpleNamespace(
        models=models,
        ai_service=ai_service,
        AIService=AIService,
        MIN_BEHAVIORAL_DECISIONS=MIN_BEHAVIORAL_DECISIONS,
        DIAGNOSTIC_SURVEY_VERSION=DIAGNOSTIC_SURVEY_VERSION,
    )


@pytest.fixture
def db(app_modules):
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

    app_modules.models.Base.metadata.drop_all(bind=engine)
    app_modules.models.Base.metadata.create_all(bind=engine)
    session = testing_session_local()

    try:
        yield session
    finally:
        session.close()
        app_modules.models.Base.metadata.drop_all(bind=engine)


def run_prediction(app_modules, db, user_id):
    return asyncio.run(
        app_modules.AIService.get_user_risk_prediction(
            db=db,
            user_id=user_id,
        )
    )


def create_user(
    app_modules,
    db,
    *,
    total_decisions=0,
    correct_decisions=0,
    total_points=0,
    risk_score=0,
):
    user = app_modules.models.User(
        name="Hybrid User",
        email=f"hybrid-{total_decisions}-{correct_decisions}@example.com",
        password="not-used",
        total_decisions=total_decisions,
        correct_decisions=correct_decisions,
        total_points=total_points,
        risk_score=risk_score,
    )
    db.add(user)
    db.commit()
    db.refresh(user)

    return user


def create_survey_submission(
    app_modules,
    db,
    user_id,
    *,
    primary_weakness,
    initial_risk,
):
    submission = app_modules.models.SurveySubmission(
        user_id=user_id,
        survey_version=app_modules.DIAGNOSTIC_SURVEY_VERSION,
        primary_weakness=primary_weakness,
        initial_risk=initial_risk,
        phishing_score=0,
        passwords_score=0,
        malware_score=0,
        phishing_risk_score=0,
        passwords_risk_score=0,
        malware_risk_score=0,
        total_risk_score=0,
    )
    db.add(submission)
    db.commit()
    db.refresh(submission)

    return submission


def fail_if_predictor_is_called(app_modules, monkeypatch):
    def fail(_features):
        raise AssertionError("Random Forest should not be called")

    monkeypatch.setattr(
        app_modules.ai_service.predictor,
        "predict_risk",
        fail,
    )


def stub_predictor(
    app_modules,
    monkeypatch,
    calls,
    risk_level="MEDIO",
    probability=0.73,
):
    def predict(features):
        calls.append(features)
        return {
            "risk_level": risk_level,
            "probability": probability,
        }

    monkeypatch.setattr(
        app_modules.ai_service.predictor,
        "predict_risk",
        predict,
    )


def assert_base_contract(data):
    existing_fields = {
        "user_id",
        "risk_level",
        "probability",
        "recommended_training",
        "recommended_scenario",
        "message",
    }
    new_fields = {
        "risk_source",
        "behavioral_decisions",
        "min_behavioral_decisions",
        "sufficient_behavioral_data",
    }

    assert existing_fields.issubset(data)
    assert new_fields.issubset(data)


def test_zero_decisions_uses_password_survey_without_predictor(
    app_modules,
    db,
    monkeypatch,
):
    fail_if_predictor_is_called(app_modules, monkeypatch)
    user = create_user(app_modules, db, total_decisions=0)
    create_survey_submission(
        app_modules,
        db,
        user.id,
        primary_weakness="passwords",
        initial_risk="ALTO",
    )

    data = run_prediction(app_modules, db, user.id)

    assert_base_contract(data)
    assert data["risk_source"] == "survey"
    assert data["risk_level"] == "ALTO"
    assert data["recommended_training"] == "passwords"
    assert data["recommended_scenario"] == 2
    assert data["probability"] == 0.0
    assert data["behavioral_decisions"] == 0
    assert data["min_behavioral_decisions"] == (
        app_modules.MIN_BEHAVIORAL_DECISIONS
    )
    assert data["sufficient_behavioral_data"] is False


def test_two_decisions_still_uses_malware_survey(
    app_modules,
    db,
    monkeypatch,
):
    fail_if_predictor_is_called(app_modules, monkeypatch)
    user = create_user(
        app_modules,
        db,
        total_decisions=2,
        correct_decisions=1,
    )
    create_survey_submission(
        app_modules,
        db,
        user.id,
        primary_weakness="malware",
        initial_risk="MEDIO",
    )

    data = run_prediction(app_modules, db, user.id)

    assert data["risk_source"] == "survey"
    assert data["risk_level"] == "MEDIO"
    assert data["recommended_training"] == "malware"
    assert data["recommended_scenario"] == 3
    assert data["behavioral_decisions"] == 2
    assert data["sufficient_behavioral_data"] is False


def test_three_decisions_uses_random_forest(app_modules, db, monkeypatch):
    calls = []
    stub_predictor(
        app_modules,
        monkeypatch,
        calls,
        risk_level="MEDIO",
        probability=0.66,
    )
    user = create_user(
        app_modules,
        db,
        total_decisions=app_modules.MIN_BEHAVIORAL_DECISIONS,
        correct_decisions=2,
        total_points=30,
    )
    create_survey_submission(
        app_modules,
        db,
        user.id,
        primary_weakness="passwords",
        initial_risk="ALTO",
    )

    data = run_prediction(app_modules, db, user.id)

    assert_base_contract(data)
    assert len(calls) == 1
    assert calls[0]["total_decisions"] == (
        app_modules.MIN_BEHAVIORAL_DECISIONS
    )
    assert data["risk_source"] == "random_forest"
    assert data["risk_level"] == "MEDIO"
    assert data["probability"] == 0.66
    assert data["behavioral_decisions"] == (
        app_modules.MIN_BEHAVIORAL_DECISIONS
    )
    assert data["min_behavioral_decisions"] == (
        app_modules.MIN_BEHAVIORAL_DECISIONS
    )
    assert data["sufficient_behavioral_data"] is True


def test_more_than_three_decisions_uses_random_forest(
    app_modules,
    db,
    monkeypatch,
):
    calls = []
    stub_predictor(
        app_modules,
        monkeypatch,
        calls,
        risk_level="BAJO",
        probability=0.81,
    )
    user = create_user(
        app_modules,
        db,
        total_decisions=5,
        correct_decisions=5,
        total_points=80,
    )

    data = run_prediction(app_modules, db, user.id)

    assert len(calls) == 1
    assert data["risk_source"] == "random_forest"
    assert data["sufficient_behavioral_data"] is True
    assert data["behavioral_decisions"] == 5
    assert data["probability"] == 0.81


def test_survey_primary_weakness_none_maps_to_general(
    app_modules,
    db,
    monkeypatch,
):
    fail_if_predictor_is_called(app_modules, monkeypatch)
    user = create_user(app_modules, db, total_decisions=0)
    create_survey_submission(
        app_modules,
        db,
        user.id,
        primary_weakness="none",
        initial_risk="BAJO",
    )

    data = run_prediction(app_modules, db, user.id)

    assert data["risk_source"] == "survey"
    assert data["recommended_training"] == "general"
    assert data["recommended_scenario"] == 1
    assert "no detect" in data["message"].lower()


def test_low_behavioral_data_without_survey_returns_conflict(
    app_modules,
    db,
    monkeypatch,
):
    fail_if_predictor_is_called(app_modules, monkeypatch)
    user = create_user(app_modules, db, total_decisions=0)

    with pytest.raises(HTTPException) as exc_info:
        run_prediction(app_modules, db, user.id)

    assert exc_info.value.status_code == 409
    assert (
        exc_info.value.detail
        == "Diagnostic survey required before risk evaluation."
    )


def test_random_forest_contract_preserves_existing_behavior(
    app_modules,
    db,
    monkeypatch,
):
    calls = []
    stub_predictor(
        app_modules,
        monkeypatch,
        calls,
        risk_level="ALTO",
        probability=0.91,
    )
    user = create_user(
        app_modules,
        db,
        total_decisions=4,
        correct_decisions=1,
        total_points=10,
    )

    data = run_prediction(app_modules, db, user.id)

    assert_base_contract(data)
    assert len(calls) == 1
    assert data["risk_source"] == "random_forest"
    assert data["risk_level"] == "ALTO"
    assert data["probability"] == 0.91
    assert data["recommended_training"] == "general"
    assert data["recommended_scenario"] == 1
    assert data["sufficient_behavioral_data"] is True
