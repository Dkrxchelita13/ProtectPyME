""" from fastapi import FastAPI 
from pydantic import BaseModel

from routes import users

from routes import decisions

from app.database import engine
from app.models import Base

app = FastAPI()

# Crear tablas automáticamente
Base.metadata.create_all(bind=engine)


app.include_router(users.router)


app.include_router(decisions.router)

 """
from dotenv import load_dotenv
load_dotenv()

from app.routes import minigames

from app.routes import ai


from app.routes import users, decisions, scenarios, auth, leaderboard, admin
from app.database import engine
from app.models import Base
from app.seed import seed_scenarios

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from fastapi.exceptions import RequestValidationError
from fastapi.middleware.cors import CORSMiddleware

from starlette.exceptions import HTTPException as StarletteHTTPException

from datetime import datetime
import time
import logging
logger = logging.getLogger("protectpyme")

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s | %(levelname)s | %(name)s | %(message)s"
)

app = FastAPI()


origins = [
    "*"
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.on_event("startup")
def startup():
    Base.metadata.create_all(bind=engine)
    seed_scenarios()
    logger.info("Application startup complete")

app.include_router(users.router)
app.include_router(decisions.router)
app.include_router(scenarios.router)
app.include_router(auth.router)
app.include_router(leaderboard.router)
app.include_router(admin.router)
app.include_router(minigames.router)
app.include_router(ai.router)


@app.get("/")
def root():
    return {"message": "ProtectPYME API running"}


#Middleware performance
@app.middleware("http")
async def add_process_time_header(request: Request, call_next):
    start_time = time.time()

    response = await call_next(request)

    process_time = round(time.time() - start_time, 4)
    response.headers["X-Process-Time"] = str(process_time)

    logger.info(f"{request.method} {request.url.path} completed in {process_time}s")

    return response
#Health check real

@app.get("/health")
def health():
    logger.info("Health check endpoint called")
    return {
        "status": "ok",
        "timestamp": datetime.utcnow()
    }

# excepcionesManejo Global de Excepciones




@app.exception_handler(StarletteHTTPException)
async def http_exception_handler(request: Request, exc: StarletteHTTPException):
    logger.warning(f"HTTP error: {exc.detail}")
    return JSONResponse(
        status_code=exc.status_code,
        content={
            "success": False,
            "error": exc.detail
        }
    )


@app.exception_handler(RequestValidationError)
async def validation_exception_handler(request: Request, exc: RequestValidationError):
    logger.warning(f"Validation error: {exc.errors()}")
    return JSONResponse(
        status_code=422,
        content={
            "success": False,
            "error": "Validation error",
            "details": exc.errors()
        }
    )


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    logger.error(f"Unhandled error: {str(exc)}")
    return JSONResponse(
        status_code=500,
        content={
            "success": False,
            "error": "Internal server error"
        }
    )

