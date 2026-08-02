import copy
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
from app.services import learning_content_service, minigame_service  # noqa: E402


REQUIRED_FIELDS = {
    "topic",
    "risk",
    "minigame",
    "title",
    "vulnerability",
    "learning_objective",
    "explanation",
    "tips",
    "recommended_action",
    "key_concepts",
    "practical_example",
    "common_mistake",
    "quick_check",
    "visual_key",
}


@pytest.fixture
def lesson_clients():
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

    unauthenticated_app = FastAPI()
    unauthenticated_app.include_router(router)
    unauthenticated_client = TestClient(unauthenticated_app)

    app = FastAPI()
    app.include_router(router)
    app.dependency_overrides[get_current_user] = lambda: object()
    client = TestClient(app)

    try:
        yield client, unauthenticated_client, router
    finally:
        for name, module in previous_modules.items():
            if module is None:
                sys.modules.pop(name, None)
            else:
                sys.modules[name] = module


def get_lesson(client, topic, risk, minigame="quiz"):
    return client.get(
        "/minigames/lesson",
        params={
            "topic": topic,
            "risk": risk,
            "minigame": minigame,
        },
    )


def assert_lesson_contract(data):
    assert REQUIRED_FIELDS == set(data)

    text_fields = REQUIRED_FIELDS - {
        "tips",
        "key_concepts",
        "practical_example",
        "common_mistake",
        "quick_check",
    }

    for field in text_fields:
        assert isinstance(data[field], str)
        assert data[field].strip()

    assert isinstance(data["tips"], list)
    assert len(data["tips"]) == 3

    for tip in data["tips"]:
        assert isinstance(tip, str)
        assert tip.strip()
        assert tip.endswith(".")

    assert 2 <= len(data["key_concepts"]) <= 4

    for concept in data["key_concepts"]:
        assert set(concept) == {
            "term",
            "definition",
            "why_it_matters",
            "example",
        }
        assert all(str(value).strip() for value in concept.values())

    assert set(data["practical_example"]) == {"title", "steps"}
    assert data["practical_example"]["title"].strip()
    assert 3 <= len(data["practical_example"]["steps"]) <= 5
    assert all(step.strip() for step in data["practical_example"]["steps"])

    assert set(data["common_mistake"]) == {"title", "explanation"}
    assert data["common_mistake"]["title"].strip()
    assert data["common_mistake"]["explanation"].strip()

    assert set(data["quick_check"]) == {
        "question",
        "options",
        "correct_option",
        "explanation",
    }
    assert data["quick_check"]["question"].strip()
    assert len(data["quick_check"]["options"]) == 3
    assert 0 <= data["quick_check"]["correct_option"] <= 2
    assert data["quick_check"]["explanation"].strip()

    assert "://" not in data["visual_key"]
    assert "/" not in data["visual_key"]


def test_get_without_authentication_keeps_httpbearer_behavior(lesson_clients):
    _, unauthenticated_client, _ = lesson_clients
    response = unauthenticated_client.get(
        "/minigames/lesson",
        params={
            "topic": "phishing",
            "risk": "alto",
            "minigame": "quiz",
        },
    )

    assert response.status_code in (401, 403)


def test_phishing_alto_returns_200(lesson_clients):
    client, _, _ = lesson_clients
    response = get_lesson(client, "phishing", "alto")

    assert response.status_code == 200
    assert response.json()["topic"] == "phishing"
    assert response.json()["risk"] == "alto"


def test_passwords_medio_returns_200(lesson_clients):
    client, _, _ = lesson_clients
    response = get_lesson(client, "passwords", "medio")

    assert response.status_code == 200
    assert response.json()["topic"] == "passwords"
    assert response.json()["risk"] == "medio"


def test_malware_bajo_returns_200(lesson_clients):
    client, _, _ = lesson_clients
    response = get_lesson(client, "malware", "bajo")

    assert response.status_code == 200
    assert response.json()["topic"] == "malware"
    assert response.json()["risk"] == "bajo"


def test_wifi_alto_returns_200(lesson_clients):
    client, _, _ = lesson_clients
    response = get_lesson(client, "wifi", "alto")

    assert response.status_code == 200
    assert response.json()["topic"] == "wifi"
    assert response.json()["risk"] == "alto"


def test_minigame_is_required(lesson_clients):
    client, _, _ = lesson_clients
    response = client.get(
        "/minigames/lesson",
        params={
            "topic": "phishing",
            "risk": "alto",
        },
    )

    assert response.status_code == 422


@pytest.mark.parametrize(
    ("alias", "canonical"),
    [
        ("quiz", "quiz"),
        ("kahoot", "quiz"),
        ("wordsearch", "wordsearch"),
        ("sopa", "wordsearch"),
        ("sopa_letras", "wordsearch"),
        ("sopaletras", "wordsearch"),
        ("crossword", "crossword"),
        ("crucigrama", "crossword"),
    ],
)
def test_minigame_aliases_are_normalized(lesson_clients, alias, canonical):
    client, _, _ = lesson_clients
    response = get_lesson(client, "phishing", "alto", alias)

    assert response.status_code == 200
    assert response.json()["minigame"] == canonical


def test_invalid_minigame_returns_controlled_error(lesson_clients):
    client, _, _ = lesson_clients
    response = get_lesson(client, "phishing", "alto", "memory")

    assert response.status_code == 400
    assert "Minigame" in response.json()["detail"]


def test_all_thirty_six_combinations_exist():
    for topic in ("phishing", "passwords", "malware", "wifi"):
        for risk in ("alto", "medio", "bajo"):
            for minigame in ("quiz", "wordsearch", "crossword"):
                lesson = learning_content_service.get_learning_content(
                    topic,
                    risk,
                    minigame,
                )

                assert lesson["topic"] == topic
                assert lesson["risk"] == risk
                assert lesson["minigame"] == minigame
                assert_lesson_contract(lesson)


def test_each_response_contains_all_fields(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "phishing", "alto").json()

    assert REQUIRED_FIELDS == set(data)


def test_each_response_contains_exactly_three_tips(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "passwords", "medio").json()

    assert len(data["tips"]) == 3


def test_no_required_field_is_empty(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "malware", "bajo").json()

    assert_lesson_contract(data)


def test_returned_topic_and_risk_are_normalized(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "password", "HIGH").json()

    assert data["topic"] == "passwords"
    assert data["risk"] == "alto"


def test_topic_alias_keeps_current_normalization(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "network", "medio").json()

    assert data["topic"] == "wifi"
    assert data["risk"] == "medio"


def test_uppercase_risk_is_normalized(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "malware", "BAJO").json()

    assert data["risk"] == "bajo"


def test_response_model_is_configured(lesson_clients):
    _, _, router = lesson_clients
    lesson_route = next(
        route
        for route in router.routes
        if getattr(route, "path", None) == "/minigames/lesson"
    )

    assert lesson_route.response_model is schemas.MinigameLessonResponse


def test_passwords_bajo_crossword_teaches_hash_salt_and_argon2id(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "passwords", "bajo", "crossword").json()
    terms = {
        concept["term"].lower()
        for concept in data["key_concepts"]
    }
    concept_text = " ".join(
        " ".join(concept.values())
        for concept in data["key_concepts"]
    ).lower()

    assert "hash" in terms
    assert "salt" in terms
    assert "argon2id" in terms
    assert "cifrado" in concept_text


def test_phishing_bajo_teaches_spf_dkim_and_dmarc(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "phishing", "bajo", "quiz").json()
    terms = {
        concept["term"].lower()
        for concept in data["key_concepts"]
    }

    assert {"spf", "dkim", "dmarc"}.issubset(terms)


def test_malware_medio_teaches_ransomware_and_spyware(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "malware", "medio", "wordsearch").json()
    terms = {
        concept["term"].lower()
        for concept in data["key_concepts"]
    }

    assert "ransomware" in terms
    assert "spyware" in terms


def test_wifi_bajo_teaches_evil_twin_and_wpa3(lesson_clients):
    client, _, _ = lesson_clients
    data = get_lesson(client, "wifi", "bajo", "crossword").json()
    terms = {
        concept["term"].lower()
        for concept in data["key_concepts"]
    }

    assert "evil twin" in terms
    assert "wpa3" in terms


def test_lesson_endpoint_does_not_alter_minigame_banks(lesson_clients):
    client, _, _ = lesson_clients
    quiz_before = copy.deepcopy(minigame_service.QUIZ)
    crossword_before = copy.deepcopy(minigame_service.CROSSWORD)
    wordsearch_before = copy.deepcopy(minigame_service.WORDSEARCH)

    response = get_lesson(client, "phishing", "alto")

    assert response.status_code == 200
    assert minigame_service.QUIZ == quiz_before
    assert minigame_service.CROSSWORD == crossword_before
    assert minigame_service.WORDSEARCH == wordsearch_before


def test_existing_minigame_routes_still_work(lesson_clients):
    client, _, _ = lesson_clients
    quiz = client.get(
        "/minigames/quiz",
        params={
            "topic": "phishing",
            "risk": "alto",
        },
    )
    wordsearch = client.get(
        "/minigames/wordsearch",
        params={
            "topic": "phishing",
            "risk": "alto",
        },
    )
    crossword = client.get(
        "/minigames/crossword",
        params={
            "topic": "phishing",
            "risk": "alto",
        },
    )

    assert quiz.status_code == 200
    assert wordsearch.status_code == 200
    assert crossword.status_code == 200
    assert isinstance(quiz.json(), list)
    assert isinstance(wordsearch.json(), list)
    assert isinstance(crossword.json(), list)
