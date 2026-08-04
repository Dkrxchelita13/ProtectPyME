import unicodedata

from fastapi import HTTPException
from sqlalchemy.orm import Session



from app.ai.predict import RiskPredictor

from app.ai.rules import get_recommendation

from app.services.analytics import get_user_analytics
from app.models import SurveySubmission, User
from app.services.survey_service import DIAGNOSTIC_SURVEY_VERSION


MIN_BEHAVIORAL_DECISIONS = 3
PLAYABLE_TOPICS = ("phishing", "passwords", "malware", "wifi")
PLAYABLE_TOPIC_ALIASES = {
    "password": "passwords",
    "contrasenas": "passwords",
    "malicious_software": "malware",
    "redes_wifi": "wifi",
    "network": "wifi",
}
SCENARIO_TOPIC_MAP = {
    1: "phishing",
    2: "passwords",
    3: "malware",
    4: "wifi",
}
FINAL_FALLBACK_PLAYABLE_TOPIC = "phishing"

# Singleton
predictor = RiskPredictor()


def normalize_playable_topic(topic: str) -> str | None:
    normalized = _normalize_topic_text(topic)

    if normalized in PLAYABLE_TOPICS:
        return normalized

    return PLAYABLE_TOPIC_ALIASES.get(normalized)


def resolve_playable_topic(
    topic: str | None,
    recommended_scenario: int | None = None,
    survey_primary_weakness: str | None = None
) -> str:
    playable_topic = normalize_playable_topic(topic)

    if playable_topic:
        return playable_topic

    scenario_topic = SCENARIO_TOPIC_MAP.get(_safe_int(recommended_scenario))

    if scenario_topic:
        return scenario_topic

    survey_topic = normalize_playable_topic(survey_primary_weakness)

    if survey_topic:
        return survey_topic

    # Last defense: scenario 1 and phishing banks are guaranteed to exist.
    return FINAL_FALLBACK_PLAYABLE_TOPIC


def _normalize_topic_text(topic: str | None) -> str:
    raw = str(topic or "").strip().lower()
    normalized = unicodedata.normalize("NFKD", raw)

    return "".join(
        character
        for character in normalized
        if not unicodedata.combining(character)
    )


def _safe_int(value) -> int | None:
    try:
        return int(value)
    except (TypeError, ValueError):
        return None


SURVEY_RECOMMENDATIONS = {
    "phishing": {
        "training": "phishing",
        "scenario": 1,
    },
    "passwords": {
        "training": "passwords",
        "scenario": 2,
    },
    "malware": {
        "training": "malware",
        "scenario": 3,
    },
    "none": {
        "training": "general",
        "scenario": 1,
    },
}


SURVEY_MESSAGES = {
    ("phishing", "ALTO"): (
        "Necesitas reforzar la identificación de remitentes, enlaces y mensajes "
        "sospechosos antes de interactuar con ellos."
    ),
    ("phishing", "MEDIO"): (
        "Reconoces algunas señales de phishing, pero debes revisar con mayor "
        "atención el remitente, el dominio y los enlaces."
    ),
    ("phishing", "BAJO"): (
        "Demuestras buenas prácticas iniciales frente al phishing. Continúa "
        "fortaleciendo la verificación de mensajes y enlaces."
    ),
    ("passwords", "ALTO"): (
        "Necesitas reforzar el uso de contraseñas únicas, extensas y difíciles "
        "de predecir, además de la protección adicional de tus cuentas."
    ),
    ("passwords", "MEDIO"): (
        "Conoces algunas prácticas de protección de cuentas, pero debes mejorar "
        "la creación y administración de contraseñas."
    ),
    ("passwords", "BAJO"): (
        "Demuestras buenas prácticas iniciales para proteger tus cuentas. "
        "Continúa utilizando contraseñas únicas y mecanismos adicionales de "
        "autenticación."
    ),
    ("malware", "ALTO"): (
        "Necesitas reforzar la prevención de malware y evitar conectar "
        "dispositivos USB desconocidos a los equipos de la empresa."
    ),
    ("malware", "MEDIO"): (
        "Identificas algunos riesgos de dispositivos externos, pero debes "
        "fortalecer tu respuesta ante memorias USB desconocidas."
    ),
    ("malware", "BAJO"): (
        "Demuestras buenas prácticas iniciales frente a dispositivos USB y "
        "malware. Continúa aplicando medidas preventivas."
    ),
    ("none", "BAJO"): (
        "Tu diagnóstico inicial no detectó un área crítica. Continúa con la "
        "ruta general de capacitación para comprobar tus conocimientos en "
        "situaciones prácticas."
    ),
}


GENERAL_SURVEY_MESSAGE = (
    "Tu diagnóstico inicial está listo. Continúa con la ruta general de "
    "capacitación para fortalecer tus conocimientos en situaciones prácticas."
)


class AIService:

    @staticmethod
    async def get_user_risk_prediction(
        db: Session,
        user_id: int
    ) -> dict:

        user = (
            db.query(User)
            .filter(User.id == user_id)
            .first()
        )

        if not user:
            raise Exception("User not found")

        analytics = get_user_analytics(
            db,
            user_id
        )

        total_decisions = user.total_decisions or 0

        if total_decisions < MIN_BEHAVIORAL_DECISIONS:
            return AIService._get_survey_based_risk(
                db=db,
                user_id=user_id,
                total_decisions=total_decisions
            )

        features = {
            "total_points": analytics["total_points"],
            "correct_decisions": user.correct_decisions,
            "total_decisions": total_decisions,
            "accuracy": analytics["accuracy"],
            "risk_score": analytics["risk_index"],
            "awareness_score": analytics["awareness_score"],
            "decisions_last_7_days": analytics["decisions_last_7_days"],
            "most_failed_category": analytics["most_failed_category"] or "phishing"
        }

        
        prediction = predictor.predict_risk(features)

        
        recommendation = get_recommendation(
            analytics["most_failed_category"]
        )
        survey_primary_weakness = AIService._get_latest_survey_primary_weakness(
            db,
            user_id
        )
        playable_training = resolve_playable_topic(
            recommendation["training"],
            recommendation["scenario"],
            survey_primary_weakness
        )

        return {
            "user_id": user_id,
            "risk_level": prediction["risk_level"],
            "probability": prediction["probability"],


            "recommended_training":
                playable_training,

            "recommended_scenario":
                recommendation["scenario"],

            "message":
                recommendation["message"],

            "risk_source": "random_forest",
            "behavioral_decisions": total_decisions,
            "min_behavioral_decisions": MIN_BEHAVIORAL_DECISIONS,
            "sufficient_behavioral_data": True
        }

    @staticmethod
    def _get_survey_based_risk(
        db: Session,
        user_id: int,
        total_decisions: int
    ) -> dict:

        submission = (
            db.query(SurveySubmission)
            .filter(
                SurveySubmission.user_id == user_id,
                SurveySubmission.survey_version == DIAGNOSTIC_SURVEY_VERSION
            )
            .first()
        )

        if submission is None:
            raise HTTPException(
                status_code=409,
                detail="Diagnostic survey required before risk evaluation."
            )

        primary_weakness = AIService._normalize_survey_weakness(
            submission.primary_weakness
        )
        risk_level = AIService._normalize_survey_risk(
            submission.initial_risk
        )

        recommendation = SURVEY_RECOMMENDATIONS.get(
            primary_weakness,
            {
                "training": "general",
                "scenario": 1,
            }
        )
        playable_training = resolve_playable_topic(
            recommendation["training"],
            recommendation["scenario"],
            primary_weakness
        )

        return {
            "user_id": user_id,
            "risk_level": risk_level,
            "probability": 0.0,
            "recommended_training": playable_training,
            "recommended_scenario": recommendation["scenario"],
            "message": AIService._get_survey_message(
                primary_weakness,
                risk_level
            ),
            "risk_source": "survey",
            "behavioral_decisions": total_decisions,
            "min_behavioral_decisions": MIN_BEHAVIORAL_DECISIONS,
            "sufficient_behavioral_data": False
        }

    @staticmethod
    def _normalize_survey_weakness(primary_weakness: str) -> str:
        normalized = (primary_weakness or "").strip().lower()

        if normalized in SURVEY_RECOMMENDATIONS:
            return normalized

        return "general"

    @staticmethod
    def _get_latest_survey_primary_weakness(
        db: Session,
        user_id: int
    ) -> str | None:
        submission = (
            db.query(SurveySubmission)
            .filter(
                SurveySubmission.user_id == user_id,
                SurveySubmission.survey_version == DIAGNOSTIC_SURVEY_VERSION
            )
            .first()
        )

        if submission is None:
            return None

        return submission.primary_weakness

    @staticmethod
    def _normalize_survey_risk(risk_level: str) -> str:
        normalized = (risk_level or "").strip().upper()

        if normalized in ("ALTO", "MEDIO", "BAJO"):
            return normalized

        return "NO DISPONIBLE"

    @staticmethod
    def _get_survey_message(
        primary_weakness: str,
        risk_level: str
    ) -> str:
        if primary_weakness == "none":
            return SURVEY_MESSAGES[("none", "BAJO")]

        return SURVEY_MESSAGES.get(
            (primary_weakness, risk_level),
            GENERAL_SURVEY_MESSAGE
        )
        
