import joblib
import numpy as np
import os
import logging

from app.services.topic_taxonomy import to_rf_category


logger = logging.getLogger("protectpyme")

class RiskPredictor:
    def __init__(self):
        base_path = "app/ai"
        self.model = joblib.load(os.path.join(base_path, "model.pkl"))
        self.category_map = joblib.load(os.path.join(base_path, "encoder.pkl"))
        self.risk_labels = {0: "BAJO", 1: "MEDIO", 2: "ALTO"}

    def predict_risk(self, features_dict: dict):
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
        
        # Construcción del vector en el orden estricto requerido
        vector = [
            features_dict.get("total_points", 0),
            features_dict.get("correct_decisions", 0),
            features_dict.get("total_decisions", 0),
            features_dict.get("accuracy", 0.0),
            features_dict.get("risk_score", 0.0), # Se mapea internamente desde risk_index
            features_dict.get("awareness_score", 0.0),
            features_dict.get("decisions_last_7_days", 0),
            cat_encoded
        ]
        
        # Predicción
        input_array = np.array([vector])
        pred_class = int(self.model.predict(input_array)[0])
        probabilities = self.model.predict_proba(input_array)[0]
        
        return {
            "risk_level": self.risk_labels[pred_class],
            "probability": round(float(probabilities[pred_class]), 2)
        }
