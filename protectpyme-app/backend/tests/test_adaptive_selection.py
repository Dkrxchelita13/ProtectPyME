import copy
import os
import sys
import types
import uuid
from datetime import datetime, timedelta
from types import SimpleNamespace

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base, sessionmaker
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
    from app.services import adaptive_selection_service
    from app.services import minigame_service
    from app.services import minigame_session_service
    from app.services.concept_catalog import get_concepts

    try:
        yield SimpleNamespace(
            models=models,
            minigames_route=minigames_route,
            adaptive_selection_service=adaptive_selection_service,
            minigame_service=minigame_service,
            minigame_session_service=minigame_session_service,
            get_concepts=get_concepts,
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
    globals()["adaptive_selection_service"] = (
        app_modules.adaptive_selection_service
    )
    globals()["minigame_service"] = app_modules.minigame_service
    globals()["minigame_session_service"] = (
        app_modules.minigame_session_service
    )
    globals()["get_concepts"] = app_modules.get_concepts


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
def adaptive_app(db):
    app = FastAPI()
    app.include_router(minigames_route.router)

    def override_db():
        yield db

    app.dependency_overrides[minigames_route.get_db] = override_db
    return app


def create_user(db, email="adaptive@example.com"):
    user = models.User(
        name="Adaptive User",
        email=email,
        password="not-used",
    )
    db.add(user)
    db.commit()
    db.refresh(user)
    return user


def add_mastery(db, user_id, concept_id, score, topic="passwords", attempts=1):
    record = models.UserConceptMastery(
        user_id=user_id,
        concept_id=concept_id,
        topic=topic,
        mastery_score=score,
        alpha=2.0,
        beta=2.0,
        attempt_count=attempts,
        correct_count=0,
        incorrect_count=attempts,
        evidence_weight=float(attempts),
    )
    db.add(record)
    db.commit()
    db.refresh(record)
    return record


def add_completed_session(
    db,
    user_id,
    item_ids,
    session_index=0,
    topic="passwords",
    risk="bajo",
    minigame="quiz",
):
    now = datetime.utcnow()
    record = models.MinigameSessionRecord(
        id=str(uuid.uuid4()),
        user_id=user_id,
        topic=topic,
        risk=risk,
        minigame=minigame,
        item_ids=list(item_ids),
        concept_ids=["passwords.salt"],
        status="completed",
        started_at=now - timedelta(minutes=session_index + 1),
        completed_at=now - timedelta(minutes=session_index),
    )
    db.add(record)
    db.commit()
    db.refresh(record)
    return record


def quiz_item(item_id, concept_ids, difficulty="bajo", topic="passwords"):
    return {
        "item_id": item_id,
        "topic": topic,
        "risk": difficulty,
        "minigame": "quiz",
        "concept_ids": list(concept_ids),
        "difficulty": difficulty,
        "question": f"Question {item_id}",
        "options": ["A", "B", "C", "D"],
        "answer": 0,
    }


def candidate_bank():
    return [
        quiz_item("item_a", ["passwords.salt"]),
        quiz_item("item_b", ["passwords.hash"]),
        quiz_item("item_c", ["passwords.argon2id"]),
        quiz_item("item_d", ["passwords.password_spraying"]),
    ]


def select(db, user_id, candidates=None, session_id="session-a", limit=3):
    return adaptive_selection_service.select_adaptive_items(
        db=db,
        user_id=user_id,
        topic="passwords",
        risk="bajo",
        minigame="quiz",
        candidates=candidates or candidate_bank(),
        session_id=session_id,
        limit=limit,
    )


def item_ids(items):
    return [item["item_id"] for item in items]


def concept_ids_from_items(items):
    concept_ids = []

    for item in items:
        for concept_id in minigame_service.get_item_concept_ids(item):
            if concept_id not in concept_ids:
                concept_ids.append(concept_id)

    return concept_ids


def test_new_user_selection_works_without_mastery(db):
    user = create_user(db)

    selected = select(db, user.id)

    assert len(selected) == 3
    assert db.query(models.UserConceptMastery).count() == 0


def test_weak_concept_is_prioritized(db):
    user = create_user(db)
    add_mastery(db, user.id, "passwords.salt", 35)
    add_mastery(db, user.id, "passwords.hash", 85)

    selected = select(db, user.id)

    assert selected[0]["item_id"] == "item_a"


def test_mastered_concept_is_deprioritized(db):
    user = create_user(db)
    add_mastery(db, user.id, "passwords.salt", 85)
    add_mastery(db, user.id, "passwords.hash", 35)
    candidates = [
        quiz_item("item_a", ["passwords.salt"]),
        quiz_item("item_b", ["passwords.hash"]),
    ]

    selected = select(db, user.id, candidates=candidates, limit=2)

    assert item_ids(selected).index("item_b") < item_ids(selected).index("item_a")


def test_unpracticed_concept_gets_exploration_bonus(db):
    user = create_user(db)
    add_mastery(db, user.id, "passwords.salt", 55)
    candidates = [
        quiz_item("practiced", ["passwords.salt"]),
        quiz_item("unpracticed", ["passwords.hash"]),
    ]

    selected = select(db, user.id, candidates=candidates, limit=2)

    assert selected[0]["item_id"] == "unpracticed"


def test_recent_item_is_penalized(db):
    user = create_user(db)
    add_completed_session(db, user.id, ["item_a"])

    selected = select(
        db,
        user.id,
        candidates=[
            quiz_item("item_a", ["passwords.salt"]),
            quiz_item("item_b", ["passwords.hash"]),
        ],
        limit=2,
    )

    assert selected[0]["item_id"] == "item_b"


def test_recent_item_can_be_used_when_pool_is_small(db):
    user = create_user(db)
    add_completed_session(db, user.id, ["item_a"])

    selected = select(
        db,
        user.id,
        candidates=[quiz_item("item_a", ["passwords.salt"])],
        limit=3,
    )

    assert item_ids(selected) == ["item_a"]


def test_only_exact_topic_risk_minigame_candidates_are_used(db):
    user = create_user(db)
    candidates = [
        quiz_item("exact", ["passwords.salt"]),
        quiz_item("wrong_topic", ["phishing.phishing"], topic="phishing"),
        quiz_item("wrong_risk", ["passwords.hash"], difficulty="alto"),
        {
            **quiz_item("wrong_game", ["passwords.argon2id"]),
            "minigame": "crossword",
        },
    ]

    selected = select(db, user.id, candidates=candidates, limit=4)

    assert item_ids(selected) == ["exact"]


def test_selection_respects_limit(db):
    user = create_user(db)

    selected = select(db, user.id, limit=2)

    assert len(selected) == 2


def test_small_pool_returns_all_items(db):
    user = create_user(db)
    candidates = [
        quiz_item("item_a", ["passwords.salt"]),
        quiz_item("item_b", ["passwords.hash"]),
    ]

    selected = select(db, user.id, candidates=candidates, limit=3)

    assert set(item_ids(selected)) == {"item_a", "item_b"}


def test_selection_does_not_mutate_candidates(db):
    user = create_user(db)
    candidates = candidate_bank()
    before = copy.deepcopy(candidates)

    select(db, user.id, candidates=candidates)

    assert candidates == before


def test_same_session_id_produces_same_order(db):
    user = create_user(db)

    first = select(db, user.id, session_id="same-session")
    second = select(db, user.id, session_id="same-session")

    assert item_ids(first) == item_ids(second)


def test_different_users_use_independent_mastery(db):
    user_one = create_user(db, "one@example.com")
    user_two = create_user(db, "two@example.com")
    add_mastery(db, user_one.id, "passwords.salt", 35)
    add_mastery(db, user_two.id, "passwords.hash", 35)

    first = select(db, user_one.id)
    second = select(db, user_two.id)

    assert first[0]["item_id"] == "item_a"
    assert second[0]["item_id"] == "item_b"


def test_multiconcept_item_uses_weakest_concept(db):
    user = create_user(db)
    add_mastery(db, user.id, "passwords.salt", 90)
    add_mastery(db, user.id, "passwords.hash", 35)
    candidates = [
        quiz_item("multi", ["passwords.salt", "passwords.hash"]),
        quiz_item("mastered", ["passwords.salt"]),
    ]

    selected = select(db, user.id, candidates=candidates, limit=2)

    assert selected[0]["item_id"] == "multi"


def test_validation_case_prioritizes_weak_then_exploration_then_mastered(db):
    user = create_user(db)
    add_mastery(db, user.id, "passwords.salt", 35)
    add_mastery(db, user.id, "passwords.hash", 85)
    add_completed_session(db, user.id, ["item_c"])
    candidates = [
        quiz_item("item_a", ["passwords.salt"]),
        quiz_item("item_b", ["passwords.hash"]),
        quiz_item("item_c", ["passwords.argon2id"]),
    ]

    selected = select(db, user.id, candidates=candidates, limit=3)

    assert selected[0]["item_id"] == "item_a"
    assert "item_b" in item_ids(selected)
    assert "item_c" in item_ids(selected)


def test_selected_items_are_persisted(db, monkeypatch):
    user = create_user(db)
    monkeypatch.setattr(
        minigame_session_service,
        "_get_bank_items",
        lambda topic, risk, minigame: candidate_bank(),
    )

    session = minigame_session_service.create_minigame_session(
        topic="passwords",
        risk="bajo",
        minigame="quiz",
        db=db,
        user_id=user.id,
    )
    record = db.query(models.MinigameSessionRecord).filter(
        models.MinigameSessionRecord.id == session["session_id"]
    ).one()

    assert record.item_ids == item_ids(session["items"])


def test_lesson_contains_only_selected_concepts(db, monkeypatch):
    user = create_user(db)
    monkeypatch.setattr(
        minigame_session_service,
        "_get_bank_items",
        lambda topic, risk, minigame: candidate_bank(),
    )

    session = minigame_session_service.create_minigame_session(
        topic="passwords",
        risk="bajo",
        minigame="quiz",
        db=db,
        user_id=user.id,
    )

    lesson_terms = [
        concept["term"]
        for concept in session["lesson"]["key_concepts"]
    ]
    selected_terms = [
        concept["term"]
        for concept in get_concepts(concept_ids_from_items(session["items"]))
    ]
    assert lesson_terms == selected_terms


def test_response_items_match_database_item_ids(db, monkeypatch):
    user = create_user(db)
    monkeypatch.setattr(
        minigame_session_service,
        "_get_bank_items",
        lambda topic, risk, minigame: candidate_bank(),
    )

    response = minigame_session_service.create_minigame_session(
        topic="passwords",
        risk="bajo",
        minigame="quiz",
        db=db,
        user_id=user.id,
    )
    record = db.query(models.MinigameSessionRecord).filter(
        models.MinigameSessionRecord.id == response["session_id"]
    ).one()

    assert item_ids(response["items"]) == record.item_ids


def test_legacy_endpoints_are_unchanged(db, adaptive_app):
    user = create_user(db)
    adaptive_app.dependency_overrides[minigames_route.get_current_user] = (
        lambda: SimpleNamespace(id=user.id)
    )
    client = TestClient(adaptive_app)

    quiz_response = client.get("/minigames/quiz?topic=passwords&risk=bajo")
    wordsearch_response = client.get(
        "/minigames/wordsearch?topic=passwords&risk=bajo"
    )
    crossword_response = client.get(
        "/minigames/crossword?topic=passwords&risk=bajo"
    )

    assert quiz_response.status_code == 200
    assert wordsearch_response.status_code == 200
    assert crossword_response.status_code == 200
    assert quiz_response.json() == minigame_service.get_quiz("passwords", "bajo")
    assert wordsearch_response.json() == minigame_service.get_wordsearch(
        "passwords",
        "bajo",
    )
    assert crossword_response.json() == minigame_service.get_crossword(
        "passwords",
        "bajo",
    )
