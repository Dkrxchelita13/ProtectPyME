import importlib
import os
import sys
import types
from collections import Counter
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
        "app.services.pilot_service",
        "app.services.pilot_assessment_service",
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
        ("app.services", ("pilot_service", "pilot_assessment_service")),
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
    pilot_service = importlib.import_module("app.services.pilot_service")
    pilot_assessment_service = importlib.import_module(
        "app.services.pilot_assessment_service"
    )

    try:
        yield SimpleNamespace(
            engine=database.engine,
            get_current_user=auth.get_current_user,
            get_db=app_database.get_db,
            models=models,
            pilot_route=pilot_route,
            pilot_service=pilot_service,
            pilot_assessment_service=pilot_assessment_service,
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
        name="Assessment User",
        email="assessment@example.com",
        password="not-used",
    )
    db_session.add(user)
    db_session.commit()
    db_session.refresh(user)
    return user


@pytest.fixture()
def other_user(app_modules, db_session):
    user = app_modules.models.User(
        name="Other Assessment User",
        email="other-assessment@example.com",
        password="not-used",
    )
    db_session.add(user)
    db_session.commit()
    db_session.refresh(user)
    return user


def make_pilot_client(app_modules, db_session, user):
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

    return TestClient(app)


def accept_consent(app_modules, db_session, user):
    return app_modules.pilot_service.accept_pilot_consent(
        db_session,
        user.id,
    )


def start_assessment(client, phase):
    response = client.post(
        "/pilot/assessment/start",
        json={"phase": phase},
    )

    assert response.status_code == 201
    return response.json()


def answer_assessment(client, assessment_id, questions, correct_topics):
    for question in questions:
        selected_option = select_option(question, correct_topics)
        response = client.post(
            f"/pilot/assessment/{assessment_id}/answer",
            json={
                "question_id": question["question_id"],
                "selected_option": selected_option,
                "response_time_ms": 1200,
            },
        )

        assert response.status_code == 200


def select_option(question, correct_topics):
    if question["topic"] in correct_topics:
        return question["correct_option"]

    return next(
        option
        for option in ("A", "B", "C", "D")
        if option != question["correct_option"]
    )


def complete_assessment(client, assessment_id):
    response = client.post(f"/pilot/assessment/{assessment_id}/complete")

    assert response.status_code == 200
    return response.json()


def assessment_records(app_modules, db_session):
    return db_session.query(app_modules.models.PilotAssessment).all()


def assessment_answers(app_modules, db_session):
    return db_session.query(app_modules.models.PilotAssessmentAnswer).all()


def test_question_bank_is_reproducible_and_balanced(app_modules):
    service = app_modules.pilot_assessment_service

    pre_questions = service.get_questions_for_form("A")
    post_questions = service.get_questions_for_form("B")
    all_questions = service.get_all_questions()

    assert service.INSTRUMENT_VERSION == "pilot_assessment_v1"
    assert len(pre_questions) == 12
    assert len(post_questions) == 12
    assert len(all_questions) == 24
    assert len({question["question_id"] for question in all_questions}) == 24
    assert Counter(question["topic"] for question in pre_questions) == {
        "phishing": 3,
        "passwords": 3,
        "malware": 3,
        "wifi": 3,
    }
    assert Counter(question["topic"] for question in post_questions) == {
        "phishing": 3,
        "passwords": 3,
        "malware": 3,
        "wifi": 3,
    }


def test_question_bank_uses_expected_constructs(app_modules):
    service = app_modules.pilot_assessment_service

    constructs = {
        question["construct"]
        for question in service.get_all_questions()
    }

    assert constructs == {
        "senales_phishing",
        "dominio_url",
        "reporte_accion_segura",
        "secreto_credenciales",
        "mfa_verificacion",
        "contrasena_unica_larga",
        "usb_archivo_desconocido",
        "ransomware_malware",
        "respuesta_segura",
        "conexion_segura",
        "red_falsa_ssid",
        "trafico_exfiltracion",
    }


def test_start_requires_active_pilot_consent(app_modules, db_session, user):
    client = make_pilot_client(app_modules, db_session, user)

    response = client.post(
        "/pilot/assessment/start",
        json={"phase": "PRE"},
    )

    assert response.status_code == 403
    assert assessment_records(app_modules, db_session) == []


def test_status_does_not_require_consent_to_read(app_modules, db_session, user):
    client = make_pilot_client(app_modules, db_session, user)

    response = client.get("/pilot/assessment/status")

    assert response.status_code == 200
    assert response.json() == {
        "instrument_version": "pilot_assessment_v1",
        "consent_active": False,
        "pre": None,
        "post": None,
        "next_phase": None,
    }


def test_start_pre_returns_public_questions_without_answer_key(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)

    data = start_assessment(client, "PRE")

    assert data["phase"] == "PRE"
    assert data["status"] == "started"
    assert len(data["questions"]) == 12
    assert data["questions"][0]["question_id"] == "pre_phishing_01"
    assert "correct_option" not in data["questions"][0]
    assert "is_correct" not in data["questions"][0]
    assert "topic" not in data["questions"][0]
    assert "construct" not in data["questions"][0]


def test_start_pre_is_idempotent_while_incomplete(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)

    first = start_assessment(client, "PRE")
    second = start_assessment(client, "PRE")

    assert first["assessment_id"] == second["assessment_id"]
    assert len(assessment_records(app_modules, db_session)) == 1


def test_post_cannot_start_before_completed_pre(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)

    response = client.post(
        "/pilot/assessment/start",
        json={"phase": "POST"},
    )

    assert response.status_code == 409


def test_answer_rejects_invalid_question_option_and_duplicate(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")
    assessment_id = assessment["assessment_id"]

    invalid_question = client.post(
        f"/pilot/assessment/{assessment_id}/answer",
        json={
            "question_id": "post_phishing_01",
            "selected_option": "A",
            "response_time_ms": 1000,
        },
    )
    invalid_option = client.post(
        f"/pilot/assessment/{assessment_id}/answer",
        json={
            "question_id": service.get_questions_for_form("A")[0]["question_id"],
            "selected_option": "Z",
            "response_time_ms": 1000,
        },
    )
    first_answer = client.post(
        f"/pilot/assessment/{assessment_id}/answer",
        json={
            "question_id": service.get_questions_for_form("A")[0]["question_id"],
            "selected_option": "B",
            "response_time_ms": 1000,
        },
    )
    duplicate_answer = client.post(
        f"/pilot/assessment/{assessment_id}/answer",
        json={
            "question_id": service.get_questions_for_form("A")[0]["question_id"],
            "selected_option": "B",
            "response_time_ms": 1000,
        },
    )

    assert invalid_question.status_code == 400
    assert invalid_option.status_code == 400
    assert first_answer.status_code == 200
    assert duplicate_answer.status_code == 409


def test_answer_payload_forbids_client_side_scoring_fields(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")

    response = client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/answer",
        json={
            "question_id": service.get_questions_for_form("A")[0]["question_id"],
            "selected_option": "B",
            "response_time_ms": 1000,
            "is_correct": True,
        },
    )

    assert response.status_code == 422
    assert assessment_answers(app_modules, db_session) == []


def test_answer_response_does_not_expose_correctness(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")

    response = client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/answer",
        json={
            "question_id": service.get_questions_for_form("A")[0]["question_id"],
            "selected_option": "A",
            "response_time_ms": 1000,
        },
    )

    assert response.status_code == 200
    data = response.json()
    assert data["recorded"] is True
    assert "is_correct" not in data
    assert "correct_option" not in data


def test_complete_requires_all_twelve_answers(app_modules, db_session, user):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")
    question = service.get_questions_for_form("A")[0]
    client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/answer",
        json={
            "question_id": question["question_id"],
            "selected_option": question["correct_option"],
            "response_time_ms": 1000,
        },
    )

    response = client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/complete"
    )

    assert response.status_code == 409


def test_pre_complete_calculates_deterministic_scores(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")
    answer_assessment(
        client,
        assessment["assessment_id"],
        service.get_questions_for_form("A"),
        correct_topics={"phishing", "passwords"},
    )

    data = complete_assessment(client, assessment["assessment_id"])

    assert data["phase"] == "PRE"
    assert data["total_score"] == 50.0
    assert data["topic_scores"] == {
        "phishing": 100.0,
        "passwords": 100.0,
        "malware": 0.0,
        "wifi": 0.0,
    }


def test_results_include_gain_only_after_post_is_completed(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    pre = start_assessment(client, "PRE")
    answer_assessment(
        client,
        pre["assessment_id"],
        service.get_questions_for_form("A"),
        correct_topics={"phishing", "passwords"},
    )
    complete_assessment(client, pre["assessment_id"])

    pre_results = client.get("/pilot/assessment/results").json()
    post = start_assessment(client, "POST")
    answer_assessment(
        client,
        post["assessment_id"],
        service.get_questions_for_form("B"),
        correct_topics={"phishing", "passwords", "malware", "wifi"},
    )
    complete_assessment(client, post["assessment_id"])
    final_results = client.get("/pilot/assessment/results").json()

    assert pre_results["pre"]["total_score"] == 50.0
    assert pre_results["post"] is None
    assert pre_results["gain"] is None
    assert final_results["post"]["total_score"] == 100.0
    assert final_results["gain"] == {
        "total": 50.0,
        "phishing": 0.0,
        "passwords": 0.0,
        "malware": 100.0,
        "wifi": 100.0,
    }


def test_completed_assessment_cannot_be_modified_or_completed_twice(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")
    answer_assessment(
        client,
        assessment["assessment_id"],
        service.get_questions_for_form("A"),
        correct_topics={"phishing", "passwords", "malware", "wifi"},
    )
    complete_assessment(client, assessment["assessment_id"])

    answer_response = client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/answer",
        json={
            "question_id": service.get_questions_for_form("A")[0]["question_id"],
            "selected_option": "B",
            "response_time_ms": 1000,
        },
    )
    complete_response = client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/complete"
    )

    assert answer_response.status_code == 409
    assert complete_response.status_code == 409


def test_revoke_blocks_new_assessment_activity_without_deleting_history(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")
    app_modules.pilot_service.revoke_pilot_consent(db_session, user.id)

    answer_response = client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/answer",
        json={
            "question_id": service.get_questions_for_form("A")[0]["question_id"],
            "selected_option": "B",
            "response_time_ms": 1000,
        },
    )
    status_response = client.get("/pilot/assessment/status")

    assert answer_response.status_code == 403
    assert status_response.status_code == 200
    assert status_response.json()["pre"]["assessment_id"] == (
        assessment["assessment_id"]
    )


def test_reaccept_preserves_participant_code_and_allows_assessment_continue(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accepted = accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")
    app_modules.pilot_service.revoke_pilot_consent(db_session, user.id)
    reaccepted = accept_consent(app_modules, db_session, user)

    response = client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/answer",
        json={
            "question_id": service.get_questions_for_form("A")[0]["question_id"],
            "selected_option": "B",
            "response_time_ms": 1000,
        },
    )

    assert reaccepted["participant_code"] == accepted["participant_code"]
    assert response.status_code == 200


def test_users_cannot_access_other_users_assessment(
    app_modules,
    db_session,
    user,
    other_user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    accept_consent(app_modules, db_session, other_user)
    first_client = make_pilot_client(app_modules, db_session, user)
    second_client = make_pilot_client(app_modules, db_session, other_user)
    assessment = start_assessment(first_client, "PRE")

    response = second_client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/answer",
        json={
            "question_id": service.get_questions_for_form("A")[0]["question_id"],
            "selected_option": "B",
            "response_time_ms": 1000,
        },
    )

    assert response.status_code == 404


def test_assessment_tables_do_not_store_pii(app_modules, db_session, user):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")
    answer_assessment(
        client,
        assessment["assessment_id"],
        service.get_questions_for_form("A"),
        correct_topics={"phishing", "passwords", "malware", "wifi"},
    )

    complete_assessment(client, assessment["assessment_id"])
    assessment_record = assessment_records(app_modules, db_session)[0]
    answer_record = assessment_answers(app_modules, db_session)[0]

    for record in (assessment_record, answer_record):
        assert not hasattr(record, "name")
        assert not hasattr(record, "email")
        assert not hasattr(record, "password")
        assert not hasattr(record, "google_sub")
        assert not hasattr(record, "participant_code")
