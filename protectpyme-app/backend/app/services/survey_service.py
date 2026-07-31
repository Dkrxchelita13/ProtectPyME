from dataclasses import dataclass
from typing import Dict, Iterable, List

from fastapi import HTTPException
from sqlalchemy.orm import Session

from app import models, schemas


DIAGNOSTIC_SURVEY_VERSION = "diagnostic_v1"
CATEGORY_ORDER = ("phishing", "passwords", "malware")
QUESTION_ORDER = (
    "P1_PHISH_HABITO",
    "P2_PHISH_CONOCIMIENTO",
    "P3_PASS_HABITO",
    "P4_PASS_CONOCIMIENTO",
    "P5_USB_HABITO",
    "P6_USB_CONOCIMIENTO",
)
MAX_CATEGORY_SCORE = 4


@dataclass(frozen=True)
class QuestionRule:
    category: str
    option_risk_scores: Dict[str, int]


QUESTION_RULES: Dict[str, QuestionRule] = {
    "P1_PHISH_HABITO": QuestionRule(
        category="phishing",
        option_risk_scores={"A": 2, "B": 0, "C": 1},
    ),
    "P2_PHISH_CONOCIMIENTO": QuestionRule(
        category="phishing",
        option_risk_scores={"A": 0, "B": 1, "C": 2},
    ),
    "P3_PASS_HABITO": QuestionRule(
        category="passwords",
        option_risk_scores={"A": 2, "B": 0, "C": 2},
    ),
    "P4_PASS_CONOCIMIENTO": QuestionRule(
        category="passwords",
        option_risk_scores={"A": 2, "B": 0, "C": 2},
    ),
    "P5_USB_HABITO": QuestionRule(
        category="malware",
        option_risk_scores={"A": 2, "B": 0, "C": 2},
    ),
    "P6_USB_CONOCIMIENTO": QuestionRule(
        category="malware",
        option_risk_scores={"A": 0, "B": 1, "C": 2},
    ),
}


def validate_survey_version(survey_version: str):
    if survey_version != DIAGNOSTIC_SURVEY_VERSION:
        raise HTTPException(
            status_code=400,
            detail="Invalid survey_version"
        )


def validate_answers(answers: Iterable[schemas.SurveyAnswerSubmit]):
    answers = list(answers)

    if len(answers) != len(QUESTION_ORDER):
        raise HTTPException(
            status_code=400,
            detail="Diagnostic survey requires exactly 6 answers"
        )

    seen_question_ids = set()

    for answer in answers:
        question_id = answer.question_id

        if question_id in seen_question_ids:
            raise HTTPException(
                status_code=400,
                detail="Duplicate question_id"
            )

        seen_question_ids.add(question_id)

        if question_id not in QUESTION_RULES:
            raise HTTPException(
                status_code=400,
                detail="Invalid question_id"
            )

        rule = QUESTION_RULES[question_id]

        if answer.category not in CATEGORY_ORDER:
            raise HTTPException(
                status_code=400,
                detail="Invalid category"
            )

        if answer.category != rule.category:
            raise HTTPException(
                status_code=400,
                detail="Category does not match question_id"
            )

        selected_option = normalize_selected_option(answer.selected_option)

        if selected_option not in rule.option_risk_scores:
            raise HTTPException(
                status_code=400,
                detail="Invalid selected_option"
            )

    missing_questions = set(QUESTION_ORDER) - seen_question_ids
    extra_questions = seen_question_ids - set(QUESTION_ORDER)

    if missing_questions:
        raise HTTPException(
            status_code=400,
            detail="Missing required survey questions"
        )

    if extra_questions:
        raise HTTPException(
            status_code=400,
            detail="Unexpected survey questions"
        )

    return answers


def normalize_selected_option(selected_option: str) -> str:
    return (selected_option or "").strip().upper()


def evaluate_answer(answer: schemas.SurveyAnswerSubmit) -> dict:
    rule = QUESTION_RULES[answer.question_id]
    selected_option = normalize_selected_option(answer.selected_option)
    risk_score = rule.option_risk_scores[selected_option]

    return {
        "question_id": answer.question_id,
        "category": rule.category,
        "selected_option": selected_option,
        "safe_score": 2 - risk_score,
        "risk_score": risk_score,
    }


def calculate_category_scores(evaluated_answers: Iterable[dict]) -> Dict[str, dict]:
    scores = {
        category: {
            "safe_score": 0,
            "max_score": MAX_CATEGORY_SCORE,
            "risk_score": 0,
        }
        for category in CATEGORY_ORDER
    }

    for answer in evaluated_answers:
        category_score = scores[answer["category"]]
        category_score["safe_score"] += answer["safe_score"]
        category_score["risk_score"] += answer["risk_score"]

    return scores


def get_primary_weakness(
    category_scores: Dict[str, dict],
    total_risk_score: int
) -> str:
    if total_risk_score == 0:
        return "none"

    return max(
        CATEGORY_ORDER,
        key=lambda category: category_scores[category]["risk_score"],
    )


def get_initial_risk(total_risk_score: int, category_scores: Dict[str, dict]) -> str:
    if (
        total_risk_score >= 6
        or any(score["risk_score"] == 4 for score in category_scores.values())
    ):
        return "ALTO"

    if 2 <= total_risk_score <= 5:
        return "MEDIO"

    return "BAJO"


def evaluate_submission(answers: Iterable[schemas.SurveyAnswerSubmit]) -> dict:
    ordered_answers = sorted(
        [evaluate_answer(answer) for answer in answers],
        key=lambda answer: QUESTION_ORDER.index(answer["question_id"]),
    )
    category_scores = calculate_category_scores(ordered_answers)
    total_risk_score = sum(
        score["risk_score"]
        for score in category_scores.values()
    )

    return {
        "answers": ordered_answers,
        "category_scores": category_scores,
        "primary_weakness": get_primary_weakness(
            category_scores,
            total_risk_score
        ),
        "initial_risk": get_initial_risk(total_risk_score, category_scores),
        "total_risk_score": total_risk_score,
    }


def get_submission(
    db: Session,
    user_id: int,
    survey_version: str = DIAGNOSTIC_SURVEY_VERSION,
):
    return (
        db.query(models.SurveySubmission)
        .filter(
            models.SurveySubmission.user_id == user_id,
            models.SurveySubmission.survey_version == survey_version,
        )
        .first()
    )


def submit_survey(
    db: Session,
    user_id: int,
    request: schemas.SurveySubmitRequest,
) -> dict:
    validate_survey_version(request.survey_version)
    answers = validate_answers(request.answers)

    if get_submission(db, user_id, request.survey_version):
        raise HTTPException(
            status_code=409,
            detail="Survey already submitted for this version"
        )

    evaluation = evaluate_submission(answers)
    category_scores = evaluation["category_scores"]

    submission = models.SurveySubmission(
        user_id=user_id,
        survey_version=request.survey_version,
        primary_weakness=evaluation["primary_weakness"],
        initial_risk=evaluation["initial_risk"],
        phishing_score=category_scores["phishing"]["safe_score"],
        passwords_score=category_scores["passwords"]["safe_score"],
        malware_score=category_scores["malware"]["safe_score"],
        phishing_risk_score=category_scores["phishing"]["risk_score"],
        passwords_risk_score=category_scores["passwords"]["risk_score"],
        malware_risk_score=category_scores["malware"]["risk_score"],
        total_risk_score=evaluation["total_risk_score"],
    )

    for answer in evaluation["answers"]:
        submission.answers.append(
            models.SurveyAnswer(
                user_id=user_id,
                question_id=answer["question_id"],
                category=answer["category"],
                selected_option=answer["selected_option"],
                safe_score=answer["safe_score"],
                risk_score=answer["risk_score"],
            )
        )

    try:
        db.add(submission)
        db.commit()
        db.refresh(submission)
    except Exception:
        db.rollback()
        raise

    return build_submit_response(submission)


def build_category_scores(submission: models.SurveySubmission) -> Dict[str, dict]:
    return {
        "phishing": {
            "safe_score": submission.phishing_score,
            "max_score": MAX_CATEGORY_SCORE,
            "risk_score": submission.phishing_risk_score,
        },
        "passwords": {
            "safe_score": submission.passwords_score,
            "max_score": MAX_CATEGORY_SCORE,
            "risk_score": submission.passwords_risk_score,
        },
        "malware": {
            "safe_score": submission.malware_score,
            "max_score": MAX_CATEGORY_SCORE,
            "risk_score": submission.malware_risk_score,
        },
    }


def build_submit_response(submission: models.SurveySubmission) -> dict:
    return {
        "submitted": True,
        "survey_version": submission.survey_version,
        "primary_weakness": submission.primary_weakness,
        "initial_risk": submission.initial_risk,
        "total_risk_score": submission.total_risk_score,
        "category_scores": build_category_scores(submission),
    }


def build_submission_response(submission: models.SurveySubmission) -> dict:
    answers = sorted(
        submission.answers,
        key=lambda answer: QUESTION_ORDER.index(answer.question_id),
    )

    return {
        "id": submission.id,
        "survey_version": submission.survey_version,
        "submitted_at": submission.created_at,
        "primary_weakness": submission.primary_weakness,
        "initial_risk": submission.initial_risk,
        "total_risk_score": submission.total_risk_score,
        "category_scores": build_category_scores(submission),
        "answers": [
            {
                "question_id": answer.question_id,
                "category": answer.category,
                "selected_option": answer.selected_option,
                "safe_score": answer.safe_score,
                "risk_score": answer.risk_score,
            }
            for answer in answers
        ],
    }


def build_status_response(submission: models.SurveySubmission | None) -> dict:
    if submission is None:
        return {
            "has_submitted": False,
            "survey_version": None,
            "submitted_at": None,
            "primary_weakness": None,
            "initial_risk": None,
        }

    return {
        "has_submitted": True,
        "survey_version": submission.survey_version,
        "submitted_at": submission.created_at,
        "primary_weakness": submission.primary_weakness,
        "initial_risk": submission.initial_risk,
    }
