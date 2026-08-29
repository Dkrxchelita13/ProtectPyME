import sys
import types

from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

TestBase = declarative_base()
TestBase.__test__ = False
engine = create_engine(
    "sqlite://",
    connect_args={"check_same_thread": False},
    poolclass=StaticPool,
)
TestingSessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)


def get_test_db():
    db = TestingSessionLocal()

    try:
        yield db
    finally:
        db.close()


fake_database = types.ModuleType("app.database")
fake_database.Base = TestBase
fake_database.engine = engine
fake_database.SessionLocal = TestingSessionLocal
fake_database.get_db = get_test_db
sys.modules["app.database"] = fake_database

def test_seed_scenarios_adds_missing_new_scenarios_without_touching_existing(monkeypatch):
    from app import models
    from app import seed

    models.Base.metadata.create_all(bind=engine)

    db = TestingSessionLocal()
    try:
        for scenario_id in range(1, 5):
            db.add(
                models.Scenario(
                    id=scenario_id,
                    title=f"Existing {scenario_id}",
                    description="Existing scenario",
                    difficulty="easy",
                    category="legacy",
                    correct_choice="legacy_choice",
                    points_correct=1,
                    points_incorrect=0,
                )
            )
        db.commit()
    finally:
        db.close()

    monkeypatch.setattr(seed, "SessionLocal", TestingSessionLocal)

    seed.seed_scenarios()

    db = TestingSessionLocal()
    try:
        scenarios = db.query(models.Scenario).order_by(models.Scenario.id).all()
        assert [scenario.id for scenario in scenarios] == [1, 2, 3, 4, 5, 6, 7]
        assert db.query(models.Scenario).filter_by(id=1).one().title == "Existing 1"
        assert db.query(models.Scenario).filter_by(id=5).one().category == "phishing"
        assert db.query(models.Scenario).filter_by(id=6).one().category == "passwords"
        assert db.query(models.Scenario).filter_by(id=6).one().correct_choice == "rechazar_reportar_ti"
        assert db.query(models.Scenario).filter_by(id=7).one().category == "wifi"
        assert db.query(models.Scenario).filter_by(id=7).one().correct_choice == "revisar_bloquear"
    finally:
        db.close()

    seed.seed_scenarios()

    db = TestingSessionLocal()
    try:
        assert db.query(models.Scenario).count() == 7
        assert db.query(models.Scenario).filter_by(id=5).count() == 1
        assert db.query(models.Scenario).filter_by(id=6).count() == 1
        assert db.query(models.Scenario).filter_by(id=7).count() == 1
    finally:
        db.close()
        models.Base.metadata.drop_all(bind=engine)
