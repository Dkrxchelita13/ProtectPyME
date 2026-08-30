import importlib
import os
import sys
import types
from collections import Counter
from datetime import datetime, timedelta
from types import SimpleNamespace
import uuid

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


def complete_pre_assessment(app_modules, db_session, user, client):
    service = app_modules.pilot_assessment_service
    assessment = start_assessment(client, "PRE")
    answer_assessment(
        client,
        assessment["assessment_id"],
        service.get_questions_for_form("A"),
        correct_topics={"phishing", "passwords", "malware", "wifi"},
    )
    complete_assessment(client, assessment["assessment_id"])
    return db_session.query(app_modules.models.PilotAssessment).filter_by(
        id=assessment["assessment_id"],
    ).first()


def add_decision(app_modules, db_session, user_id, scenario_id, created_at):
    decision = app_modules.models.Decision(
        user_id=user_id,
        scenario_id=scenario_id,
        choice="qa_intervention",
        is_correct=1,
        points_awarded=10,
        risk_level="low",
        feedback="QA intervention decision",
        response_time=1000,
        created_at=created_at,
    )
    db_session.add(decision)
    return decision


def add_minigame_session(
    app_modules,
    db_session,
    user_id,
    completed_at,
    status="completed",
):
    session = app_modules.models.MinigameSessionRecord(
        id=str(uuid.uuid4()),
        user_id=user_id,
        topic="phishing",
        risk="alto",
        minigame="quiz",
        item_ids=["pilot_qa_item"],
        concept_ids=["phishing.signals"],
        status=status,
        started_at=completed_at - timedelta(minutes=5),
        completed_at=completed_at if status == "completed" else None,
    )
    db_session.add(session)
    return session


def add_minimum_post_intervention(app_modules, db_session, user_id, pre_completed_at):
    for offset, scenario_id in enumerate((1, 2, 5), start=1):
        add_decision(
            app_modules,
            db_session,
            user_id,
            scenario_id,
            pre_completed_at + timedelta(minutes=offset),
        )

    add_minigame_session(
        app_modules,
        db_session,
        user_id,
        pre_completed_at + timedelta(minutes=10),
    )
    db_session.commit()


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


def test_question_bank_has_valid_options_and_answer_key(app_modules):
    service = app_modules.pilot_assessment_service

    for question in service.get_all_questions():
        options = question["options"]
        assert len(options) == 4
        assert all(option.strip() for option in options)
        assert len(set(options)) == 4
        assert question["correct_option"] in {"A", "B", "C", "D"}


def test_pre_post_pairs_keep_constructs_and_distinct_prompts(app_modules):
    service = app_modules.pilot_assessment_service

    pre_questions = service.get_questions_for_form("A")
    post_questions = service.get_questions_for_form("B")

    for pre_question, post_question in zip(pre_questions, post_questions):
        assert pre_question["topic"] == post_question["topic"]
        assert pre_question["construct"] == post_question["construct"]
        assert pre_question["prompt"] != post_question["prompt"]


def test_hardened_post_questions_preserve_metadata(app_modules):
    service = app_modules.pilot_assessment_service
    questions = {
        question["question_id"]: question
        for question in service.get_questions_for_form("B")
    }

    expected_metadata = {
        "post_phishing_01": ("B", "phishing", "senales_phishing"),
        "post_phishing_03": ("B", "phishing", "reporte_accion_segura"),
        "post_passwords_02": ("B", "passwords", "mfa_verificacion"),
        "post_malware_01": ("B", "malware", "usb_archivo_desconocido"),
        "post_wifi_03": ("B", "wifi", "trafico_exfiltracion"),
    }

    for question_id, metadata in expected_metadata.items():
        question = questions[question_id]
        assert (
            question["form"],
            question["topic"],
            question["construct"],
        ) == metadata


def test_hardened_bank_removes_known_weak_or_contaminating_phrases(
    app_modules,
):
    service = app_modules.pilot_assessment_service
    bank_text = " ".join(
        " ".join([
            question["prompt"],
            *question["options"],
        ])
        for question in service.get_all_questions()
    ).lower()

    forbidden_phrases = [
        "que use un saludo general",
        "portal falso",
        "cambiar credenciales si aplica",
        "recibes un adjunto inesperado de un contacto real",
        "solo afecta si el equipo no tiene teclado",
        "enviar mas archivos para verificar velocidad",
        "usarlo solo para enviar contraseñas",
        "conexion no reconocida que transfiere documentos internos",
        "problema exclusivo de la impresora",
    ]

    for phrase in forbidden_phrases:
        assert phrase not in bank_text


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
        "post_eligible": False,
        "intervention_progress": {
            "distinct_scenarios_completed": 0,
            "required_distinct_scenarios": 3,
            "completed_minigame_sessions": 0,
            "required_minigame_sessions": 1,
        },
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
    assert data["answered_question_ids"] == []
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
    assert second["answered_question_ids"] == []
    assert len(assessment_records(app_modules, db_session)) == 1


def test_started_assessment_status_reports_answered_question_ids(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")
    questions = service.get_questions_for_form("A")

    initial_status = client.get("/pilot/assessment/status").json()
    assert initial_status["pre"]["answered_count"] == 0
    assert initial_status["pre"]["answered_question_ids"] == []

    response = client.post(
        f"/pilot/assessment/{assessment['assessment_id']}/answer",
        json={
            "question_id": questions[0]["question_id"],
            "selected_option": questions[0]["correct_option"],
            "response_time_ms": 1000,
        },
    )

    assert response.status_code == 200
    one_answer_status = client.get("/pilot/assessment/status").json()
    assert one_answer_status["pre"]["answered_count"] == 1
    assert one_answer_status["pre"]["answered_question_ids"] == [
        "pre_phishing_01",
    ]


def test_answered_question_ids_follow_form_order_not_db_order(
    app_modules,
    db_session,
    user,
):
    service = app_modules.pilot_assessment_service
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    assessment = start_assessment(client, "PRE")
    questions = service.get_questions_for_form("A")

    for question in (questions[2], questions[0], questions[1]):
        response = client.post(
            f"/pilot/assessment/{assessment['assessment_id']}/answer",
            json={
                "question_id": question["question_id"],
                "selected_option": question["correct_option"],
                "response_time_ms": 1000,
            },
        )
        assert response.status_code == 200

    status_data = client.get("/pilot/assessment/status").json()
    recovered = start_assessment(client, "PRE")

    assert status_data["pre"]["answered_question_ids"] == [
        "pre_phishing_01",
        "pre_phishing_02",
        "pre_phishing_03",
    ]
    assert recovered["answered_question_ids"] == [
        "pre_phishing_01",
        "pre_phishing_02",
        "pre_phishing_03",
    ]


def test_recovered_start_keeps_public_questions_without_answer_state(
    app_modules,
    db_session,
    user,
):
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

    recovered = start_assessment(client, "PRE")

    assert recovered["assessment_id"] == assessment["assessment_id"]
    assert recovered["answered_question_ids"] == ["pre_phishing_01"]
    assert len(recovered["questions"]) == 12

    for public_question in recovered["questions"]:
        assert "correct_option" not in public_question
        assert "is_correct" not in public_question
        assert "selected_option" not in public_question
        assert "topic" not in public_question
        assert "construct" not in public_question


def test_other_user_answered_question_ids_are_isolated(
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
    first = start_assessment(first_client, "PRE")
    second = start_assessment(second_client, "PRE")
    first_question = service.get_questions_for_form("A")[0]

    response = first_client.post(
        f"/pilot/assessment/{first['assessment_id']}/answer",
        json={
            "question_id": first_question["question_id"],
            "selected_option": first_question["correct_option"],
            "response_time_ms": 1000,
        },
    )

    assert response.status_code == 200
    assert first_client.get("/pilot/assessment/status").json()["pre"][
        "answered_question_ids"
    ] == ["pre_phishing_01"]
    assert second_client.get("/pilot/assessment/status").json()["pre"][
        "answered_question_ids"
    ] == []
    assert second["answered_question_ids"] == []


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


def test_status_reports_post_ineligible_without_pre(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)

    data = client.get("/pilot/assessment/status").json()

    assert data["post_eligible"] is False
    assert data["intervention_progress"] == {
        "distinct_scenarios_completed": 0,
        "required_distinct_scenarios": 3,
        "completed_minigame_sessions": 0,
        "required_minigame_sessions": 1,
    }


def test_completed_pre_without_intervention_is_not_post_eligible(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    complete_pre_assessment(app_modules, db_session, user, client)

    status = client.get("/pilot/assessment/status").json()
    start_post = client.post(
        "/pilot/assessment/start",
        json={"phase": "POST"},
    )

    assert status["post_eligible"] is False
    assert status["intervention_progress"] == {
        "distinct_scenarios_completed": 0,
        "required_distinct_scenarios": 3,
        "completed_minigame_sessions": 0,
        "required_minigame_sessions": 1,
    }
    assert start_post.status_code == 409


def test_repeated_same_scenario_counts_once_for_post_eligibility(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    pre = complete_pre_assessment(app_modules, db_session, user, client)

    for offset in range(1, 4):
        add_decision(
            app_modules,
            db_session,
            user.id,
            1,
            pre.completed_at + timedelta(minutes=offset),
        )

    add_minigame_session(
        app_modules,
        db_session,
        user.id,
        pre.completed_at + timedelta(minutes=10),
    )
    db_session.commit()

    status = client.get("/pilot/assessment/status").json()

    assert status["post_eligible"] is False
    assert status["intervention_progress"]["distinct_scenarios_completed"] == 1
    assert status["intervention_progress"]["completed_minigame_sessions"] == 1


def test_three_distinct_scenarios_without_completed_minigame_is_not_eligible(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    pre = complete_pre_assessment(app_modules, db_session, user, client)

    for offset, scenario_id in enumerate((1, 2, 5), start=1):
        add_decision(
            app_modules,
            db_session,
            user.id,
            scenario_id,
            pre.completed_at + timedelta(minutes=offset),
        )

    db_session.commit()

    status = client.get("/pilot/assessment/status").json()

    assert status["post_eligible"] is False
    assert status["intervention_progress"]["distinct_scenarios_completed"] == 3
    assert status["intervention_progress"]["completed_minigame_sessions"] == 0


def test_two_distinct_scenarios_with_minigame_is_not_eligible(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    pre = complete_pre_assessment(app_modules, db_session, user, client)

    for offset, scenario_id in enumerate((1, 2), start=1):
        add_decision(
            app_modules,
            db_session,
            user.id,
            scenario_id,
            pre.completed_at + timedelta(minutes=offset),
        )

    add_minigame_session(
        app_modules,
        db_session,
        user.id,
        pre.completed_at + timedelta(minutes=10),
    )
    db_session.commit()

    status = client.get("/pilot/assessment/status").json()

    assert status["post_eligible"] is False
    assert status["intervention_progress"]["distinct_scenarios_completed"] == 2
    assert status["intervention_progress"]["completed_minigame_sessions"] == 1


def test_started_minigame_does_not_count_for_post_eligibility(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    pre = complete_pre_assessment(app_modules, db_session, user, client)

    for offset, scenario_id in enumerate((1, 2, 5), start=1):
        add_decision(
            app_modules,
            db_session,
            user.id,
            scenario_id,
            pre.completed_at + timedelta(minutes=offset),
        )

    add_minigame_session(
        app_modules,
        db_session,
        user.id,
        pre.completed_at + timedelta(minutes=10),
        status="started",
    )
    db_session.commit()

    status = client.get("/pilot/assessment/status").json()

    assert status["post_eligible"] is False
    assert status["intervention_progress"]["distinct_scenarios_completed"] == 3
    assert status["intervention_progress"]["completed_minigame_sessions"] == 0


def test_minimum_intervention_enables_post_start(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    pre = complete_pre_assessment(app_modules, db_session, user, client)
    add_minimum_post_intervention(
        app_modules,
        db_session,
        user.id,
        pre.completed_at,
    )

    status = client.get("/pilot/assessment/status").json()
    post = start_assessment(client, "POST")

    assert status["post_eligible"] is True
    assert status["intervention_progress"] == {
        "distinct_scenarios_completed": 3,
        "required_distinct_scenarios": 3,
        "completed_minigame_sessions": 1,
        "required_minigame_sessions": 1,
    }
    assert post["phase"] == "POST"
    assert post["answered_question_ids"] == []


def test_activity_before_pre_completion_does_not_count_for_post_eligibility(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    pre = complete_pre_assessment(app_modules, db_session, user, client)

    for offset, scenario_id in enumerate((1, 2, 5), start=1):
        add_decision(
            app_modules,
            db_session,
            user.id,
            scenario_id,
            pre.completed_at - timedelta(minutes=offset),
        )

    add_minigame_session(
        app_modules,
        db_session,
        user.id,
        pre.completed_at - timedelta(minutes=10),
    )
    db_session.commit()

    status = client.get("/pilot/assessment/status").json()

    assert status["post_eligible"] is False
    assert status["intervention_progress"]["distinct_scenarios_completed"] == 0
    assert status["intervention_progress"]["completed_minigame_sessions"] == 0


def test_other_user_activity_does_not_count_for_post_eligibility(
    app_modules,
    db_session,
    user,
    other_user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    pre = complete_pre_assessment(app_modules, db_session, user, client)
    add_minimum_post_intervention(
        app_modules,
        db_session,
        other_user.id,
        pre.completed_at,
    )

    status = client.get("/pilot/assessment/status").json()

    assert status["post_eligible"] is False
    assert status["intervention_progress"]["distinct_scenarios_completed"] == 0
    assert status["intervention_progress"]["completed_minigame_sessions"] == 0


def test_started_post_can_be_recovered_without_creating_another_post(
    app_modules,
    db_session,
    user,
):
    accept_consent(app_modules, db_session, user)
    client = make_pilot_client(app_modules, db_session, user)
    pre = complete_pre_assessment(app_modules, db_session, user, client)
    add_minimum_post_intervention(
        app_modules,
        db_session,
        user.id,
        pre.completed_at,
    )
    first = start_assessment(client, "POST")

    for record in db_session.query(app_modules.models.Decision).all():
        db_session.delete(record)

    for record in db_session.query(app_modules.models.MinigameSessionRecord).all():
        db_session.delete(record)

    db_session.commit()
    second = start_assessment(client, "POST")

    assert second["assessment_id"] == first["assessment_id"]
    assert len(assessment_records(app_modules, db_session)) == 2


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
    pre_record = db_session.query(app_modules.models.PilotAssessment).filter_by(
        id=pre["assessment_id"],
    ).first()
    add_minimum_post_intervention(
        app_modules,
        db_session,
        user.id,
        pre_record.completed_at,
    )

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
