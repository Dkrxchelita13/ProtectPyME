import os
import sys
import types

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

os.environ.setdefault("SECRET_KEY", "test-secret")

TestBase = declarative_base()
TestBase.__test__ = False


def get_test_db():
    db = TestingSessionLocal()

    try:
        yield db
    finally:
        db.close()


engine = create_engine(
    "sqlite://",
    connect_args={"check_same_thread": False},
    poolclass=StaticPool,
)
TestingSessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

fake_database = types.ModuleType("app.database")
fake_database.Base = TestBase
fake_database.engine = engine
fake_database.SessionLocal = TestingSessionLocal
fake_database.get_db = get_test_db
sys.modules["app.database"] = fake_database

NEW_SCENARIOS = [
    (5, "phishing", "reportar_phishing", "hacer_clic_login"),
    (6, "passwords", "rechazar_reportar_ti", "entregar_password"),
    (7, "wifi", "revisar_bloquear", "ignorar_alerta"),
]


@pytest.fixture()
def db_session():
    from app import models

    models.Base.metadata.create_all(bind=engine)

    db = TestingSessionLocal()
    try:
        yield db
    finally:
        db.close()
        models.Base.metadata.drop_all(bind=engine)


@pytest.fixture()
def client(db_session):
    from app.database import get_db
    from app.routes.decisions import router as decisions_router

    app = FastAPI()
    app.include_router(decisions_router)

    def override_get_db():
        yield db_session

    app.dependency_overrides[get_db] = override_get_db

    with TestClient(app) as test_client:
        yield test_client


def create_user(db_session, email="scenario@test.local"):
    from app import models

    user = models.User(
        email=email,
        name="Scenario Tester",
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


def create_scenario(db_session, scenario_id, category, correct_choice):
    from app import models

    scenario = models.Scenario(
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


def auth_headers(user):
    from app.auth import create_access_token

    token = create_access_token({"sub": str(user.id)})
    return {"Authorization": f"Bearer {token}"}


@pytest.mark.parametrize("scenario_id,category,correct_choice,incorrect_choice", NEW_SCENARIOS)
def test_new_scenarios_accept_correct_decision_over_http(
    client,
    db_session,
    scenario_id,
    category,
    correct_choice,
    incorrect_choice,
):
    from app import models

    user = create_user(db_session, email=f"correct-{scenario_id}@test.local")
    create_scenario(db_session, scenario_id, category, correct_choice)

    response = client.post(
        "/decisions/",
        json={
            "scenario_id": scenario_id,
            "choice": correct_choice,
            "response_time": 12,
        },
        headers=auth_headers(user),
    )

    assert response.status_code == 200
    data = response.json()
    assert data["scenario_id"] == scenario_id
    assert data["points_awarded"] == 20

    decision = db_session.query(models.Decision).filter_by(id=data["id"]).one()
    assert decision.user_id == user.id
    assert decision.scenario_id == scenario_id
    assert decision.is_correct == 1

    db_session.refresh(user)
    assert user.total_decisions == 1
    assert user.correct_decisions == 1
    assert user.total_points == 20

    category_points = db_session.query(models.UserCategoryPoints).filter_by(
        user_id=user.id,
        category=category,
    ).one()
    assert category_points.total_points == 20

    scenario_one_decisions = db_session.query(models.Decision).filter_by(scenario_id=1).count()
    assert scenario_one_decisions == 0


@pytest.mark.parametrize("scenario_id,category,correct_choice,incorrect_choice", NEW_SCENARIOS)
def test_new_scenarios_accept_incorrect_decision_and_keep_category(
    client,
    db_session,
    scenario_id,
    category,
    correct_choice,
    incorrect_choice,
):
    from app import models
    from app.services.analytics import get_user_analytics

    user = create_user(db_session, email=f"incorrect-{scenario_id}@test.local")
    create_scenario(db_session, scenario_id, category, correct_choice)

    response = client.post(
        "/decisions/",
        json={
            "scenario_id": scenario_id,
            "choice": incorrect_choice,
            "response_time": 18,
        },
        headers=auth_headers(user),
    )

    assert response.status_code == 200
    data = response.json()
    assert data["scenario_id"] == scenario_id
    assert data["points_awarded"] == 0

    decision = db_session.query(models.Decision).filter_by(id=data["id"]).one()
    assert decision.user_id == user.id
    assert decision.scenario_id == scenario_id
    assert decision.is_correct == 0
    assert decision.risk_level == "medium"

    db_session.refresh(user)
    assert user.total_decisions == 1
    assert user.correct_decisions == 0
    assert user.total_points == 0

    analytics = get_user_analytics(db_session, user.id)
    assert analytics["most_failed_category"] == category

    scenario_one_decisions = db_session.query(models.Decision).filter_by(scenario_id=1).count()
    assert scenario_one_decisions == 0
