import joblib
import numpy as np
import os

class RiskPredictor:
    def __init__(self):
        base_path = "app/ai"
        self.model = joblib.load(os.path.join(base_path, "model.pkl"))
        self.category_map = joblib.load(os.path.join(base_path, "encoder.pkl"))
        self.risk_labels = {0: "BAJO", 1: "MEDIO", 2: "ALTO"}

    def predict_risk(self, features_dict: dict):
        # Convertir categoría de string a número seguro
        cat_str = features_dict.get("most_failed_category", "phishing")
        cat_encoded = self.category_map.get(cat_str, 0)
        
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