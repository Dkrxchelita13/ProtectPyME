from datetime import datetime, timedelta
from jose import JWTError, jwt
from passlib.context import CryptContext

from fastapi import Depends, HTTPException
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from sqlalchemy.orm import Session

from fastapi import HTTPException, status, Depends

from app.database import get_db
from app.models import User

#SECRET_KEY = "supersecretkey"
import os

import logging
logger = logging.getLogger("protectpyme")

#SECRET_KEY = os.getenv("SECRET_KEY", "devsecret")
SECRET_KEY = os.getenv("SECRET_KEY")
if not SECRET_KEY:
    raise RuntimeError("SECRET_KEY not configured")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 60

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")

# Hash password
def hash_password(password: str):
    return pwd_context.hash(password)

# Verify password
def verify_password(plain, hashed):
    return pwd_context.verify(plain, hashed)


def get_google_client_ids():
    raw_client_ids = os.getenv("GOOGLE_CLIENT_IDS") or os.getenv("GOOGLE_CLIENT_ID")

    if not raw_client_ids:
        logger.error("GOOGLE_CLIENT_IDS is not configured")
        raise HTTPException(status_code=500, detail="Google OAuth is not configured")

    client_ids = [
        client_id.strip()
        for client_id in raw_client_ids.split(",")
        if client_id.strip()
    ]

    if not client_ids:
        logger.error("GOOGLE_CLIENT_IDS is empty")
        raise HTTPException(status_code=500, detail="Google OAuth is not configured")

    return client_ids


def verify_google_id_token(token: str):
    try:
        from google.auth.transport import requests as google_requests
        from google.oauth2 import id_token as google_id_token
    except ImportError:
        logger.error("google-auth dependency is not installed")
        raise HTTPException(status_code=500, detail="Google OAuth dependency is missing")

    last_error = None
    request = google_requests.Request()

    for client_id in get_google_client_ids():
        try:
            payload = google_id_token.verify_oauth2_token(
                token,
                request,
                client_id
            )

            issuer = payload.get("iss")
            if issuer not in ("accounts.google.com", "https://accounts.google.com"):
                logger.warning("Invalid Google token issuer: %s", issuer)
                raise HTTPException(status_code=401, detail="Invalid Google token")

            if payload.get("email_verified") is not True:
                logger.warning("Google login rejected because email is not verified")
                raise HTTPException(status_code=401, detail="Google email is not verified")

            return payload

        except ValueError as exc:
            last_error = exc

    logger.warning("Invalid Google token: %s", last_error)
    raise HTTPException(status_code=401, detail="Invalid Google token")

# Create token
def create_access_token(data: dict):
    to_encode = data.copy()
    expire = datetime.utcnow() + timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
    to_encode.update({"exp": expire,
    "iat": datetime.utcnow()})
    return jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)


security = HTTPBearer()

def get_current_user(
    credentials: HTTPAuthorizationCredentials = Depends(security),
    db: Session = Depends(get_db)
):
    token = credentials.credentials

    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        user_id = payload.get("sub")

        if user_id is None:
            logger.warning("JWT without sub field")
            raise HTTPException(status_code=401, detail="Invalid token")

    except JWTError:
        logger.warning("Invalid JWT token attempt")
        raise HTTPException(status_code=401, detail="Invalid token")
    user = db.query(User).filter(User.id == int(user_id)).first()

    if user is None:
        #raise HTTPException(status_code=404, detail="User not found")
        raise HTTPException(status_code=401, detail="Invalid credentials")

    return user


def require_admin(current_user = Depends(get_current_user)):
    if current_user.role != "admin":
        logger.warning(f"Unauthorized admin access attempt by user {current_user.id}")
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Admin privileges required"
    )
    return current_user
