""" from sqlalchemy.orm import Session
from app.database import SessionLocal
from app.models import Scenario


def seed_scenarios():
    db: Session = SessionLocal()

    # Verificar si ya existen escenarios
    existing = db.query(Scenario).first()
    if existing:
        print("Scenarios already seeded")
        db.close()
        return

    scenarios = [

        Scenario(
            title="Phishing Email",
            description="Recibes un correo del 'banco' pidiendo tu contraseña",
            difficulty="easy",
            category="phishing",
            correct_choice="reportar",
            points_correct=10,
            points_incorrect=0
        ),

        Scenario(
            title="USB desconocido",
            description="Encuentras un USB en la oficina y quieres abrirlo",
            difficulty="medium",
            category="malware",
            correct_choice="no_conectar",
            points_correct=10,
            points_incorrect=0
        ),

        Scenario(
            title="Contraseña débil",
            description="Usar '123456' como contraseña en sistemas de la empresa",
            difficulty="easy",
            category="passwords",
            correct_choice="no_usar",
            points_correct=10,
            points_incorrect=0
        ),

        Scenario(
            title="WiFi público",
            description="Conectarte a WiFi público para trabajar",
            difficulty="medium",
            category="network",
            correct_choice="usar_vpn",
            points_correct=10,
            points_incorrect=0
        ),
    ]

    db.add_all(scenarios)
    db.commit()
    db.close()

    print("Seed scenarios created")

badges = [
    {"name": "Primeros pasos", "description": "Primera decisión tomada"},
    {"name": "Aprendiz rápido", "description": "5 correctas seguidas"},
    {"name": "Experto phishing", "description": "10 phishing correctos"},
]

for b in badges:
    if not db.query(Badge).filter_by(name=b["name"]).first():
        db.add(Badge(**b))

db.commit()
 """
from sqlalchemy.orm import Session
from app.database import SessionLocal
from app.models import Scenario, Badge


SCENARIOS = [
    {
        "id": 1,
        "title": "Phishing Email",
        "description": "Recibes un correo del 'banco' pidiendo tu contraseña",
        "difficulty": "easy",
        "category": "phishing",
        "correct_choice": "reportar_phishing",
        "points_correct": 10,
        "points_incorrect": 0,
    },
    {
        "id": 2,
        "title": "Contraseña débil",
        "description": "Usar '123456' como contraseña en sistemas de la empresa",
        "difficulty": "easy",
        "category": "passwords",
        "correct_choice": "cambiar_password",
        "points_correct": 10,
        "points_incorrect": 0,
    },
    {
        "id": 3,
        "title": "USB desconocido",
        "description": "Encuentras un USB en la oficina y quieres abrirlo",
        "difficulty": "medium",
        "category": "malware",
        "correct_choice": "no_conectar",
        "points_correct": 10,
        "points_incorrect": 0,
    },
    {
        "id": 4,
        "title": "WiFi público",
        "description": "Conectarte a WiFi público para trabajar",
        "difficulty": "medium",
        "category": "network",
        "correct_choice": "usar_vpn",
        "points_correct": 10,
        "points_incorrect": 0,
    },
    {
        "id": 5,
        "title": "Portal falso de proveedores",
        "description": "Detectar un portal falso que intenta robar credenciales",
        "difficulty": "medium",
        "category": "phishing",
        "correct_choice": "reportar_phishing",
        "points_correct": 10,
        "points_incorrect": 0,
    },
    {
        "id": 6,
        "title": "Llamada sospechosa solicitando contraseña",
        "description": "Rechazar una llamada que solicita compartir la contraseña",
        "difficulty": "medium",
        "category": "passwords",
        "correct_choice": "rechazar_reportar_ti",
        "points_correct": 10,
        "points_incorrect": 0,
    },
    {
        "id": 7,
        "title": "Exfiltración de datos en red",
        "description": "Revisar y bloquear tráfico sospechoso de salida",
        "difficulty": "medium",
        "category": "wifi",
        "correct_choice": "revisar_bloquear",
        "points_correct": 10,
        "points_incorrect": 0,
    },
]


def seed_scenarios():
    db: Session = SessionLocal()

    try:
        scenarios_created = 0

        for scenario_data in SCENARIOS:
            existing = db.query(Scenario).filter(Scenario.id == scenario_data["id"]).first()
            if existing:
                continue

            db.add(Scenario(**scenario_data))
            scenarios_created += 1

        if scenarios_created:
            db.commit()
            print(f"Seed scenarios created: {scenarios_created}")
        else:
            print("Scenarios already seeded")

        # -------- BADGES --------

        badges = [
            {"name": "Primeros pasos", "description": "Primera decisión tomada"},
            {"name": "Aprendiz rápido", "description": "5 correctas seguidas"},
            {"name": "Experto phishing", "description": "10 phishing correctos"},
        ]

        for b in badges:
            existing_badge = db.query(Badge).filter_by(name=b["name"]).first()
            if not existing_badge:
                db.add(Badge(**b))

        db.commit()
        print("Badges seeded")

    finally:
        db.close()
