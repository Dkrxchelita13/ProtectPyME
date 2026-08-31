import os
import sys
import types
import uuid
from types import SimpleNamespace

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import declarative_base, sessionmaker
from sqlalchemy.pool import StaticPool


os.environ.setdefault("SECRET_KEY", "test-secret")

def install_fake_database():
    _test_base = declarative_base()
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
    fake_database.Base = _test_base
    fake_database.engine = fake_engine
    fake_database.SessionLocal = testing_session_local
    fake_database.get_db = get_test_db
    sys.modules["app.database"] = fake_database


@pytest.fixture
def app_modules():
    managed_modules = (
        "app.database",
        "app.models",
    )
    previous_modules = {
        name: sys.modules.get(name)
        for name in managed_modules
    }

    if "app.database" not in sys.modules:
        install_fake_database()

    from app import models, schemas
    from app.services import concept_mastery_service
    from app.services import minigame_session_service
    from app.services.concept_catalog import CONCEPT_CATALOG

    try:
        yield SimpleNamespace(
            models=models,
            schemas=schemas,
            concept_mastery_service=concept_mastery_service,
            minigame_session_service=minigame_session_service,
            concept_catalog=CONCEPT_CATALOG,
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
    globals()["schemas"] = app_modules.schemas
    globals()["concept_mastery_service"] = app_modules.concept_mastery_service
    globals()["minigame_session_service"] = app_modules.minigame_session_service
    globals()["CONCEPT_CATALOG"] = app_modules.concept_catalog


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
def mastery_app(db):
    managed_modules = (
        "app.routes.auth",
        "app.routes.minigames",
    )
    previous_modules = {
        name: sys.modules.get(name)
        for name in managed_modules
    }

    from app.routes import minigames as minigames_route
    app = FastAPI()
    app.include_router(minigames_route.router)

    def override_db():
        yield db

    app.dependency_overrides[minigames_route.get_db] = override_db

    try:
        yield SimpleNamespace(
            app=app,
            get_current_user=minigames_route.get_current_user,
        )
    finally:
        for name, module in previous_modules.items():
            if module is None:
                sys.modules.pop(name, None)
            else:
                sys.modules[name] = module


def create_user(db, email="mastery@example.com"):
    user = models.User(
        name="Mastery User",
        email=email,
        password="not-used",
    )
    db.add(user)
    db.commit()
    db.refresh(user)
    return user


def create_session_record(
    db,
    user_id,
    topic="passwords",
    risk="bajo",
    minigame="crossword",
    concept_ids=None,
):
    session = models.MinigameSessionRecord(
        id=str(uuid.uuid4()),
        user_id=user_id,
        topic=topic,
        risk=risk,
        minigame=minigame,
        item_ids=["item-1"],
        concept_ids=concept_ids or ["passwords.salt"],
        status="started",
    )
    db.add(session)
    db.commit()
    db.refresh(session)
    return session


def add_attempt(
    db,
    session,
    user_id,
    concept_ids=None,
    difficulty="bajo",
    correct=True,
    item_id="item-1",
    attempt_number=1,
):
    attempt = models.MinigameAttempt(
        session_id=session.id,
        user_id=user_id,
        item_id=item_id,
        concept_ids=concept_ids or ["passwords.salt"],
        difficulty=difficulty,
        correct=correct,
        response_time_ms=1000,
        attempt_number=attempt_number,
        points_delta=10 if correct else 0,
    )
    db.add(attempt)
    db.commit()
    db.refresh(attempt)
    return attempt


def complete_session(db, user_id, session_id):
    return minigame_session_service.complete_minigame_session(
        db=db,
        user_id=user_id,
        session_id=session_id,
    )


def mastery_record(db, user_id, concept_id):
    return (
        db.query(models.UserConceptMastery)
        .filter(
            models.UserConceptMastery.user_id == user_id,
            models.UserConceptMastery.concept_id == concept_id,
        )
        .one()
    )


def test_initial_mastery_values_without_practice(db):
    user = create_user(db)

    result = concept_mastery_service.get_user_concept_mastery(
        db,
        user.id,
        "passwords.salt",
    )

    assert result["mastery_score"] == 50.0
    assert result["mastery_level"] == "sin_datos"
    assert result["attempt_count"] == 0
    assert result["last_practiced_at"] is None


def test_difficulty_weights_are_defined_and_validated():
    assert concept_mastery_service.get_difficulty_weight("bajo") == 1.0
    assert concept_mastery_service.get_difficulty_weight("medio") == 1.25
    assert concept_mastery_service.get_difficulty_weight("alto") == 1.5

    with pytest.raises(ValueError):
        concept_mastery_service.get_difficulty_weight("extremo")


def test_correct_attempt_updates_alpha_score_and_counts(db):
    user = create_user(db)
    session = create_session_record(db, user.id)
    add_attempt(db, session, user.id, difficulty="bajo", correct=True)

    complete_session(db, user.id, session.id)
    record = mastery_record(db, user.id, "passwords.salt")

    assert record.alpha == 3.0
    assert record.beta == 2.0
    assert record.mastery_score == 60.0
    assert record.attempt_count == 1
    assert record.correct_count == 1
    assert record.incorrect_count == 0
    assert record.evidence_weight == 1.0
    assert record.topic == "passwords"


def test_incorrect_attempt_updates_beta_score_and_counts(db):
    user = create_user(db)
    session = create_session_record(db, user.id)
    add_attempt(db, session, user.id, difficulty="alto", correct=False)

    complete_session(db, user.id, session.id)
    record = mastery_record(db, user.id, "passwords.salt")

    assert record.alpha == 2.0
    assert record.beta == 3.5
    assert record.mastery_score == 36.36
    assert record.attempt_count == 1
    assert record.correct_count == 0
    assert record.incorrect_count == 1
    assert record.evidence_weight == 1.5


@pytest.mark.parametrize(
    ("topic", "concept_id"),
    (
        ("passwords", "passwords.credential_request"),
        ("passwords", "passwords.identity_verification"),
        ("wifi", "wifi.suspicious_traffic"),
        ("wifi", "wifi.data_exfiltration"),
    ),
)
def test_new_scenario_concepts_update_beta_on_incorrect_attempt(db, topic, concept_id):
    user = create_user(db)
    session = create_session_record(
        db,
        user.id,
        topic=topic,
        risk="medio",
        minigame="quiz",
        concept_ids=[concept_id],
    )
    add_attempt(
        db,
        session,
        user.id,
        concept_ids=[concept_id],
        difficulty="medio",
        correct=False,
    )

    complete_session(db, user.id, session.id)
    record = mastery_record(db, user.id, concept_id)

    assert record.topic == topic
    assert record.alpha == 2.0
    assert record.beta == 3.25
    assert record.mastery_score == 38.1
    assert record.attempt_count == 1
    assert record.correct_count == 0
    assert record.incorrect_count == 1
    assert record.evidence_weight == 1.25


def test_duplicate_concepts_in_same_attempt_are_counted_once(db):
    user = create_user(db)
    session = create_session_record(db, user.id)
    add_attempt(
        db,
        session,
        user.id,
        concept_ids=["passwords.salt", "passwords.salt"],
        correct=True,
    )

    complete_session(db, user.id, session.id)
    record = mastery_record(db, user.id, "passwords.salt")

    assert record.attempt_count == 1
    assert record.correct_count == 1
    assert record.evidence_weight == 1.0


def test_multiple_concepts_in_same_attempt_are_updated_independently(db):
    user = create_user(db)
    session = create_session_record(
        db,
        user.id,
        concept_ids=["passwords.salt", "passwords.hash"],
    )
    add_attempt(
        db,
        session,
        user.id,
        concept_ids=["passwords.salt", "passwords.hash"],
        difficulty="medio",
        correct=True,
    )

    complete_session(db, user.id, session.id)

    salt = mastery_record(db, user.id, "passwords.salt")
    hash_record = mastery_record(db, user.id, "passwords.hash")
    assert salt.mastery_score == 61.9
    assert hash_record.mastery_score == 61.9
    assert salt.evidence_weight == 1.25
    assert hash_record.evidence_weight == 1.25


def test_mastery_level_thresholds():
    assert concept_mastery_service.get_mastery_level(50, 0) == "sin_datos"
    assert concept_mastery_service.get_mastery_level(49.99, 1) == (
        "necesita_refuerzo"
    )
    assert concept_mastery_service.get_mastery_level(50, 1) == "en_desarrollo"
    assert concept_mastery_service.get_mastery_level(74.99, 1) == (
        "en_desarrollo"
    )
    assert concept_mastery_service.get_mastery_level(75, 1) == "dominado"


def test_mastery_updates_only_when_session_is_completed_once(db):
    user = create_user(db)
    session = create_session_record(db, user.id)
    add_attempt(db, session, user.id)

    complete_session(db, user.id, session.id)

    second_summary = complete_session(db, user.id, session.id)

    record = mastery_record(db, user.id, "passwords.salt")
    assert record.attempt_count == 1
    assert record.correct_count == 1
    assert record.evidence_weight == 1.0
    assert second_summary["status"] == "completed"


def test_session_without_attempts_does_not_create_mastery_rows(db):
    user = create_user(db)
    session = create_session_record(db, user.id)

    summary = complete_session(db, user.id, session.id)

    assert summary["total_attempts"] == 0
    assert db.query(models.UserConceptMastery).count() == 0


def test_invalid_concept_rolls_back_session_completion(db):
    user = create_user(db)
    session = create_session_record(
        db,
        user.id,
        concept_ids=["passwords.unknown"],
    )
    add_attempt(
        db,
        session,
        user.id,
        concept_ids=["passwords.unknown"],
    )

    with pytest.raises(minigame_session_service.MinigameSessionValidationError):
        complete_session(db, user.id, session.id)

    db.refresh(session)
    assert session.status == "started"
    assert session.completed_at is None
    assert db.query(models.UserConceptMastery).count() == 0


def test_completion_integrity_error_logs_safe_postgres_details(
    db,
    caplog,
    monkeypatch,
):
    class FakeDiag:
        constraint_name = "uq_user_concept_mastery_user_concept"

    class FakePostgresIntegrityError(Exception):
        pgcode = "23505"
        diag = FakeDiag()

    user = create_user(db)
    session = create_session_record(db, user.id)
    add_attempt(db, session, user.id)

    def fail_commit():
        raise IntegrityError(
            "UPDATE minigame_session_records",
            {},
            FakePostgresIntegrityError(),
        )

    monkeypatch.setattr(db, "commit", fail_commit)
    caplog.set_level("WARNING", logger="protectpyme")

    with pytest.raises(
        minigame_session_service.MinigameSessionConflictError
    ) as exc_info:
        complete_session(db, user.id, session.id)

    assert str(exc_info.value) == "Minigame session could not be completed."
    assert "[MINIGAME COMPLETE REJECTED]" in caplog.text
    assert f"session={session.id}" in caplog.text
    assert f"user_id={user.id}" in caplog.text
    assert "status=completed" in caplog.text
    assert "attempts=1" in caplog.text
    assert "reason=integrity_error" in caplog.text
    assert "sqlstate=23505" in caplog.text
    assert "constraint=uq_user_concept_mastery_user_concept" in caplog.text
    assert "orig_exception=FakePostgresIntegrityError" in caplog.text


def test_complete_response_contract_is_unchanged(db):
    user = create_user(db)
    session = create_session_record(db, user.id)
    add_attempt(db, session, user.id)

    summary = complete_session(db, user.id, session.id)

    assert set(summary) == set(schemas.MinigameSessionSummaryResponse.model_fields)
    assert summary["status"] == "completed"


def test_mastery_endpoint_requires_authentication(mastery_app):
    response = TestClient(mastery_app.app).get("/minigames/mastery")

    assert response.status_code in (401, 403)


def test_mastery_endpoint_returns_current_user_only(db, mastery_app):
    user_one = create_user(db, "one@example.com")
    user_two = create_user(db, "two@example.com")
    session = create_session_record(db, user_one.id)
    add_attempt(db, session, user_one.id, correct=False)
    complete_session(db, user_one.id, session.id)

    mastery_app.app.dependency_overrides[mastery_app.get_current_user] = (
        lambda: SimpleNamespace(id=user_two.id)
    )
    response = TestClient(mastery_app.app).get(
        "/minigames/mastery",
        params={"topic": "passwords", "include_unpracticed": False},
    )

    assert response.status_code == 200
    data = response.json()
    assert data["practiced_concepts"] == 0
    assert data["concepts"] == []


def test_mastery_endpoint_includes_unpracticed_concepts(db, mastery_app):
    user = create_user(db)
    mastery_app.app.dependency_overrides[mastery_app.get_current_user] = (
        lambda: SimpleNamespace(id=user.id)
    )

    response = TestClient(mastery_app.app).get(
        "/minigames/mastery",
        params={"topic": "passwords"},
    )

    assert response.status_code == 200
    data = response.json()
    password_concepts = [
        concept
        for concept in CONCEPT_CATALOG.values()
        if concept["topic"] == "passwords"
    ]
    assert data["total_concepts"] == len(password_concepts)
    assert data["practiced_concepts"] == 0
    assert data["concepts"][0]["mastery_level"] == "sin_datos"
    assert data["concepts"][0]["mastery_score"] == 50.0


def test_mastery_endpoint_can_exclude_unpracticed_concepts(db, mastery_app):
    user = create_user(db)
    session = create_session_record(db, user.id)
    add_attempt(db, session, user.id, correct=True)
    complete_session(db, user.id, session.id)
    mastery_app.app.dependency_overrides[mastery_app.get_current_user] = (
        lambda: SimpleNamespace(id=user.id)
    )

    response = TestClient(mastery_app.app).get(
        "/minigames/mastery",
        params={"topic": "passwords", "include_unpracticed": False},
    )

    assert response.status_code == 200
    data = response.json()
    assert data["total_concepts"] == 1
    assert data["practiced_concepts"] == 1
    assert data["concepts"][0]["concept_id"] == "passwords.salt"
    assert data["concepts"][0]["term"] == "Salt"


def test_mastery_endpoint_rejects_invalid_topic(db, mastery_app):
    user = create_user(db)
    mastery_app.app.dependency_overrides[mastery_app.get_current_user] = (
        lambda: SimpleNamespace(id=user.id)
    )

    response = TestClient(mastery_app.app).get(
        "/minigames/mastery",
        params={"topic": "social"},
    )

    assert response.status_code == 400


def test_mastery_endpoint_orders_needs_reinforcement_first(db, mastery_app):
    user = create_user(db)
    session = create_session_record(
        db,
        user.id,
        concept_ids=["passwords.salt", "passwords.hash"],
    )
    add_attempt(
        db,
        session,
        user.id,
        concept_ids=["passwords.salt"],
        correct=False,
        item_id="item-1",
    )
    add_attempt(
        db,
        session,
        user.id,
        concept_ids=["passwords.hash"],
        correct=True,
        item_id="item-2",
        attempt_number=1,
    )
    complete_session(db, user.id, session.id)
    mastery_app.app.dependency_overrides[mastery_app.get_current_user] = (
        lambda: SimpleNamespace(id=user.id)
    )

    response = TestClient(mastery_app.app).get(
        "/minigames/mastery",
        params={"topic": "passwords", "include_unpracticed": False},
    )

    assert response.status_code == 200
    data = response.json()
    assert data["needs_reinforcement_count"] == 1
    assert data["developing_count"] == 1
    assert [
        concept["concept_id"]
        for concept in data["concepts"]
    ] == ["passwords.salt", "passwords.hash"]
