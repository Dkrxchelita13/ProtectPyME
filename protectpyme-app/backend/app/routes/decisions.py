""" from fastapi import APIRouter

router = APIRouter(prefix="/decisions", tags=["decisions"])

@router.post("/")
def save_decision():
    return {"msg": "decision saved"}

class Decision(BaseModel ):
    user_id: int
    scenario_id: int
    choice: str

@router.get("/")
def root():
    return {"message": "ProtectPYME API running"}

@router.post("/decision")
def save_decision(decision: Decision):
    return { "status": "saved", "data": decision} """

""" from fastapi import APIRouter
from pydantic import BaseModel
from fastapi import Depends
from sqlalchemy.orm import Session
from app.database import get_db
from app.crud import crud_decisions
from app.schemas import DecisionCreate

router = APIRouter(
    prefix="/decisions",
    tags=["decisions"]
)

# Schema local (puedes moverlo luego a schemas.py)
class Decision(BaseModel):
    user_id: int
    scenario_id: int
    choice: str

@router.post("/")
def create_decision(decision: Decision):
    return {
        "status": "saved",
        "data": decision
    }

@router.get("/")
def list_decisions():
    return {"msg": "list of decisions"}
 """
from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from app.database import get_db
from app.crud import crud_decisions
from app.schemas import DecisionCreate


from app.auth import get_current_user

from app import models


from app.schemas import DecisionOut

from app.services.audit_service import log_event
from fastapi import Request

import logging
logger = logging.getLogger("protectpyme")

router = APIRouter(
    prefix="/decisions",
    tags=["decisions"]
)

""" @router.post("/")
def create_decision(
    decision: DecisionCreate,
    db: Session = Depends(get_db)
):
    return crud_decisions.create_decision(db, decision) """

""" def create_decision(
    decision: DecisionCreate,
    db: Session = Depends(get_db),
    user_id: int = Depends(get_current_user)
):
    return create_decision(db, decision) """
#preoteccion con jwt
""" @router.post("/")
def create_decision(
    decision: DecisionCreate,
    db: Session = Depends(get_db),
    current_user = Depends(get_current_user)
):
    return crud_decisions.create_decision(
        db,
        decision,
        current_user.id
    )
 """
""" @router.post("/")
def create_decision(
    decision: DecisionCreate,
    db: Session = Depends(get_db),
    user_id: int = Depends(get_current_user)
):
    return crud_decisions.create_decision(
        db,
        decision,
        user_id
    ) """
@router.post("/", response_model=DecisionOut)
def create_decision(
    decision: DecisionCreate,
    request: Request,
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    new_decision = crud_decisions.create_decision(
        db,
        decision,
        current_user
    )

    log_event(
        db=db,
        actor_user_id=current_user.id,
        target_user_id=current_user.id,
        action="CREATE_DECISION",
        description=f"Scenario {decision.scenario_id}",
        ip_address=request.client.host
    )

    return new_decision
""" @router.get("/")
def list_decisions(db: Session = Depends(get_db)):
    return crud_decisions.get_decisions(db)
 """

""" @router.get("/me", response_model=list[DecisionOut])
def list_my_decisions(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    return crud_decisions.get_user_decisions(db, current_user.id) """
@router.get("/me", response_model=list[DecisionOut])
def list_my_decisions(
    skip: int = 0,
    limit: int = 20,
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):

    logger.info(f"User {current_user.id} requested decision history")

    return crud_decisions.get_user_decisions(db, current_user.id, skip, limit)