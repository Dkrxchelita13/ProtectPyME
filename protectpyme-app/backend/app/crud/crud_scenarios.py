""" from sqlalchemy.orm import Session
from app import models, schemas

def create_scenario(db: Session, scenario: schemas.ScenarioCreate):
    db_scenario = models.Scenario(**scenario.model_dump())
    db.add(db_scenario)
    db.commit()
    db.refresh(db_scenario)
    return db_scenario

def get_scenarios(db: Session):
    return db.query(models.Scenario).all()

def get_scenario(db: Session, scenario_id: int):
    return db.query(models.Scenario).filter(models.Scenario.id == scenario_id).first()

def delete_scenario(db: Session, scenario_id: int):
    scenario = db.query(models.Scenario).filter(models.Scenario.id == scenario_id).first()
    if scenario:
        db.delete(scenario)
        db.commit()
    return scenario
 """
from sqlalchemy.orm import Session
from app import models, schemas

def create_scenario(db: Session, scenario: schemas.ScenarioCreate):
    db_scenario = models.Scenario(**scenario.dict())
    db.add(db_scenario)
    db.commit()
    db.refresh(db_scenario)
    return db_scenario

def get_scenarios(db: Session):
    return db.query(models.Scenario).all()

def get_scenario(db: Session, scenario_id: int):
    return db.query(models.Scenario).filter(
        models.Scenario.id == scenario_id
    ).first()

def delete_scenario(db: Session, scenario_id: int):
    sc = db.query(models.Scenario).filter(
        models.Scenario.id == scenario_id
    ).first()
    if sc:
        db.delete(sc)
        db.commit()
    return sc
