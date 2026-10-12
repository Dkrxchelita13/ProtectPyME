import importlib
import os
import sys
import types
from datetime import datetime
from types import SimpleNamespace

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool


os.environ.setdefault("SECRET_KEY", "test-secret")

BACKEND_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
if BACKEND_DIR not in sys.path:
    sys.path.insert(0, BACKEND_DIR)


SCENARIO_CONFIG = {
    1: ("phishing", "reportar_phishing", "abrir_correo"),
    2: ("passwords", "cambiar_password", "ignorar_alerta"),
    3: ("malware", "no_conectar", "conectar_usb"),
    5: ("phishing", "reportar_phishing", "hacer_clic_login"),
    6: ("passwords", "rechazar_reportar_ti", "entregar_password"),
    7: ("wifi", "revisar_bloquear", "ignorar_alerta"),
}


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
        "app.crud.crud_decisions",
        "app.routes.decisions",
        "app.routes.progress",
        "app.services.audit_service",
        "app.services.progress_service",
    )
    previous_modules = {
        name: sys.modules.get(name)
        for name in managed_modules
    }

    for name in managed_modules:
        sys.modules.pop(name, None)

    for package_name, attribute_names in (
        ("app", ("auth", "database", "models", "schemas")),
        ("app.crud", ("crud_decisions",)),
        ("app.routes", ("decisions", "progress")),
        ("app.services", ("audit_service", "progress_service")),
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
    crud_decisions = importlib.import_module("app.crud.crud_decisions")
    decisions_route = importlib.import_module("app.routes.decisions")
    progress_route = importlib.import_module("app.routes.progress")
    progress_service = importlib.import_module("app.services.progress_service")

    try:
        yield SimpleNamespace(
            auth=auth,
            crud_decisions=crud_decisions,
            decisions_route=decisions_route,
            engine=database.engine,
            get_db=app_database.get_db,
            models=models,
            progress_route=progress_route,
            progress_service=progress_service,
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
def client(app_modules, db_session):
    app = FastAPI()
    app.include_router(app_modules.decisions_route.router)
    app.include_router(app_modules.progress_route.router)

    def override_get_db():
        yield db_session

    app.dependency_overrides[app_modules.get_db] = override_get_db

    with TestClient(app) as test_client:
        yield test_client


def create_user(app_modules, db_session, email="progress@example.com"):
    user = app_modules.models.User(
        email=email,
        name="Progress User",
        password="not-used",
        level="Bronze",
        total_points=0,
        total_decisions=0,
        correct_decisions=0,
        risk_score=0,
    )
    db_session.add(user)
    db_session.commit()
    db_session.refresh(user)
    return user


def create_scenario(app_modules, db_session, scenario_id):
    category, correct_choice, _ = SCENARIO_CONFIG[scenario_id]
    scenario = app_modules.models.Scenario(
        id=scenario_id,
        title=f"Scenario {scenario_id}",
        description=f"Scenario {scenario_id} description",
        difficulty="medium",
        category=category,
        correct_choice=correct_choice,
        points_correct=10,
        points_incorrect=0,
    )
    db_session.add(scenario)
    db_session.commit()
    return scenario


def ensure_scenarios(app_modules, db_session, scenario_ids):
    for scenario_id in scenario_ids:
        create_scenario(app_modules, db_session, scenario_id)


def auth_headers(app_modules, user):
    token = app_modules.auth.create_access_token({"sub": str(user.id)})
    return {"Authorization": f"Bearer {token}"}


def post_decision(
    app_modules,
    client,
    user,
    scenario_id,
    choice=None,
    response_time=12
):
    _, correct_choice, _ = SCENARIO_CONFIG[scenario_id]

    response = client.post(
        "/decisions/",
        json={
            "scenario_id": scenario_id,
            "choice": choice if choice is not None else correct_choice,
            "response_time": response_time,
        },
        headers=auth_headers(app_modules, user),
    )

    assert response.status_code == 200
    return response


def get_progress(app_modules, client, user):
    response = client.get(
        "/progress/me",
        headers=auth_headers(app_modules, user),
    )

    assert response.status_code == 200
    return response.json()


def add_decision(
    app_modules,
    db_session,
    user,
    scenario_id,
    is_correct=0
):
    decision = app_modules.models.Decision(
        user_id=user.id,
        scenario_id=scenario_id,
        choice="recorded_choice",
        is_correct=is_correct,
        points_awarded=0,
        risk_level="medium",
        feedback="recorded",
        response_time=12,
        created_at=datetime.utcnow(),
    )
    db_session.add(decision)
    db_session.commit()
    return decision


def test_new_user_progress_defaults(client, app_modules, db_session):
    user = create_user(app_modules, db_session)

    progress = get_progress(app_modules, client, user)

    assert progress["user_id"] == user.id
    assert progress["highest_initial_stage_unlocked"] == 1
    assert progress["highest_intermediate_stage_unlocked"] == 1
    assert progress["minigames_unlocked"] is False
    assert progress["updated_at"]


def test_correct_scenario_one_unlocks_initial_stage_two(
    client,
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    ensure_scenarios(app_modules, db_session, [1])

    post_decision(app_modules, client, user, 1)

    progress = get_progress(app_modules, client, user)
    assert progress["highest_initial_stage_unlocked"] == 2
    assert progress["minigames_unlocked"] is False


def test_incorrect_scenario_one_unlocks_initial_stage_two(
    client,
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    ensure_scenarios(app_modules, db_session, [1])
    _, _, incorrect_choice = SCENARIO_CONFIG[1]

    post_decision(
        app_modules,
        client,
        user,
        1,
        choice=incorrect_choice,
    )

    progress = get_progress(app_modules, client, user)
    assert progress["highest_initial_stage_unlocked"] == 2
    assert progress["minigames_unlocked"] is False


def test_repeating_scenario_one_is_idempotent(
    client,
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    ensure_scenarios(app_modules, db_session, [1])

    post_decision(app_modules, client, user, 1, response_time=10)
    post_decision(app_modules, client, user, 1, response_time=11)
    post_decision(app_modules, client, user, 1, response_time=12)

    progress = get_progress(app_modules, client, user)
    assert progress["highest_initial_stage_unlocked"] == 2


def test_scenario_two_after_one_unlocks_stage_three(
    client,
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    ensure_scenarios(app_modules, db_session, [1, 2])

    post_decision(app_modules, client, user, 1)
    post_decision(app_modules, client, user, 2)

    progress = get_progress(app_modules, client, user)
    assert progress["highest_initial_stage_unlocked"] == 3
    assert progress["minigames_unlocked"] is False


def test_scenario_three_after_sequence_unlocks_minigames(
    client,
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    ensure_scenarios(app_modules, db_session, [1, 2, 3])

    post_decision(app_modules, client, user, 1)
    post_decision(app_modules, client, user, 2)
    post_decision(app_modules, client, user, 3)

    progress = get_progress(app_modules, client, user)
    assert progress["highest_initial_stage_unlocked"] == 4
    assert progress["minigames_unlocked"] is True


def test_out_of_sequence_scenario_three_does_not_infer_missing_stages(
    client,
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    ensure_scenarios(app_modules, db_session, [3])

    post_decision(app_modules, client, user, 3)

    progress = get_progress(app_modules, client, user)
    assert progress["highest_initial_stage_unlocked"] == 1
    assert progress["minigames_unlocked"] is False


def test_intermediate_sequence_uses_playable_backend_ids(
    client,
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    ensure_scenarios(app_modules, db_session, [5, 6, 7])

    post_decision(app_modules, client, user, 5)
    post_decision(app_modules, client, user, 6)
    post_decision(app_modules, client, user, 7)

    progress = get_progress(app_modules, client, user)
    assert progress["highest_initial_stage_unlocked"] == 1
    assert progress["highest_intermediate_stage_unlocked"] == 4
    assert progress["minigames_unlocked"] is True


def test_users_have_independent_progress(client, app_modules, db_session):
    user = create_user(app_modules, db_session, email="user-one@test.local")
    other_user = create_user(app_modules, db_session, email="user-two@test.local")
    ensure_scenarios(app_modules, db_session, [1])

    post_decision(app_modules, client, user, 1)

    user_progress = get_progress(app_modules, client, user)
    other_progress = get_progress(app_modules, client, other_user)
    assert user_progress["highest_initial_stage_unlocked"] == 2
    assert other_progress["highest_initial_stage_unlocked"] == 1


def test_get_progress_requires_authentication(client):
    response = client.get("/progress/me")

    assert response.status_code in (401, 403)


def test_progress_has_no_public_user_or_post_routes(
    client,
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)

    get_other_user = client.get(
        f"/progress/{user.id}",
        headers=auth_headers(app_modules, user),
    )
    post_progress = client.post(
        "/progress/me",
        json={"highest_initial_stage_unlocked": 4},
        headers=auth_headers(app_modules, user),
    )

    assert get_other_user.status_code == 404
    assert post_progress.status_code == 405


def test_progress_is_monotonic_when_lower_scenario_repeats(
    client,
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    ensure_scenarios(app_modules, db_session, [1, 2])

    post_decision(app_modules, client, user, 1)
    post_decision(app_modules, client, user, 2)
    post_decision(app_modules, client, user, 1)

    progress = get_progress(app_modules, client, user)
    assert progress["highest_initial_stage_unlocked"] == 3


def test_decision_and_progress_rollback_together(
    client,
    app_modules,
    db_session,
    monkeypatch,
):
    user = create_user(app_modules, db_session)
    ensure_scenarios(app_modules, db_session, [1])

    def fail_progress_update(*args, **kwargs):
        raise RuntimeError("forced progress failure")

    monkeypatch.setattr(
        app_modules.crud_decisions.progress_service,
        "apply_scenario_completion",
        fail_progress_update,
    )

    response = client.post(
        "/decisions/",
        json={
            "scenario_id": 1,
            "choice": SCENARIO_CONFIG[1][1],
            "response_time": 12,
        },
        headers=auth_headers(app_modules, user),
    )

    assert response.status_code == 500
    assert db_session.query(app_modules.models.Decision).count() == 0
    assert db_session.query(app_modules.models.UserProgress).count() == 0


def test_backfill_infers_contiguous_initial_progress(
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    add_decision(app_modules, db_session, user, 1)
    add_decision(app_modules, db_session, user, 2)

    report = app_modules.progress_service.backfill_user_progress(
        db_session,
        dry_run=True,
    )

    assert report["dry_run"] is True
    assert report["users_seen"] == 1
    result = report["results"][0]
    assert result["target"]["highest_initial_stage_unlocked"] == 3
    assert result["target"]["minigames_unlocked"] is False
    assert result["inconsistent_scenario_ids"] == []
    assert db_session.query(app_modules.models.UserProgress).count() == 0


def test_backfill_does_not_infer_missing_previous_scenarios(
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    add_decision(app_modules, db_session, user, 3)

    report = app_modules.progress_service.backfill_user_progress(
        db_session,
        dry_run=True,
    )

    result = report["results"][0]
    assert result["target"]["highest_initial_stage_unlocked"] == 1
    assert result["target"]["minigames_unlocked"] is False
    assert result["inconsistent_scenario_ids"] == [3]


def test_backfill_uses_incorrect_decisions_as_completion(
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    add_decision(app_modules, db_session, user, 1, is_correct=0)

    report = app_modules.progress_service.backfill_user_progress(
        db_session,
        dry_run=True,
    )

    result = report["results"][0]
    assert result["target"]["highest_initial_stage_unlocked"] == 2


def test_backfill_apply_is_monotonic_and_conservative(
    app_modules,
    db_session,
):
    user = create_user(app_modules, db_session)
    existing = app_modules.models.UserProgress(
        user_id=user.id,
        highest_initial_stage_unlocked=3,
        highest_intermediate_stage_unlocked=1,
        minigames_unlocked=False,
    )
    db_session.add(existing)
    add_decision(app_modules, db_session, user, 1)

    report = app_modules.progress_service.backfill_user_progress(
        db_session,
        dry_run=False,
    )
    db_session.commit()

    result = report["results"][0]
    db_session.refresh(existing)
    assert result["action"] == "noop"
    assert existing.highest_initial_stage_unlocked == 3
