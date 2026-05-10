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