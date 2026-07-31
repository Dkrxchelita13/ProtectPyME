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

TestBase = declarative_base()
engine = create_engine(
    "sqlite://",
    connect_args={"check_same_thread": False},
    poolclass=StaticPool,
)
TestingSessionLocal = sessionmaker(
    autocommit=False,
    autoflush=False,
    bind=engine,
)


def get_test_db():
    db = TestingSessionLocal()

    try:
        yield db
    finally:
        db.close()


fake_database = types.ModuleType("app.database")
fake_database.Base = TestBase
fake_database.engine = engine
fake_database.SessionLocal = TestingSessionLocal
fake_database.get_db = get_test_db
sys.modules["app.database"] = fake_database

from app import models, schemas  # noqa: E402
from app.auth import create_access_token  # noqa: E402
from app.routes.survey import router  # noqa: E402
from app.services import survey_service  # noqa: E402


app = FastAPI()
app.include_router(router)
client = TestClient(app)


def safe_payload():
    return {
        "survey_version": "diagnostic_v1",
        "answers": [
            {
                "question_id": "P1_PHISH_HABITO",
                "category": "phishing",
                "selected_option": "B",
            },
            {
                "question_id": "P2_PHISH_CONOCIMIENTO",
                "category": "phishing",
                "selected_option": "A",
            },
            {
                "question_id": "P3_PASS_HABITO",
                "category": "passwords",
                "selected_option": "B",
            },
            {
                "question_id": "P4_PASS_CONOCIMIENTO",
                "category": "passwords",
                "selected_option": "B",
            },
            {
                "question_id": "P5_USB_HABITO",
                "category": "malware",
                "selected_option": "B",
            },
            {
                "question_id": "P6_USB_CONOCIMIENTO",
                "category": "malware",
                "selected_option": "A",
            },
        ],
    }


@pytest.fixture(autouse=True)
def reset_database():
    TestBase.metadata.drop_all(bind=engine)
    TestBase.metadata.create_all(bind=engine)
    yield
    TestBase.metadata.drop_all(bind=engine)


def auth_headers():
    db = TestingSessionLocal()
    user = models.User(
        name="Survey User",
        email="survey@example.com",
        password="not-used",
    )
    db.add(user)
    db.commit()
    db.refresh(user)
    token = create_access_token({"sub": str(user.id)})
    db.close()

    return {
        "Authorization": f"Bearer {token}"
    }


def submit(payload):
    return client.post(
        "/survey/submit",
        json=payload,
        headers=auth_headers()
    )


def test_evaluate_answer_safe_score_is_derived_from_risk_score():
    answer = schemas.SurveyAnswerSubmit(
        question_id="P1_PHISH_HABITO",
        category="phishing",
        selected_option="A",
    )

    result = survey_service.evaluate_answer(answer)

    assert result["risk_score"] == 2
    assert result["safe_score"] == 0


def test_all_safe_answers_are_low_risk():
    payload = safe_payload()
    answers = [
        schemas.SurveyAnswerSubmit(**answer)
        for answer in payload["answers"]
    ]

    result = survey_service.evaluate_submission(answers)

    assert result["total_risk_score"] == 0
    assert result["initial_risk"] == "BAJO"


def test_one_high_risk_answer_is_medium_risk():
    payload = safe_payload()
    payload["answers"][0]["selected_option"] = "A"
    answers = [
        schemas.SurveyAnswerSubmit(**answer)
        for answer in payload["answers"]
    ]

    result = survey_service.evaluate_submission(answers)

    assert result["total_risk_score"] == 2
    assert result["initial_risk"] == "MEDIO"


def test_two_high_risk_answers_in_same_category_are_high_risk():
    payload = safe_payload()
    payload["answers"][0]["selected_option"] = "A"
    payload["answers"][1]["selected_option"] = "C"
    answers = [
        schemas.SurveyAnswerSubmit(**answer)
        for answer in payload["answers"]
    ]

    result = survey_service.evaluate_submission(answers)

    assert result["category_scores"]["phishing"]["risk_score"] == 4
    assert result["initial_risk"] == "ALTO"


def test_global_risk_score_six_or_more_is_high_risk():
    payload = safe_payload()
    payload["answers"][0]["selected_option"] = "A"
    payload["answers"][2]["selected_option"] = "A"
    payload["answers"][4]["selected_option"] = "A"
    answers = [
        schemas.SurveyAnswerSubmit(**answer)
        for answer in payload["answers"]
    ]

    result = survey_service.evaluate_submission(answers)

    assert result["total_risk_score"] == 6
    assert result["initial_risk"] == "ALTO"


def test_primary_weakness_tie_prefers_phishing():
    payload = safe_payload()
    payload["answers"][0]["selected_option"] = "A"
    payload["answers"][2]["selected_option"] = "A"
    payload["answers"][4]["selected_option"] = "A"
    answers = [
        schemas.SurveyAnswerSubmit(**answer)
        for answer in payload["answers"]
    ]

    result = survey_service.evaluate_submission(answers)

    assert result["primary_weakness"] == "phishing"


def test_post_without_authentication_keeps_httpbearer_behavior():
    response = client.post("/survey/submit", json=safe_payload())

    assert response.status_code in (401, 403)


def test_post_with_five_answers_is_rejected():
    payload = safe_payload()
    payload["answers"] = payload["answers"][:5]

    response = submit(payload)

    assert response.status_code == 400


def test_post_with_seven_answers_is_rejected():
    payload = safe_payload()
    payload["answers"].append(
        {
            "question_id": "EXTRA",
            "category": "phishing",
            "selected_option": "A",
        }
    )

    response = submit(payload)

    assert response.status_code == 400


def test_invalid_question_id_is_rejected():
    payload = safe_payload()
    payload["answers"][0]["question_id"] = "INVALID"

    response = submit(payload)

    assert response.status_code == 400


def test_duplicate_question_id_is_rejected():
    payload = safe_payload()
    payload["answers"][1]["question_id"] = "P1_PHISH_HABITO"

    response = submit(payload)

    assert response.status_code == 400


def test_wrong_category_for_question_id_is_rejected():
    payload = safe_payload()
    payload["answers"][0]["category"] = "malware"

    response = submit(payload)

    assert response.status_code == 400


def test_invalid_selected_option_is_rejected():
    payload = safe_payload()
    payload["answers"][0]["selected_option"] = "D"

    response = submit(payload)

    assert response.status_code == 400


def test_invalid_survey_version_is_rejected():
    payload = safe_payload()
    payload["survey_version"] = "other"

    response = submit(payload)

    assert response.status_code == 400


def test_valid_post_returns_created_response():
    response = submit(safe_payload())

    assert response.status_code == 201
    data = response.json()
    assert data["submitted"] is True
    assert data["survey_version"] == "diagnostic_v1"
    assert data["total_risk_score"] == 0
    assert data["initial_risk"] == "BAJO"


def test_second_post_same_version_returns_conflict():
    headers = auth_headers()

    first = client.post(
        "/survey/submit",
        json=safe_payload(),
        headers=headers
    )
    second = client.post(
        "/survey/submit",
        json=safe_payload(),
        headers=headers
    )

    assert first.status_code == 201
    assert second.status_code == 409


def test_status_before_submission_returns_false():
    response = client.get(
        "/survey/status",
        headers=auth_headers()
    )

    assert response.status_code == 200
    assert response.json()["has_submitted"] is False


def test_status_after_submission_returns_true():
    headers = auth_headers()

    client.post(
        "/survey/submit",
        json=safe_payload(),
        headers=headers
    )
    response = client.get(
        "/survey/status",
        headers=headers
    )

    assert response.status_code == 200
    assert response.json()["has_submitted"] is True
    assert response.json()["survey_version"] == "diagnostic_v1"


def test_get_me_without_submission_returns_not_found():
    response = client.get(
        "/survey/me",
        headers=auth_headers()
    )

    assert response.status_code == 404


def test_get_me_returns_six_answers_in_canonical_order():
    headers = auth_headers()

    client.post(
        "/survey/submit",
        json=safe_payload(),
        headers=headers
    )
    response = client.get(
        "/survey/me",
        headers=headers
    )

    assert response.status_code == 200
    answers = response.json()["answers"]
    assert len(answers) == 6
    assert [answer["question_id"] for answer in answers] == list(
        survey_service.QUESTION_ORDER
    )
