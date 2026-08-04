import os
import sys
import types

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base, sessionmaker
from sqlalchemy.pool import StaticPool


os.environ.setdefault("SECRET_KEY", "test-secret")

from app import schemas  # noqa: E402
from app.services import minigame_service, minigame_session_service  # noqa: E402
from app.services.concept_catalog import get_concepts  # noqa: E402


TOPICS = ("phishing", "passwords", "malware", "wifi")
RISKS = ("alto", "medio", "bajo")
MINIGAMES = ("quiz", "wordsearch", "crossword")


@pytest.fixture
def minigame_client():
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

    from app.routes.auth import get_current_user
    from app.routes.minigames import router

    app = FastAPI()
    app.include_router(router)
    app.dependency_overrides[get_current_user] = lambda: object()
    client = TestClient(app)

    try:
        yield client, router
    finally:
        for name, module in previous_modules.items():
            if module is None:
                sys.modules.pop(name, None)
            else:
                sys.modules[name] = module


def get_bank(topic, risk, minigame):
    if minigame == "quiz":
        return minigame_service.get_quiz(topic, risk)
    if minigame == "wordsearch":
        return minigame_service.get_wordsearch(topic, risk)
    return minigame_service.get_crossword(topic, risk)


def item_concept_ids(item):
    if "concept_ids" in item:
        return list(item["concept_ids"])
    return [item["concept_id"]]


def lesson_terms_for_concept_ids(concept_ids):
    return [
        concept["term"]
        for concept in get_concepts(concept_ids)
    ]


def assert_session_contract(session):
    assert set(session) == {
        "session_id",
        "topic",
        "risk",
        "minigame",
        "lesson",
        "items",
    }
    assert session["session_id"]
    assert session["items"]
    assert session["lesson"]["topic"] == session["topic"]
    assert session["lesson"]["risk"] == session["risk"]
    assert session["lesson"]["minigame"] == session["minigame"]

    for item in session["items"]:
        assert item["item_id"]
        assert item["concept_ids"]
        assert item["difficulty"] == session["risk"]
        assert "answer" in item


def test_session_response_contract(minigame_client):
    client, router = minigame_client
    response = client.post(
        "/minigames/session",
        json={
            "topic": "passwords",
            "risk": "bajo",
            "minigame": "crossword",
        },
    )
    route = next(
        route
        for route in router.routes
        if getattr(route, "path", None) == "/minigames/session"
    )

    assert response.status_code == 200
    assert route.response_model is schemas.MinigameSessionResponse
    assert_session_contract(response.json())


@pytest.mark.parametrize("topic", TOPICS)
@pytest.mark.parametrize("risk", RISKS)
@pytest.mark.parametrize("minigame", MINIGAMES)
def test_all_36_session_combinations_are_valid(topic, risk, minigame):
    session = minigame_session_service.create_minigame_session(
        topic=topic,
        risk=risk,
        minigame=minigame,
    )

    assert_session_contract(session)


@pytest.mark.parametrize("minigame", MINIGAMES)
def test_session_uses_items_from_requested_bank(minigame):
    session = minigame_session_service.create_minigame_session(
        topic="phishing",
        risk="alto",
        minigame=minigame,
    )
    bank = get_bank("phishing", "alto", minigame)

    assert [
        item["item_id"]
        for item in session["items"]
    ] == [
        item["item_id"]
        for item in bank
    ]


def test_session_lesson_uses_selected_item_concepts():
    session = minigame_session_service.create_minigame_session(
        topic="passwords",
        risk="bajo",
        minigame="crossword",
    )
    selected_concept_ids = []

    for item in session["items"]:
        for concept_id in item["concept_ids"]:
            if concept_id not in selected_concept_ids:
                selected_concept_ids.append(concept_id)

    assert [
        concept["term"]
        for concept in session["lesson"]["key_concepts"]
    ] == lesson_terms_for_concept_ids(selected_concept_ids)


def test_session_does_not_include_unselected_concepts():
    session = minigame_session_service.create_minigame_session(
        topic="passwords",
        risk="bajo",
        minigame="crossword",
    )
    terms = {
        concept["term"].lower()
        for concept in session["lesson"]["key_concepts"]
    }

    assert "password spraying" not in terms


def test_passwords_bajo_crossword_session_teaches_salt_hash_argon2id():
    session = minigame_session_service.create_minigame_session(
        topic="passwords",
        risk="bajo",
        minigame="crossword",
    )

    assert [
        concept_id
        for item in session["items"]
        for concept_id in item["concept_ids"]
    ] == [
        "passwords.salt",
        "passwords.hash",
        "passwords.argon2id",
    ]

    text = " ".join(
        " ".join(concept.values())
        for concept in session["lesson"]["key_concepts"]
    ).lower()

    assert "salt" in text
    assert "hash" in text
    assert "argon2id" in text
    assert "cifrado reversible" in text


def test_passwords_bajo_wordsearch_session_teaches_salt_hash_argon2id():
    session = minigame_session_service.create_minigame_session(
        topic="passwords",
        risk="bajo",
        minigame="wordsearch",
    )

    assert [
        concept_id
        for item in session["items"]
        for concept_id in item["concept_ids"]
    ] == [
        "passwords.salt",
        "passwords.hash",
        "passwords.argon2id",
    ]


def test_quiz_session_supports_multiple_concept_ids():
    session = minigame_session_service.create_minigame_session(
        topic="passwords",
        risk="bajo",
        minigame="quiz",
    )

    assert any(
        item["concept_ids"] == ["passwords.salt", "passwords.hash"]
        for item in session["items"]
    )


def test_old_minigame_routes_still_work(minigame_client):
    client, _ = minigame_client

    for path in ("/minigames/quiz", "/minigames/wordsearch", "/minigames/crossword"):
        response = client.get(
            path,
            params={
                "topic": "phishing",
                "risk": "alto",
            },
        )

        assert response.status_code == 200
        assert isinstance(response.json(), list)


def test_old_lesson_route_still_works(minigame_client):
    client, _ = minigame_client
    response = client.get(
        "/minigames/lesson",
        params={
            "topic": "passwords",
            "risk": "bajo",
            "minigame": "crossword",
        },
    )

    assert response.status_code == 200
    assert response.json()["minigame"] == "crossword"
