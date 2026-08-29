import asyncio
import os
import sys
import types
from datetime import datetime, timedelta
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
    from app.ai import rules
    from app.services import analytics
    from app.services import ai_service
    from app.services import scenario_recommendation_service
    from app.services import topic_taxonomy
    from app.services.ai_service import AIService, MIN_BEHAVIORAL_DECISIONS
    from app.services.survey_service import DIAGNOSTIC_SURVEY_VERSION

    return SimpleNamespace(
        models=models,
        analytics=analytics,
        ai_service=ai_service,
        rules=rules,
        scenario_recommendation_service=scenario_recommendation_service,
        topic_taxonomy=topic_taxonomy,
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


def create_scenario(app_modules, db, scenario_id, category):
    scenario = app_modules.models.Scenario(
        id=scenario_id,
        title=f"Scenario {scenario_id}",
        description="Test scenario",
        difficulty="medium",
        category=category,
        correct_choice="correct",
        points_correct=10,
        points_incorrect=0,
    )
    db.add(scenario)
    db.commit()
    db.refresh(scenario)

    return scenario


def create_decision(
    app_modules,
    db,
    user_id,
    scenario_id,
    *,
    is_correct=0,
    created_at=None,
):
    decision = app_modules.models.Decision(
        user_id=user_id,
        scenario_id=scenario_id,
        choice="incorrect" if not is_correct else "correct",
        is_correct=is_correct,
        points_awarded=0,
        risk_level="medium" if not is_correct else "low",
        feedback="Test feedback",
        created_at=created_at or datetime.utcnow(),
    )
    db.add(decision)
    db.commit()
    db.refresh(decision)

    return decision


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


def stub_analytics(app_modules, monkeypatch, *, most_failed_category):
    def get_analytics(_db, _user_id):
        return {
            "total_points": 50,
            "accuracy": 80.0,
            "risk_index": 15.0,
            "awareness_score": 75.0,
            "decisions_last_7_days": 3,
            "most_failed_category": most_failed_category,
        }

    monkeypatch.setattr(
        app_modules.ai_service,
        "get_user_analytics",
        get_analytics,
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


def test_playable_topic_accepts_supported_topics(app_modules):
    assert app_modules.ai_service.normalize_playable_topic("phishing") == "phishing"
    assert app_modules.ai_service.normalize_playable_topic("passwords") == "passwords"
    assert app_modules.ai_service.normalize_playable_topic("malware") == "malware"
    assert app_modules.ai_service.normalize_playable_topic("wifi") == "wifi"


def test_playable_topic_normalizes_password(app_modules):
    assert app_modules.ai_service.normalize_playable_topic("password") == "passwords"
    assert app_modules.ai_service.normalize_playable_topic("contraseñas") == "passwords"


def test_topic_taxonomy_normalizes_historical_aliases(app_modules):
    taxonomy = app_modules.topic_taxonomy

    assert taxonomy.normalize_topic("network") == "wifi"
    assert taxonomy.normalize_topic("wifi") == "wifi"
    assert taxonomy.normalize_topic("password") == "passwords"
    assert taxonomy.normalize_topic("passwords") == "passwords"
    assert taxonomy.normalize_topic("social_engineering") == "phishing"
    assert taxonomy.normalize_topic("malware") == "malware"
    assert taxonomy.normalize_topic("unknown") is None
    assert taxonomy.normalize_topic("general") is None
    assert taxonomy.normalize_topic(None) is None


def test_rf_category_adapter_documents_malware_fallback(app_modules):
    taxonomy = app_modules.topic_taxonomy

    passwords_mapping = taxonomy.to_rf_category("passwords")
    malware_mapping = taxonomy.to_rf_category("malware")

    assert passwords_mapping.rf_category == "password"
    assert passwords_mapping.used_fallback is False
    assert malware_mapping.rf_category == "phishing"
    assert malware_mapping.used_fallback is True
    assert malware_mapping.reason == "malware_without_rf_category"


def test_rules_never_recommends_historical_scenario_4(app_modules):
    recommendation = app_modules.rules.get_recommendation("network")

    assert recommendation["training"] == "wifi"
    assert recommendation["scenario"] == 7


def test_general_is_resolved_to_playable_topic(app_modules):
    assert (
        app_modules.ai_service.resolve_playable_topic("general", 1, None)
        == "phishing"
    )


def test_null_topic_is_resolved_to_playable_topic(app_modules):
    assert (
        app_modules.ai_service.resolve_playable_topic(None, None, None)
        == "phishing"
    )


def test_unknown_topic_is_resolved_to_playable_topic(app_modules):
    assert (
        app_modules.ai_service.resolve_playable_topic("unknown", None, "malware")
        == "malware"
    )


def test_selector_uses_canonical_candidate_lists(app_modules):
    selector = app_modules.scenario_recommendation_service.select_playable_scenario

    assert selector(None, None, "phishing") == 1
    assert selector(None, None, "passwords") == 2
    assert selector(None, None, "malware") == 3
    assert selector(None, None, "wifi") == 7
    assert selector(None, None, "network") == 7
    assert selector(None, None, "unknown") == 1


def test_selector_prefers_less_practiced_candidate(app_modules, db):
    user = create_user(app_modules, db)
    now = datetime.utcnow()

    for index in range(5):
        create_decision(
            app_modules,
            db,
            user.id,
            1,
            created_at=now - timedelta(minutes=10 + index),
        )

    create_decision(
        app_modules,
        db,
        user.id,
        5,
        created_at=now - timedelta(minutes=20),
    )

    selected = (
        app_modules.scenario_recommendation_service.select_playable_scenario(
            db,
            user.id,
            "phishing",
        )
    )

    assert selected == 5


def test_selector_avoids_immediate_repeat_when_possible(app_modules, db):
    user = create_user(app_modules, db)
    now = datetime.utcnow()

    for index in range(4):
        create_decision(
            app_modules,
            db,
            user.id,
            1,
            created_at=now - timedelta(minutes=20 + index),
        )

    create_decision(
        app_modules,
        db,
        user.id,
        5,
        created_at=now,
    )

    selected = (
        app_modules.scenario_recommendation_service.select_playable_scenario(
            db,
            user.id,
            "phishing",
        )
    )

    assert selected == 1


def test_analytics_aggregates_historical_categories(app_modules, db):
    user = create_user(
        app_modules,
        db,
        total_decisions=3,
        correct_decisions=0,
    )
    create_scenario(app_modules, db, 4, "network")
    create_scenario(app_modules, db, 7, "wifi")
    create_scenario(app_modules, db, 2, "password")

    create_decision(app_modules, db, user.id, 4)
    create_decision(app_modules, db, user.id, 7)
    create_decision(app_modules, db, user.id, 2)

    analytics = app_modules.analytics.get_user_analytics(db, user.id)

    assert analytics["most_failed_category"] == "wifi"


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


def test_survey_primary_weakness_none_maps_to_playable_topic(
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
    assert data["recommended_training"] == "phishing"
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
    assert data["recommended_training"] == "phishing"
    assert data["recommended_scenario"] == 1
    assert data["sufficient_behavioral_data"] is True


def test_perfect_user_does_not_receive_general_training(
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
        probability=0.95,
    )
    user = create_user(
        app_modules,
        db,
        total_decisions=6,
        correct_decisions=6,
        total_points=100,
    )

    data = run_prediction(app_modules, db, user.id)

    assert len(calls) == 1
    assert data["risk_source"] == "random_forest"
    assert data["risk_level"] == "BAJO"
    assert data["recommended_training"] == "phishing"
    assert data["recommended_scenario"] == 1
    assert data["message"] == (
        "Excelente desempeño. No se detectaron áreas críticas de mejora."
    )


def test_existing_malware_recommendation_is_unchanged(
    app_modules,
    db,
    monkeypatch,
):
    calls = []
    stub_predictor(app_modules, monkeypatch, calls)
    stub_analytics(app_modules, monkeypatch, most_failed_category="malware")
    user = create_user(
        app_modules,
        db,
        total_decisions=4,
        correct_decisions=2,
        total_points=30,
    )

    data = run_prediction(app_modules, db, user.id)

    assert len(calls) == 1
    assert data["recommended_training"] == "malware"
    assert data["recommended_scenario"] == 3


def test_existing_passwords_recommendation_is_unchanged(
    app_modules,
    db,
    monkeypatch,
):
    calls = []
    stub_predictor(app_modules, monkeypatch, calls)
    stub_analytics(app_modules, monkeypatch, most_failed_category="passwords")
    user = create_user(
        app_modules,
        db,
        total_decisions=4,
        correct_decisions=2,
        total_points=30,
    )

    data = run_prediction(app_modules, db, user.id)

    assert len(calls) == 1
    assert data["recommended_training"] == "passwords"
    assert data["recommended_scenario"] == 2


def test_passwords_reaches_rf_as_password_category(
    app_modules,
    db,
    monkeypatch,
):
    calls = []
    stub_predictor(app_modules, monkeypatch, calls)
    stub_analytics(app_modules, monkeypatch, most_failed_category="passwords")
    user = create_user(
        app_modules,
        db,
        total_decisions=4,
        correct_decisions=2,
        total_points=30,
    )

    data = run_prediction(app_modules, db, user.id)

    assert calls[0]["most_failed_category"] == "password"
    assert data["recommended_training"] == "passwords"
    assert data["recommended_scenario"] in (2, 6)


def test_network_historical_category_recommends_wifi_scenario_7(
    app_modules,
    db,
    monkeypatch,
):
    calls = []
    stub_predictor(app_modules, monkeypatch, calls)
    stub_analytics(app_modules, monkeypatch, most_failed_category="network")
    user = create_user(
        app_modules,
        db,
        total_decisions=4,
        correct_decisions=2,
        total_points=30,
    )

    data = run_prediction(app_modules, db, user.id)

    assert calls[0]["most_failed_category"] == "wifi"
    assert data["recommended_training"] == "wifi"
    assert data["recommended_scenario"] == 7


def test_wifi_category_recommends_scenario_7(
    app_modules,
    db,
    monkeypatch,
):
    calls = []
    stub_predictor(app_modules, monkeypatch, calls)
    stub_analytics(app_modules, monkeypatch, most_failed_category="wifi")
    user = create_user(
        app_modules,
        db,
        total_decisions=4,
        correct_decisions=2,
        total_points=30,
    )

    data = run_prediction(app_modules, db, user.id)

    assert calls[0]["most_failed_category"] == "wifi"
    assert data["recommended_training"] == "wifi"
    assert data["recommended_scenario"] == 7


def test_malware_rf_fallback_is_explicit_and_recommendation_stays_malware(
    app_modules,
    db,
    monkeypatch,
    caplog,
):
    calls = []
    caplog.set_level("WARNING", logger="protectpyme")
    stub_predictor(app_modules, monkeypatch, calls)
    stub_analytics(app_modules, monkeypatch, most_failed_category="malware")
    user = create_user(
        app_modules,
        db,
        total_decisions=4,
        correct_decisions=2,
        total_points=30,
    )

    data = run_prediction(app_modules, db, user.id)

    assert calls[0]["most_failed_category"] == "phishing"
    assert "malware_without_rf_category" in caplog.text
    assert data["recommended_training"] == "malware"
    assert data["recommended_scenario"] == 3


def test_ai_risk_never_returns_non_playable_scenario_4(
    app_modules,
    db,
    monkeypatch,
):
    calls = []
    stub_predictor(app_modules, monkeypatch, calls)
    stub_analytics(app_modules, monkeypatch, most_failed_category="network")
    user = create_user(
        app_modules,
        db,
        total_decisions=4,
        correct_decisions=2,
        total_points=30,
    )

    data = run_prediction(app_modules, db, user.id)

    assert data["recommended_training"] != "general"
    assert data["recommended_scenario"] != 4
    assert data["recommended_scenario"] in (1, 2, 3, 5, 6, 7)
