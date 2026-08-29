import joblib
import os
import logging
import pandas as pd

from app.services.topic_taxonomy import to_rf_category


logger = logging.getLogger("protectpyme")

INFERENCE_FEATURE_COLUMNS = [
    "total_points",
    "correct_decisions",
    "total_decisions",
    "accuracy",
    "risk_score",
    "awareness_score",
    "decisions_last_7_days",
    "failed_category_encoded",
]

class RiskPredictor:
    def __init__(self):
        base_path = "app/ai"
        self.model = joblib.load(os.path.join(base_path, "model.pkl"))
        self.category_map = joblib.load(os.path.join(base_path, "encoder.pkl"))
        self.risk_labels = {0: "BAJO", 1: "MEDIO", 2: "ALTO"}
        self.feature_columns = self._resolve_feature_columns()

    def predict_risk(self, features_dict: dict):
        feature_values = self._build_feature_values(features_dict)
        model_input = self._build_model_input(feature_values)

        pred_class = int(self.model.predict(model_input)[0])
        probabilities = self.model.predict_proba(model_input)[0]

        return {
            "risk_level": self.risk_labels[pred_class],
            "probability": round(float(probabilities[pred_class]), 2)
        }

    def _resolve_feature_columns(self):
        if hasattr(self.model, "feature_names_in_"):
            return list(self.model.feature_names_in_)

        return list(INFERENCE_FEATURE_COLUMNS)

    def _build_feature_values(self, features_dict: dict):
        # Convertir categoría educativa a la taxonomia heredada del modelo.
        category_mapping = to_rf_category(
            features_dict.get("most_failed_category")
        )

        if category_mapping.used_fallback:
            logger.warning(
                "RF category fallback: %s -> %s (%s)",
                category_mapping.original_topic,
                category_mapping.rf_category,
                category_mapping.reason,
            )

        cat_encoded = self.category_map.get(
            category_mapping.rf_category,
            0
        )

        return {
            "total_points": features_dict.get("total_points", 0),
            "correct_decisions": features_dict.get("correct_decisions", 0),
            "total_decisions": features_dict.get("total_decisions", 0),
            "accuracy": features_dict.get("accuracy", 0.0),
            "risk_score": features_dict.get("risk_score", 0.0), # Se mapea internamente desde risk_index
            "awareness_score": features_dict.get("awareness_score", 0.0),
            "decisions_last_7_days": features_dict.get("decisions_last_7_days", 0),
            "failed_category_encoded": cat_encoded,
        }

    def _build_model_input(self, feature_values: dict):
        ordered_values = {
            feature_name: feature_values[feature_name]
            for feature_name in self.feature_columns
        }

        return pd.DataFrame([ordered_values], columns=self.feature_columns)
