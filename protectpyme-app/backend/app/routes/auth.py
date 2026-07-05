""" from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from app.database import get_db
from app.models import User
from app.auth import verify_password, create_access_token

router = APIRouter(tags=["auth"])

@router.post("/login")
def login(email: str, password: str, db: Session = Depends(get_db)):
    user = db.query(User).filter(User.email == email).first()

    if not user or not verify_password(password, user.password):
        raise HTTPException(status_code=401, detail="Invalid credentials")

    token = create_access_token({"sub": str(user.id)})

    return {"access_token": token, "token_type": "bearer"}
 """
from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
from pydantic import BaseModel
#from fastapi.security import OAuth2PasswordBearer
from jose import JWTError, jwt

from app.database import get_db
from app import schemas
from app.models import User
from app.auth import (
    verify_password,
    create_access_token,
    verify_google_id_token,
    SECRET_KEY,
    ALGORITHM
)

import logging
logger = logging.getLogger("protectpyme")

router = APIRouter(tags=["auth"])

#oauth2_scheme = OAuth2PasswordBearer(tokenUrl="login")

from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials

security = HTTPBearer()

class LoginData(BaseModel):
    email: str
    password: str


@router.post("/login")
def login(data: LoginData, db: Session = Depends(get_db)):
    user = db.query(User).filter(User.email == data.email).first()

    if not user or not user.password or not verify_password(data.password, user.password):
        logger.warning(f"Failed login attempt for email {data.email}")
        raise HTTPException(status_code=401, detail="Invalid credentials")

    logger.info(f"User {user.id} logged in successfully")

    token = create_access_token({"sub": str(user.id)})
    return {"access_token": token, "token_type": "bearer"}


@router.post("/auth/google")
def google_login(data: schemas.GoogleLoginRequest, db: Session = Depends(get_db)):
    payload = verify_google_id_token(data.id_token)

    google_sub = payload.get("sub")
    email = (payload.get("email") or "").strip().lower()
    name = (payload.get("name") or "").strip()

    if not google_sub or not email:
        logger.warning("Google token missing required subject or email")
        raise HTTPException(status_code=401, detail="Invalid Google token")

    user = db.query(User).filter(User.google_sub == google_sub).first()

    if user is None:
        user = db.query(User).filter(User.email == email).first()

        if user is not None:
            if user.google_sub and user.google_sub != google_sub:
                logger.warning("Google account mismatch for email %s", email)
                raise HTTPException(status_code=409, detail="Email already linked to another Google account")

            user.google_sub = google_sub
            if not user.auth_provider:
                user.auth_provider = "local"
        else:
            user = User(
                name=name or email.split("@")[0],
                email=email,
                password=None,
                auth_provider="google",
                google_sub=google_sub
            )
            db.add(user)

    db.commit()
    db.refresh(user)

    logger.info(f"User {user.id} logged in with Google")

    token = create_access_token({"sub": str(user.id)})
    return {"access_token": token, "token_type": "bearer"}


""" def get_current_user(token: str = Depends(oauth2_scheme), db: Session = Depends(get_db)):
    credentials_exception = HTTPException(
        status_code=401,
        detail="Could not validate credentials"
    )

    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        user_id: str = payload.get("sub")

        if user_id is None:
            raise credentials_exception

    except JWTError:
        raise credentials_exception

    user = db.query(User).filter(User.id == int(user_id)).first()

    if user is None:
        raise credentials_exception

    return user """

def get_current_user(credentials: HTTPAuthorizationCredentials = Depends(security), db: Session = Depends(get_db)):
    credentials_exception = HTTPException(
        status_code=401,
        detail="Could not validate credentials"
    )

    token = credentials.credentials

    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        user_id: str = payload.get("sub")

        if user_id is None:
            raise credentials_exception

    except JWTError:
        raise credentials_exception

    user = db.query(User).filter(User.id == int(user_id)).first()

    if user is None:
        raise credentials_exception

    return user
