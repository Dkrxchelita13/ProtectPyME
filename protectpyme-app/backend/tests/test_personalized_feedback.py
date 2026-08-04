import os
import sys
import types
import uuid
from types import SimpleNamespace

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base, sessionmaker
from sqlalchemy.pool import StaticPool


os.environ.setdefault("SECRET_KEY", "test-secret")

from app import schemas  # noqa: E402
from app.services import minigame_session_service  # noqa: E402
from app.services import personalized_feedback_service  # noqa: E402


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


@pytest.fixture
def app_modules():
    managed_modules = (
        "app.database",
        "app.models",
        "app.routes.auth",
        "app.routes.minigames",
    )
    previous_modules = {
        name: sys.modules.get(name)
        for name in managed_modules
    }

    if "app.database" not in sys.modules:
        install_fake_database()

    from app import models
    from app.routes import minigames as minigames_route
    from app.services import minigame_service

    try:
        yield SimpleNamespace(
            models=models,
            minigames_route=minigames_route,
            minigame_service=minigame_service,
        )
    finally:
        for name, module in previous_modules.items():
            if module is None:
                sys.modules.pop(name, None)
            else:
                sys.modules[name] = module


@pytest.fixture(autouse=True)
def bind_app_modules(app_modules):
    globals()["models"] = app_modules.models
    globals()["minigames_route"] = app_modules.minigames_route
    globals()["minigame_service"] = app_modules.minigame_service


@pytest.fixture
def db(app_modules):
    active_models = app_modules.models
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

    active_models.Base.metadata.drop_all(bind=engine)
    active_models.Base.metadata.create_all(bind=engine)
    session = testing_session_local()

    try:
        yield session
    finally:
        session.close()
        active_models.Base.metadata.drop_all(bind=engine)


@pytest.fixture
def feedback_app(db):
    app = FastAPI()
    app.include_router(minigames_route.router)

    def override_db():
        yield db

    app.dependency_overrides[minigames_route.get_db] = override_db
    return app


def create_user(db, email="feedback@example.com"):
    user = models.User(
        name="Feedback User",
        email=email,
        password="not-used",
    )
    db.add(user)
    db.commit()
    db.refresh(user)
    return user


def create_session(
    db,
    user_id,
    *,
    status="completed",
    item_ids=None,
    concept_ids=None,
):
    session = models.MinigameSessionRecord(
        id=str(uuid.uuid4()),
        user_id=user_id,
        topic="passwords",
        risk="bajo",
        minigame="crossword",
        item_ids=item_ids or [
            "passwords_bajo_crossword_salt",
            "passwords_bajo_crossword_argon2id",
            "passwords_bajo_crossword_hash",
        ],
        concept_ids=concept_ids or [
            "passwords.salt",
            "passwords.argon2id",
            "passwords.hash",
        ],
        status=status,
    )
    db.add(session)
    db.commit()
    db.refresh(session)
    return session


def add_attempt(
    db,
    session,
    user_id,
    item_id,
    concept_ids,
    *,
    correct,
    attempt_number=1,
    points_delta=10,
):
    attempt = models.MinigameAttempt(
        session_id=session.id,
        user_id=user_id,
        item_id=item_id,
        concept_ids=concept_ids,
        difficulty=session.risk,
        correct=correct,
        response_time_ms=500,
        attempt_number=attempt_number,
        points_delta=points_delta,
    )
    db.add(attempt)
    db.commit()
    db.refresh(attempt)
    return attempt


def add_mastery(db, user_id, concept_id, mastery_score):
    record = models.UserConceptMastery(
        user_id=user_id,
        concept_id=concept_id,
        topic="passwords",
        alpha=2.0,
        beta=2.0,
        mastery_score=mastery_score,
        attempt_count=1,
        correct_count=0,
        incorrect_count=1,
        evidence_weight=1.0,
    )
    db.add(record)
    db.commit()
    db.refresh(record)
    return record


def authenticated_client(feedback_app, user):
    feedback_app.dependency_overrides[minigames_route.get_current_user] = (
        lambda: SimpleNamespace(id=user.id)
    )
    return TestClient(feedback_app)


def create_main_feedback_case(db):
    user = create_user(db)
    session = create_session(db, user.id)
    add_attempt(
        db,
        session,
        user.id,
        "passwords_bajo_crossword_salt",
        ["passwords.salt"],
        correct=True,
        attempt_number=1,
        points_delta=10,
    )
    add_attempt(
        db,
        session,
        user.id,
        "passwords_bajo_crossword_argon2id",
        ["passwords.argon2id"],
        correct=True,
        attempt_number=1,
        points_delta=10,
    )
    add_attempt(
        db,
        session,
        user.id,
        "passwords_bajo_crossword_hash",
        ["passwords.hash"],
        correct=False,
        attempt_number=1,
        points_delta=0,
    )
    add_mastery(db, user.id, "passwords.salt", 60.0)
    add_mastery(db, user.id, "passwords.argon2id", 60.0)
    add_mastery(db, user.id, "passwords.hash", 40.0)
    return user, session


def get_feedback(client, session_id):
    return client.get(f"/minigames/session/{session_id}/feedback")


def without_generated_at(payload):
    copy = dict(payload)
    copy.pop("generated_at", None)
    return copy


def test_performance_level_without_attempts():
    assert personalized_feedback_service.get_performance_level(0, 0) == (
        "sin_evidencia"
    )


def test_performance_level_needs_reinforcement():
    assert personalized_feedback_service.get_performance_level(49.99, 1) == (
        "necesita_refuerzo"
    )


def test_performance_level_developing():
    assert personalized_feedback_service.get_performance_level(50, 1) == (
        "en_desarrollo"
    )
    assert personalized_feedback_service.get_performance_level(69.99, 1) == (
        "en_desarrollo"
    )


def test_performance_level_good_progress():
    assert personalized_feedback_service.get_performance_level(70, 1) == (
        "buen_progreso"
    )
    assert personalized_feedback_service.get_performance_level(84.99, 1) == (
        "buen_progreso"
    )


def test_performance_level_excellent():
    assert personalized_feedback_service.get_performance_level(85, 1) == (
        "excelente"
    )
    assert personalized_feedback_service.get_performance_level(100, 1) == (
        "excelente"
    )


def test_concept_feedback_strength():
    assert personalized_feedback_service.classify_concept_feedback(75, 1, 0) == (
        "fortaleza"
    )


def test_concept_feedback_progress():
    assert personalized_feedback_service.classify_concept_feedback(60, 1, 0) == (
        "avance"
    )


def test_concept_feedback_reinforcement():
    assert personalized_feedback_service.classify_concept_feedback(40, 1, 0) == (
        "refuerzo"
    )
    assert personalized_feedback_service.classify_concept_feedback(60, 0, 1) == (
        "refuerzo"
    )


def test_concept_feedback_point_difficulty():
    assert personalized_feedback_service.classify_concept_feedback(60, 1, 1) == (
        "dificultad_puntual"
    )


def test_recommended_concepts_are_sorted():
    feedback = [
        concept_feedback("passwords.salt", "refuerzo", 45, 1),
        concept_feedback("passwords.hash", "refuerzo", 40, 1),
        concept_feedback("passwords.argon2id", "refuerzo", 40, 0),
    ]

    assert personalized_feedback_service._select_recommended_concept_ids(
        feedback,
        3,
    ) == [
        "passwords.hash",
        "passwords.argon2id",
        "passwords.salt",
    ]


def test_recommended_concepts_are_unique():
    feedback = [
        concept_feedback("passwords.hash", "refuerzo", 40, 1),
        concept_feedback("passwords.hash", "refuerzo", 40, 1),
    ]

    assert personalized_feedback_service._select_recommended_concept_ids(
        feedback,
        2,
    ) == ["passwords.hash"]


def test_recommended_concepts_are_limited_to_three():
    feedback = [
        concept_feedback("passwords.hash", "refuerzo", 40, 1),
        concept_feedback("passwords.salt", "refuerzo", 41, 1),
        concept_feedback("passwords.argon2id", "refuerzo", 42, 1),
        concept_feedback("passwords.password_spraying", "refuerzo", 43, 1),
    ]

    assert len(
        personalized_feedback_service._select_recommended_concept_ids(
            feedback,
            4,
        )
    ) == 3


def concept_feedback(concept_id, status, mastery_score, incorrect):
    return {
        "concept_id": concept_id,
        "status": status,
        "mastery_score": mastery_score,
        "session_incorrect": incorrect,
    }


def test_multiconcept_attempt_is_counted_once_per_concept(db):
    user = create_user(db)
    session = create_session(
        db,
        user.id,
        concept_ids=["passwords.salt", "passwords.hash"],
    )
    add_attempt(
        db,
        session,
        user.id,
        "passwords_bajo_quiz_salt_hash",
        ["passwords.salt", "passwords.hash", "passwords.salt"],
        correct=True,
    )
    add_mastery(db, user.id, "passwords.salt", 60.0)
    add_mastery(db, user.id, "passwords.hash", 60.0)

    feedback = personalized_feedback_service.get_minigame_feedback(
        db,
        user.id,
        session.id,
    )
    attempts_by_concept = {
        item["concept_id"]: item["session_attempts"]
        for item in feedback["strengths"]
    }

    assert attempts_by_concept == {
        "passwords.hash": 1,
        "passwords.salt": 1,
    }


def test_feedback_is_deterministic(db):
    user, session = create_main_feedback_case(db)

    first = personalized_feedback_service.get_minigame_feedback(
        db,
        user.id,
        session.id,
    )
    second = personalized_feedback_service.get_minigame_feedback(
        db,
        user.id,
        session.id,
    )

    assert without_generated_at(first) == without_generated_at(second)


def test_recommended_minigame_rotates():
    assert personalized_feedback_service.get_recommended_minigame(
        "passwords",
        "bajo",
        "quiz",
        [],
    ) == "wordsearch"


def test_recommended_minigame_has_relevant_content():
    assert personalized_feedback_service.get_recommended_minigame(
        "passwords",
        "bajo",
        "crossword",
        ["passwords.hash"],
    ) == "quiz"


def test_feedback_requires_authentication(db, feedback_app):
    response = TestClient(feedback_app).get(
        f"/minigames/session/{uuid.uuid4()}/feedback"
    )

    assert response.status_code in (401, 403)


def test_feedback_returns_404_for_unknown_session(db, feedback_app):
    user = create_user(db)
    client = authenticated_client(feedback_app, user)

    response = get_feedback(client, str(uuid.uuid4()))

    assert response.status_code == 404


def test_feedback_does_not_expose_another_users_session(db, feedback_app):
    owner = create_user(db, "owner@example.com")
    other = create_user(db, "other@example.com")
    session = create_session(db, owner.id)
    client = authenticated_client(feedback_app, other)

    response = get_feedback(client, session.id)

    assert response.status_code == 404


def test_feedback_rejects_started_session(db, feedback_app):
    user = create_user(db)
    session = create_session(db, user.id, status="started")
    client = authenticated_client(feedback_app, user)

    response = get_feedback(client, session.id)

    assert response.status_code == 409


def test_feedback_for_completed_session(db, feedback_app):
    user, session = create_main_feedback_case(db)
    client = authenticated_client(feedback_app, user)

    response = get_feedback(client, session.id)
    data = response.json()

    assert response.status_code == 200
    assert data["performance_level"] == "en_desarrollo"
    assert data["accuracy"] == 66.67
    assert data["recommended_concept_ids"][0] == "passwords.hash"
    assert data["recommended_topic"] == "passwords"
    assert data["recommended_minigame"] != "crossword"
    assert data["reinforcement"][0]["concept_id"] == "passwords.hash"
    assert {
        item["concept_id"]
        for item in data["strengths"]
    } == {"passwords.argon2id", "passwords.salt"}


def test_feedback_for_session_without_attempts(db, feedback_app):
    user = create_user(db)
    session = create_session(db, user.id)
    client = authenticated_client(feedback_app, user)

    response = get_feedback(client, session.id)
    data = response.json()

    assert response.status_code == 200
    assert data["performance_level"] == "sin_evidencia"
    assert data["accuracy"] == 0
    assert data["strengths"] == []
    assert data["reinforcement"] == []
    assert data["recommended_concept_ids"] == []
    assert db.query(models.UserConceptMastery).count() == 0


def test_feedback_uses_current_users_mastery(db, feedback_app):
    user, session = create_main_feedback_case(db)
    other = create_user(db, "other-mastery@example.com")
    add_mastery(db, other.id, "passwords.hash", 90.0)
    client = authenticated_client(feedback_app, user)

    data = get_feedback(client, session.id).json()

    assert data["reinforcement"][0]["concept_id"] == "passwords.hash"
    assert data["reinforcement"][0]["mastery_score"] == 40.0


def test_feedback_contains_catalog_terms(db, feedback_app):
    user, session = create_main_feedback_case(db)
    client = authenticated_client(feedback_app, user)

    data = get_feedback(client, session.id).json()
    terms = {
        item["term"]
        for item in data["strengths"] + data["reinforcement"]
    }

    assert {"Salt", "Argon2id", "Hash"}.issubset(terms)


def test_feedback_does_not_include_correct_answers(db, feedback_app):
    user, session = create_main_feedback_case(db)
    client = authenticated_client(feedback_app, user)

    data = get_feedback(client, session.id).json()

    assert "answer" not in str(data).lower()
    assert "options" not in str(data).lower()
    assert "jwt" not in str(data).lower()


def test_feedback_does_not_change_complete_contract(db):
    user = create_user(db)
    session = create_session(db, user.id, status="started")
    add_attempt(
        db,
        session,
        user.id,
        "passwords_bajo_crossword_salt",
        ["passwords.salt"],
        correct=True,
    )

    summary = minigame_session_service.complete_minigame_session(
        db,
        user.id,
        session.id,
    )

    assert set(summary) == set(schemas.MinigameSessionSummaryResponse.model_fields)


def test_feedback_does_not_modify_mastery(db, feedback_app):
    user, session = create_main_feedback_case(db)
    before = mastery_snapshot(db, user.id)
    client = authenticated_client(feedback_app, user)

    response = get_feedback(client, session.id)
    after = mastery_snapshot(db, user.id)

    assert response.status_code == 200
    assert after == before


def test_feedback_legacy_endpoints_remain_unchanged(db, feedback_app):
    user = create_user(db)
    client = authenticated_client(feedback_app, user)

    quiz = client.get(
        "/minigames/quiz",
        params={"topic": "passwords", "risk": "bajo"},
    ).json()
    wordsearch = client.get(
        "/minigames/wordsearch",
        params={"topic": "passwords", "risk": "bajo"},
    ).json()
    crossword = client.get(
        "/minigames/crossword",
        params={"topic": "passwords", "risk": "bajo"},
    ).json()

    assert isinstance(quiz[0]["answer"], int)
    assert isinstance(wordsearch[0]["answer"], str)
    assert isinstance(crossword[0]["answer"], str)


def mastery_snapshot(db, user_id):
    rows = (
        db.query(models.UserConceptMastery)
        .filter(models.UserConceptMastery.user_id == user_id)
        .order_by(models.UserConceptMastery.concept_id)
        .all()
    )
    return [
        (
            row.concept_id,
            row.mastery_score,
            row.attempt_count,
            row.correct_count,
            row.incorrect_count,
        )
        for row in rows
    ]
