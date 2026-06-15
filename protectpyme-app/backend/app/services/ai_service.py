from sqlalchemy.orm import Session



from app.ai.predict import RiskPredictor

from app.ai.rules import get_recommendation

from app.services.analytics import get_user_analytics
from app.models import User

# Singleton
predictor = RiskPredictor()


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

        features = {
            "total_points": analytics["total_points"],
            "correct_decisions": user.correct_decisions,
            "total_decisions": user.total_decisions,
            "accuracy": analytics["accuracy"],
            "risk_score": analytics["risk_index"],
            "awareness_score": analytics["awareness_score"],
            "decisions_last_7_days": analytics["decisions_last_7_days"],
            "most_failed_category": analytics["most_failed_category"] or "phishing"
        }

        
        prediction = predictor.predict_risk(features)

        print(
            "MOST FAILED CATEGORY:",
            analytics["most_failed_category"]
        )
        recommendation = get_recommendation(
            analytics["most_failed_category"]
        )

        return {
            "user_id": user_id,
            "risk_level": prediction["risk_level"],
            "probability": prediction["probability"],

            "recommended_training":
                recommendation["training"],

            "recommended_scenario":
                recommendation["scenario"],

            "message":
                recommendation["message"]
        }