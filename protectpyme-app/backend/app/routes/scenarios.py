from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from app.database import get_db
from app.schemas import Scenario, ScenarioCreate
from app.crud.crud_scenarios import (
    create_scenario,
    get_scenarios,
    get_scenario,
    delete_scenario,
)

from app.auth import get_current_user, require_admin
from app import models

router = APIRouter(prefix="/scenarios", tags=["scenarios"])


#  Solo admin puede crear escenarios
@router.post("/", response_model=Scenario)
def create(
    scenario: ScenarioCreate,
    db: Session = Depends(get_db),
    admin: models.User = Depends(require_admin)
):
    return create_scenario(db, scenario)


#  Usuario autenticado puede ver todos
@router.get("/", response_model=list[Scenario])
def read_all(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    return get_scenarios(db)


#  Usuario autenticado puede ver uno
@router.get("/{scenario_id}", response_model=Scenario)
def read_one(
    scenario_id: int,
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    sc = get_scenario(db, scenario_id)

    if not sc:
        raise HTTPException(status_code=404, detail="Scenario not found")

    return sc


#  Solo admin puede eliminar
@router.delete("/{scenario_id}")
def delete(
    scenario_id: int,
    db: Session = Depends(get_db),
    admin: models.User = Depends(require_admin)
):
    sc = delete_scenario(db, scenario_id)

    if not sc:
        raise HTTPException(status_code=404, detail="Scenario not found")

    return {"message": "Deleted successfully"}