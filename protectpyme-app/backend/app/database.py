""" import os
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

DATABASE_URL = os.getenv(
    "DATABASE_URL",
    "postgresql://admin:admin@localhost:5432/protectpyme"
)

engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(bind=engine)
 """


""" import os
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

DATABASE_URL = os.getenv(
    "DATABASE_URL",
    "postgresql://admin:admin@localhost:5432/protectpyme"
)

engine = create_engine(DATABASE_URL)

SessionLocal = sessionmaker(
    autocommit=False,
    autoflush=False,
    bind=engine
)

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
 """

""" from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, declarative_base

#DATABASE_URL = "postgresql://postgres:postgres@protectpyme_db:5432/protectpyme"
DATABASE_URL = "postgresql://postgres:postgres@protectpyme_db:5432/protectpyme"

#engine = create_engine(DATABASE_URL)
engine = create_engine(
    DATABASE_URL,
    pool_pre_ping=True
)

SessionLocal = sessionmaker(
    autocommit=False,
    autoflush=False,
    bind=engine
)


Base = declarative_base()

# Dependency
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
 """
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, declarative_base
import time

#DATABASE_URL = "postgresql://admin:admin@db:5432/protectpyme"
import os

import logging
logger = logging.getLogger("protectpyme")

DATABASE_URL = os.getenv("DATABASE_URL")
if not DATABASE_URL:
    raise RuntimeError("DATABASE_URL not configured")

for _ in range(10):
    try:
        engine = create_engine(DATABASE_URL, pool_pre_ping=True)
        engine.connect()
        break
    except Exception as e:
        logger.warning(f"Waiting for DB connection... {str(e)}")
        time.sleep(3)
    # except Exception:
    #     logger.info("Waiting for DB connection...")
    #     time.sleep(3)
    

SessionLocal = sessionmaker(
    autocommit=False,
    autoflush=False,
    bind=engine
)

Base = declarative_base()

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
logger.info("Database connection established")