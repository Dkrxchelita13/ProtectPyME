""" from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session
from app.database import get_db
#from app import crud, schemas
from app.crud.crud_users import create_user, get_user
from app import schemas

router = APIRouter()

router = APIRouter(
    prefix="/users",
    tags=["users"]
)
 """

""" @router.post("/users")
def create_user(user: schemas.UserCreate, db: Session = Depends(get_db)):
    return crud.create_user(db, user)

@router.get("/users/{user_id}")
def read_user(user_id: int, db: Session = Depends(get_db)):
    return crud.get_user(db, user_id)
 """
""" @router.post("/users")
def create_user_route(user: schemas.UserCreate, db: Session = Depends(get_db)):
    return create_user(db, user)

@router.get("/users/{user_id}")
def read_user(user_id: int, db: Session = Depends(get_db)):
    return get_user(db, user_id) """
from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from app.database import get_db
import app.schemas as schemas
from app.crud.crud_users import create_user, get_user
from app import models   
from app.models import Badge, UserBadge, User
from app.auth import get_current_user

from app.services.analytics import get_user_analytics

#audit log
from app.services.audit_service import log_event
from fastapi import Request

import logging
logger = logging.getLogger("protectpyme")

router = APIRouter(
    prefix="/users",
    tags=["users"]
)

""" @router.post("/")
def create_user_route(user: schemas.UserCreate, db: Session = Depends(get_db)):
    return create_user(db, user)
 """
@router.post("/", response_model=schemas.UserOut, status_code=201)
def create_user_route(user: schemas.UserCreate, db: Session = Depends(get_db)):
    new_user = create_user(db, user)
    logger.info(f"New user registered: {new_user.email}")
    return new_user
    #return create_user(db, user)
    

""" @router.get("/{user_id}")
def read_user(user_id: int, db: Session = Depends(get_db)):
    return get_user(db, user_id) """
#solo usuario autenticado puede ver su informacion
@router.get("/{user_id}", response_model=schemas.UserOut)
def read_user(
    user_id: int,
    current_user=Depends(get_current_user),
    db: Session = Depends(get_db)
):
    if current_user.id != user_id:
        logger.warning(
            f"User {current_user.id} tried to access user {user_id}"
        )
        raise HTTPException(status_code=403, detail="Not authorized")

    user = get_user(db, user_id)

    if not user:
        raise HTTPException(status_code=404, detail="User not found")

    logger.info(f"User {user_id} profile accessed")

    return user
    # if current_user.id != user_id:
    #     raise HTTPException(status_code=403, detail="Not authorized")

    # return get_user(db, user_id)
@router.get("/me/badges")
def my_badges(user=Depends(get_current_user), db: Session = Depends(get_db)):
    badges = (
        db.query(Badge)
        .join(UserBadge)
        .filter(UserBadge.user_id == user.id)
        .all()
    )

    return badges

@router.get("/me/stats")
def get_my_stats(current_user: models.User = Depends(get_current_user), db: Session = Depends(get_db)):

    accuracy = 0
    if current_user.total_decisions > 0:
        accuracy = round(
            (current_user.correct_decisions / current_user.total_decisions) * 100,
            2
        )

    return {
        "level": current_user.level,
        "total_points": current_user.total_points,
        "risk_score": current_user.risk_score,
        "total_decisions": current_user.total_decisions,
        "correct_decisions": current_user.correct_decisions,
        "accuracy_percentage": accuracy
    }

@router.get("/me/analytics")
def my_analytics(
    db: Session = Depends(get_db),
    current_user: models.User = Depends(get_current_user)
):
    return get_user_analytics(db, current_user.id)

from app.auth import require_admin

@router.get("/admin/all-users")
def get_all_users_admin(
    db: Session = Depends(get_db),
    admin = Depends(require_admin)
):
    return db.query(User).all()

@router.put("/admin/{user_id}/role")
def update_user_role(
    user_id: int,
    new_role: str,
    request: Request,
    db: Session = Depends(get_db),
    admin = Depends(require_admin)
):
    user = db.query(User).filter(User.id == user_id).first()
    logger.info(f"Admin {admin.id} updating role for user {user.email}")

    if not user:
        logger.warning(f"Admin {admin.id} tried to update non-existing user {user_id}")
        raise HTTPException(status_code=404, detail="User not found")

    logger.info(f"Admin {admin.id} changed role of user {user.id} to {new_role}")

    old_role = user.role
    user.role = new_role
    db.commit()

    # 🔥 Audit log
    log_event(
        db=db,
        actor_user_id=admin.id,
        target_user_id=user.id,
        action="CHANGE_ROLE",
        description=f"Changed role from {old_role} to {new_role}",
        ip_address=request.client.host
    )

    return {"message": "Role updated"}

""" @router.put("/admin/{user_id}/role")
def update_user_role(
    user_id: int,
    role_data: schemas.RoleUpdate,
    db: Session = Depends(get_db),
    admin = Depends(require_admin)
):
    user = db.query(User).filter(User.id == user_id).first()

    if not user:
        raise HTTPException(status_code=404, detail="User not found")

    user.role = role_data.role
    db.commit()

    return {"message": "Role updated"} """
""" @router.put("/admin/{user_id}/role")
def update_user_role(
    user_id: int,
    new_role: str,
    db: Session = Depends(get_db),
    admin = Depends(require_admin)
):
    user = db.query(User).filter(User.id == user_id).first()

    if not user:
        raise HTTPException(status_code=404, detail="User not found")

    user.role = new_role
    db.commit()

    return {"message": "Role updated"} """