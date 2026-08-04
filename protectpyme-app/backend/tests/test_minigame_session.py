import os
import sys
import types

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from sqlalchemy import create_engine, inspect
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
    from app import models
    from app.database import engine, SessionLocal

    models.Base.metadata.drop_all(bind=engine)
    models.Base.metadata.create_all(bind=engine)

    db = SessionLocal()
    current_user = models.User(
        name="Minigame User",
        email="minigame@example.com",
        password="not-used",
    )
    db.add(current_user)
    db.commit()
    db.refresh(current_user)
    db.close()

    app = FastAPI()
    app.include_router(router)
    app.dependency_overrides[get_current_user] = lambda: current_user
    client = TestClient(app)

    try:
        yield client, router
    finally:
        models.Base.metadata.drop_all(bind=engine)

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
        assert "answer" not in item
        assert isinstance(item["answer_text"], str)
        assert isinstance(item["correct_option"], int)


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


def test_quiz_session_uses_numeric_correct_option(minigame_client):
    client, _ = minigame_client
    response = client.post(
        "/minigames/session",
        json={
            "topic": "passwords",
            "risk": "bajo",
            "minigame": "quiz",
        },
    )
    session = response.json()
    bank = minigame_service.get_quiz("passwords", "bajo")

    assert response.status_code == 200

    for index, item in enumerate(session["items"]):
        assert "answer" not in item
        assert item["answer_text"] == ""
        assert item["correct_option"] == bank[index]["answer"]
        assert isinstance(item["correct_option"], int)


def test_wordsearch_session_uses_text_answer(minigame_client):
    client, _ = minigame_client
    response = client.post(
        "/minigames/session",
        json={
            "topic": "passwords",
            "risk": "bajo",
            "minigame": "wordsearch",
        },
    )
    session = response.json()
    bank = minigame_service.get_wordsearch("passwords", "bajo")

    assert response.status_code == 200

    for index, item in enumerate(session["items"]):
        assert "answer" not in item
        assert item["answer_text"] == bank[index]["answer"]
        assert item["correct_option"] == -1


def test_crossword_session_uses_text_answer(minigame_client):
    client, _ = minigame_client
    response = client.post(
        "/minigames/session",
        json={
            "topic": "passwords",
            "risk": "bajo",
            "minigame": "crossword",
        },
    )
    session = response.json()
    bank = minigame_service.get_crossword("passwords", "bajo")

    assert response.status_code == 200

    for index, item in enumerate(session["items"]):
        assert "answer" not in item
        assert item["answer_text"] == bank[index]["answer"]
        assert item["correct_option"] == -1


def test_all_36_sessions_have_stable_answer_types():
    for topic in TOPICS:
        for risk in RISKS:
            for minigame in MINIGAMES:
                session = minigame_session_service.create_minigame_session(
                    topic=topic,
                    risk=risk,
                    minigame=minigame,
                )

                for item in session["items"]:
                    assert "answer" not in item
                    assert isinstance(item["answer_text"], str)
                    assert isinstance(item["correct_option"], int)

                    if minigame == "quiz":
                        assert item["answer_text"] == ""
                        assert item["correct_option"] >= 0
                    else:
                        assert item["answer_text"].strip()
                        assert item["correct_option"] == -1


def test_old_routes_keep_legacy_answer_contract(minigame_client):
    client, _ = minigame_client
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
    assert "answer_text" not in quiz[0]
    assert "correct_option" not in wordsearch[0]


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


def get_db_session():
    from app.database import SessionLocal

    return SessionLocal()


def get_current_test_user():
    from app import models

    db = get_db_session()

    try:
        return db.query(models.User).filter(
            models.User.email == "minigame@example.com"
        ).first()
    finally:
        db.close()


def create_other_user(email="other-minigame@example.com"):
    from app import models

    db = get_db_session()
    user = models.User(
        name="Other User",
        email=email,
        password="not-used",
    )
    db.add(user)
    db.commit()
    db.refresh(user)
    db.close()

    return user


def create_persisted_session(
    client,
    topic="passwords",
    risk="bajo",
    minigame="crossword",
):
    response = client.post(
        "/minigames/session",
        json={
            "topic": topic,
            "risk": risk,
            "minigame": minigame,
        },
    )

    assert response.status_code == 200
    return response.json()


def record_attempt(
    client,
    session_id,
    item_id,
    *,
    correct=True,
    response_time_ms=500,
    attempt_number=1,
    points_delta=10,
    extra_payload=None,
):
    payload = {
        "session_id": session_id,
        "item_id": item_id,
        "correct": correct,
        "response_time_ms": response_time_ms,
        "attempt_number": attempt_number,
        "points_delta": points_delta,
    }

    if extra_payload:
        payload.update(extra_payload)

    return client.post("/minigames/attempts", json=payload)


def get_session_record(session_id):
    from app import models

    db = get_db_session()

    try:
        return db.query(models.MinigameSessionRecord).filter(
            models.MinigameSessionRecord.id == session_id
        ).first()
    finally:
        db.close()


def test_minigame_session_table_is_created(minigame_client):
    from app import models
    from app.database import engine

    assert "minigame_session_records" in models.Base.metadata.tables
    assert inspect(engine).has_table("minigame_session_records")


def test_minigame_attempt_table_is_created(minigame_client):
    from app import models
    from app.database import engine

    assert "minigame_attempts" in models.Base.metadata.tables
    assert inspect(engine).has_table("minigame_attempts")


def test_session_record_uses_generated_session_id(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    record = get_session_record(session["session_id"])

    assert record is not None
    assert record.id == session["session_id"]


def test_session_record_stores_selected_items(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    record = get_session_record(session["session_id"])

    assert record.item_ids == [
        item["item_id"]
        for item in session["items"]
    ]


def test_session_record_stores_unique_concepts(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    record = get_session_record(session["session_id"])

    all_concepts = [
        concept_id
        for item in session["items"]
        for concept_id in item["concept_ids"]
    ]

    assert record.concept_ids == list(dict.fromkeys(all_concepts))


def test_create_session_persists_authenticated_user(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    record = get_session_record(session["session_id"])
    user = get_current_test_user()

    assert record.user_id == user.id


def test_create_session_persists_topic_risk_minigame(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(
        client,
        topic="wifi",
        risk="medio",
        minigame="quiz",
    )
    record = get_session_record(session["session_id"])

    assert record.topic == "wifi"
    assert record.risk == "medio"
    assert record.minigame == "quiz"


def test_create_session_starts_with_started_status(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    record = get_session_record(session["session_id"])

    assert record.status == "started"
    assert record.completed_at is None


def test_session_response_contract_is_unchanged(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)

    assert_session_contract(session)


def test_session_does_not_store_full_lesson(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    record = get_session_record(session["session_id"])

    assert not hasattr(record, "lesson")
    assert "lesson" not in record.__table__.columns


def test_record_correct_attempt(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    response = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
        correct=True,
    )

    assert response.status_code == 200
    assert response.json()["correct"] is True


def test_record_incorrect_attempt(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    response = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
        correct=False,
    )

    assert response.status_code == 200
    assert response.json()["correct"] is False


def test_attempt_metadata_is_derived_from_backend(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    response = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
        extra_payload={
            "concept_ids": ["client.fake"],
            "difficulty": "alto",
        },
    )

    assert response.status_code == 422

    response = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
    )

    assert response.status_code == 200
    assert response.json()["concept_ids"] == ["passwords.argon2id"]
    assert response.json()["difficulty"] == "bajo"


def test_attempt_rejects_unknown_session(minigame_client):
    client, _ = minigame_client
    response = record_attempt(
        client,
        "00000000-0000-0000-0000-000000000000",
        "passwords_bajo_crossword_argon2id",
    )

    assert response.status_code == 404


def test_attempt_rejects_session_from_another_user(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    other_user = create_other_user()

    from app.routes.auth import get_current_user

    client.app.dependency_overrides[get_current_user] = lambda: other_user
    response = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
    )

    assert response.status_code == 404


def test_attempt_rejects_item_not_in_session(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    response = record_attempt(
        client,
        session["session_id"],
        "phishing_alto_crossword_phishing",
    )

    assert response.status_code == 400


def test_attempt_rejects_unknown_item(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    response = record_attempt(
        client,
        session["session_id"],
        "not_a_real_item",
    )

    assert response.status_code == 404


def test_attempt_rejects_duplicate_attempt_number(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    first = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
    )
    second = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
    )

    assert first.status_code == 200
    assert second.status_code == 409


def test_attempt_rejects_completed_session(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    complete = client.post(f"/minigames/session/{session['session_id']}/complete")
    response = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
    )

    assert complete.status_code == 200
    assert response.status_code == 409


def test_attempt_validates_response_time(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    response = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
        response_time_ms=3_600_001,
    )

    assert response.status_code == 422


def test_attempt_does_not_accept_client_concept_ids(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    response = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
        extra_payload={"concept_ids": ["passwords.fake"]},
    )

    assert response.status_code == 422


def test_passwords_bajo_crossword_argon2id_attempt_stores_backend_metadata(
    minigame_client,
):
    from app import models

    client, _ = minigame_client
    session = create_persisted_session(client)
    response = record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_argon2id",
    )

    db = get_db_session()

    try:
        attempt = db.query(models.MinigameAttempt).filter(
            models.MinigameAttempt.item_id == "passwords_bajo_crossword_argon2id"
        ).first()
    finally:
        db.close()

    assert response.status_code == 200
    assert attempt.item_id == "passwords_bajo_crossword_argon2id"
    assert attempt.concept_ids == ["passwords.argon2id"]
    assert attempt.difficulty == "bajo"


def test_complete_session_returns_summary(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_salt",
        correct=True,
        response_time_ms=500,
        points_delta=10,
    )
    record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_hash",
        correct=False,
        response_time_ms=700,
        points_delta=-2,
    )
    response = client.post(f"/minigames/session/{session['session_id']}/complete")
    summary = response.json()

    assert response.status_code == 200
    assert summary["session_id"] == session["session_id"]
    assert summary["total_items"] == len(session["items"])
    assert summary["attempted_items"] == 2
    assert summary["total_attempts"] == 2
    assert summary["correct_attempts"] == 1
    assert summary["incorrect_attempts"] == 1
    assert summary["points_earned"] == 8
    assert summary["accuracy"] == 50.0
    assert summary["total_response_time_ms"] == 1200


def test_complete_session_marks_completed(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    response = client.post(f"/minigames/session/{session['session_id']}/complete")
    record = get_session_record(session["session_id"])

    assert response.status_code == 200
    assert record.status == "completed"
    assert record.completed_at is not None


def test_complete_session_accuracy(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_salt",
        correct=True,
    )
    record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_hash",
        correct=False,
    )
    response = client.post(f"/minigames/session/{session['session_id']}/complete")

    assert response.status_code == 200
    assert response.json()["accuracy"] == 50.0


def test_complete_session_points_sum(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_salt",
        points_delta=10,
    )
    record_attempt(
        client,
        session["session_id"],
        "passwords_bajo_crossword_hash",
        attempt_number=1,
        points_delta=5,
    )
    response = client.post(f"/minigames/session/{session['session_id']}/complete")

    assert response.status_code == 200
    assert response.json()["points_earned"] == 15


def test_complete_empty_session(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    response = client.post(f"/minigames/session/{session['session_id']}/complete")
    summary = response.json()

    assert response.status_code == 200
    assert summary["total_attempts"] == 0
    assert summary["attempted_items"] == 0
    assert summary["accuracy"] == 0


def test_complete_session_from_another_user_is_rejected(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    other_user = create_other_user()

    from app.routes.auth import get_current_user

    client.app.dependency_overrides[get_current_user] = lambda: other_user
    response = client.post(f"/minigames/session/{session['session_id']}/complete")

    assert response.status_code == 404


def test_completed_session_cannot_be_completed_twice(minigame_client):
    client, _ = minigame_client
    session = create_persisted_session(client)
    first = client.post(f"/minigames/session/{session['session_id']}/complete")
    second = client.post(f"/minigames/session/{session['session_id']}/complete")

    assert first.status_code == 200
    assert second.status_code == 409
