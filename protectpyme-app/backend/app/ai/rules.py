def evaluate_decision(scenario, choice):

    is_correct = choice == scenario.correct_choice

    multiplier = {
        "easy": 1,
        "medium": 2,
        "hard": 3
    }

    base_points = (
        scenario.points_correct
        if is_correct
        else scenario.points_incorrect
    )

    points_awarded = base_points * multiplier.get(
        scenario.difficulty, 1
    )

    # Nivel de riesgo educativo
    if is_correct:
        risk_level = "low"
        feedback = "Excelente decisión. Aplicaste una buena práctica de ciberseguridad."
    else:
        if scenario.difficulty == "hard":
            risk_level = "high"
        else:
            risk_level = "medium"

        feedback = (
            "Esta acción puede generar una vulnerabilidad "
            "que comprometa la seguridad de la empresa."
        )

    return {
        "is_correct": is_correct,
        "points_awarded": points_awarded,
        "risk_level": risk_level,
        "feedback": feedback
    }
    
    
def get_recommendation(category):

    category = (category or "").lower()

    recommendations = {

        "phishing": {
            "training": "phishing",
            "scenario": 1,
            "message": "Practica detección de correos fraudulentos"
        },

        "password": {
            "training": "passwords",
            "scenario": 2,
            "message": "Refuerza buenas prácticas de contraseñas"
        },

        "passwords": {
            "training": "passwords",
            "scenario": 2,
            "message": "Refuerza buenas prácticas de contraseñas"
        },

        "wifi": {
            "training": "wifi",
            "scenario": 3,
            "message": "Evita redes públicas inseguras"
        },

        "social_engineering": {
            "training": "social_engineering",
            "scenario": 1,
            "message": "Fortalece tus conocimientos sobre ingeniería social"
        }
    }

    return recommendations.get(
        category,
        {
            "training": "general",
            "scenario": 1,
            "message": "Excelente desempeño. No se detectaron áreas críticas de mejora."
        }
    )