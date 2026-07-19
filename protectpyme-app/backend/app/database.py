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
""" from sqlalchemy import create_engine
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
logger.info("Database connection established") """
import logging
import os
import time

from sqlalchemy import create_engine, text
from sqlalchemy.orm import declarative_base, sessionmaker

logger = logging.getLogger("protectpyme")

DATABASE_URL = os.getenv("DATABASE_URL")

if not DATABASE_URL:
    raise RuntimeError("DATABASE_URL no está configurada")

engine = create_engine(
    DATABASE_URL,
    pool_pre_ping=True,
    pool_recycle=300,
    connect_args={
        "connect_timeout": 10,
    },
)

# Intentar la conexión hasta 10 veces.
for attempt in range(1, 11):
    try:
        with engine.connect() as connection:
            connection.execute(text("SELECT 1"))

        logger.info("Database connection established")
        break

    except Exception as error:
        logger.warning(
            "Waiting for DB connection... intento %s/10: %s",
            attempt,
            str(error),
        )

        if attempt == 10:
            raise RuntimeError(
                "No fue posible establecer conexión con PostgreSQL"
            ) from error

        time.sleep(3)

SessionLocal = sessionmaker(
    autocommit=False,
    autoflush=False,
    bind=engine,
)

Base = declarative_base()


def get_db():
    db = SessionLocal()

    try:
        yield db
    finally:
        db.close()