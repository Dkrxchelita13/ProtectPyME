import warnings

import numpy as np
import pandas as pd
import pytest

from app.ai.predict import INFERENCE_FEATURE_COLUMNS, RiskPredictor


class CapturingModel:
    def __init__(self):
        self.feature_names_in_ = np.array(INFERENCE_FEATURE_COLUMNS)
        self.predict_inputs = []
        self.predict_proba_inputs = []

    def predict(self, model_input):
        self.predict_inputs.append(model_input)
        return np.array([1])

    def predict_proba(self, model_input):
        self.predict_proba_inputs.append(model_input)
        return np.array([[0.1, 0.8, 0.1]])


def make_predictor(model=None):
    predictor = RiskPredictor.__new__(RiskPredictor)
    predictor.model = model or CapturingModel()
    predictor.category_map = {
        "phishing": 0,
        "password": 1,
        "wifi": 2,
        "social_engineering": 3,
    }
    predictor.risk_labels = {0: "BAJO", 1: "MEDIO", 2: "ALTO"}
    predictor.feature_columns = predictor._resolve_feature_columns()

    return predictor


def feature_payload(category):
    return {
        "total_points": 120,
        "correct_decisions": 8,
        "total_decisions": 10,
        "accuracy": 80.0,
        "risk_score": 20.0,
        "awareness_score": 70.0,
        "decisions_last_7_days": 4,
        "most_failed_category": category,
    }


@pytest.mark.parametrize(
    ("category", "encoded_category"),
    [
        ("phishing", 0),
        ("passwords", 1),
        ("wifi", 2),
        ("malware", 0),
    ],
)
def test_predictor_delivers_dataframe_with_feature_names(category, encoded_category):
    model = CapturingModel()
    predictor = make_predictor(model)

    result = predictor.predict_risk(feature_payload(category))

    assert result == {"risk_level": "MEDIO", "probability": 0.8}
    assert len(model.predict_inputs) == 1
    assert len(model.predict_proba_inputs) == 1

    model_input = model.predict_inputs[0]
    assert isinstance(model_input, pd.DataFrame)
    assert list(model_input.columns) == INFERENCE_FEATURE_COLUMNS
    assert model_input.shape == (1, len(INFERENCE_FEATURE_COLUMNS))
    assert model_input.iloc[0].tolist() == [
        120,
        8,
        10,
        80.0,
        20.0,
        70.0,
        4,
        encoded_category,
    ]
    pd.testing.assert_frame_equal(model_input, model.predict_proba_inputs[0])


def test_model_feature_names_match_training_order():
    predictor = RiskPredictor()

    assert list(predictor.model.feature_names_in_) == INFERENCE_FEATURE_COLUMNS
    assert predictor.feature_columns == INFERENCE_FEATURE_COLUMNS


@pytest.mark.parametrize("category", ["phishing", "passwords", "wifi"])
def test_dataframe_prediction_matches_legacy_array_for_supported_categories(category):
    predictor = RiskPredictor()
    feature_values = predictor._build_feature_values(feature_payload(category))
    model_input = predictor._build_model_input(feature_values)
    legacy_input = np.array([
        [feature_values[feature_name] for feature_name in predictor.feature_columns]
    ])

    with warnings.catch_warnings():
        warnings.simplefilter("ignore", UserWarning)
        legacy_prediction = predictor.model.predict(legacy_input)
        legacy_proba = predictor.model.predict_proba(legacy_input)

    dataframe_prediction = predictor.model.predict(model_input)
    dataframe_proba = predictor.model.predict_proba(model_input)

    np.testing.assert_array_equal(dataframe_prediction, legacy_prediction)
    np.testing.assert_array_equal(dataframe_proba, legacy_proba)


def test_predict_risk_does_not_emit_missing_feature_names_warning():
    predictor = RiskPredictor()

    with warnings.catch_warnings(record=True) as recorded_warnings:
        warnings.simplefilter("always")
        predictor.predict_risk(feature_payload("wifi"))

    missing_feature_name_warnings = [
        warning
        for warning in recorded_warnings
        if "does not have valid feature names" in str(warning.message)
    ]

    assert missing_feature_name_warnings == []
