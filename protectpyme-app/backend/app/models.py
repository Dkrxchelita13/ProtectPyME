#from datetime import datetime
#from app.database import Base

#Base = declarative_base()


from datetime import datetime

from sqlalchemy import Column, String


from sqlalchemy import (
    Boolean,
    Column,
    Integer,
    JSON,
    String,
    Text,
    ForeignKey,
    TIMESTAMP,
    DateTime,
    Float,
    UniqueConstraint,
    func
)
from sqlalchemy.orm import relationship

from app.database import Base


# -------- USERS --------
class User(Base):
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100))
    email = Column(String(100), unique=True, index=True)
    password = Column(String(255), nullable=True)
    auth_provider = Column(String(30), default="local")
    google_sub = Column(String(255), unique=True, index=True, nullable=True)
    created_at = Column(TIMESTAMP, default=datetime.utcnow)

    total_points = Column(Integer, default=0)

    # 🔥 NUEVO
    level = Column(String(20), default="Bronze")
    risk_score = Column(Integer, default=0)
    total_decisions = Column(Integer, default=0)
    correct_decisions = Column(Integer, default=0)

    role = Column(String, default="user")
# -------- SCENARIOS --------
class Scenario(Base):
    __tablename__ = "scenarios"



    id = Column(Integer, primary_key=True, index=True)
    title = Column(String, nullable=False)
    description = Column(Text)

    difficulty = Column(String, default="easy")
    category = Column(String(50))

    correct_choice = Column(String(100))

    points_correct = Column(Integer, default=10)
    points_incorrect = Column(Integer, default=0)

"""     description = Column(Text, nullable=True)
    difficulty = Column(String, default="easy")  # easy, medium, hard

    correct_choice = Column(String(100))  # opcion correcta
    points = Column(Integer, default=10)  # puntos """
"""     id = Column(Integer, primary_key=True, index=True)
    title = Column(String(150))
    description = Column(String)
    risk_level = Column(String(50)) """

# -------- DECISIONS --------
class Decision(Base):
    __tablename__ = "decisions"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"))
    scenario_id = Column(Integer, ForeignKey("scenarios.id"))
    choice = Column(String(100))
    #risk_result = Column(String(50))

    is_correct = Column(Integer, default=0)  # (0 = no, 1 = sí)


    points_awarded = Column(Integer, default=0)
    
    risk_level = Column(String(50))
    feedback = Column(Text)
    response_time = Column(Integer, nullable=True)

    #created_at = Column(TIMESTAMP)
    created_at = Column(TIMESTAMP, default=datetime.utcnow)

    #created_at = Column(TIMESTAMP, default=datetime.utcnow)



""" 
# -------- SCORES --------
class Score(Base):
    __tablename__ = "scores"
    
    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"))
    scenario_id = Column(Integer, ForeignKey("scenarios.id"))
    points = Column(Integer, default=0)


 """





"""     id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"))
    total_points = Column(Integer, default=0)
    level = Column(String(50))
 """

class Badge(Base):
    __tablename__ = "badges"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String, unique=True)
    description = Column(String)
    icon = Column(String, nullable=True)


class UserBadge(Base):
    __tablename__ = "user_badges"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"))
    badge_id = Column(Integer, ForeignKey("badges.id"))
    earned_at = Column(DateTime, default=datetime.utcnow)

class UserCategoryPoints(Base):
    __tablename__ = "user_category_points"

    user_id = Column(Integer, ForeignKey("users.id"), primary_key=True)
    category = Column(String, primary_key=True)
    total_points = Column(Integer, default=0)


# -------- DIAGNOSTIC SURVEY --------
class SurveySubmission(Base):
    __tablename__ = "survey_submissions"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, index=True)
    survey_version = Column(String(50), nullable=False)
    primary_weakness = Column(String(50), nullable=False)
    initial_risk = Column(String(10), nullable=False)
    phishing_score = Column(Integer, nullable=False)
    passwords_score = Column(Integer, nullable=False)
    malware_score = Column(Integer, nullable=False)
    phishing_risk_score = Column(Integer, nullable=False)
    passwords_risk_score = Column(Integer, nullable=False)
    malware_risk_score = Column(Integer, nullable=False)
    total_risk_score = Column(Integer, nullable=False)
    created_at = Column(
        DateTime(timezone=True),
        nullable=False,
        server_default=func.now()
    )

    answers = relationship(
        "SurveyAnswer",
        back_populates="submission",
        cascade="all, delete-orphan"
    )

    __table_args__ = (
        UniqueConstraint(
            "user_id",
            "survey_version",
            name="uq_survey_submissions_user_version"
        ),
    )


class SurveyAnswer(Base):
    __tablename__ = "survey_answers"

    id = Column(Integer, primary_key=True, index=True)
    submission_id = Column(
        Integer,
        ForeignKey("survey_submissions.id"),
        nullable=False,
        index=True
    )
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, index=True)
    question_id = Column(String(100), nullable=False)
    category = Column(String(50), nullable=False)
    selected_option = Column(String(1), nullable=False)
    safe_score = Column(Integer, nullable=False)
    risk_score = Column(Integer, nullable=False)
    created_at = Column(
        DateTime(timezone=True),
        nullable=False,
        server_default=func.now()
    )

    submission = relationship(
        "SurveySubmission",
        back_populates="answers"
    )

    __table_args__ = (
        UniqueConstraint(
            "submission_id",
            "question_id",
            name="uq_survey_answers_submission_question"
        ),
    )

# -------- MINIGAME LEARNING SESSIONS --------
class MinigameSessionRecord(Base):
    __tablename__ = "minigame_session_records"

    id = Column(String(36), primary_key=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, index=True)
    topic = Column(String(50), nullable=False)
    risk = Column(String(20), nullable=False)
    minigame = Column(String(30), nullable=False)
    item_ids = Column(JSON, nullable=False)
    concept_ids = Column(JSON, nullable=False)
    status = Column(String(20), nullable=False, default="started")
    started_at = Column(
        DateTime(timezone=True),
        nullable=False,
        default=datetime.utcnow
    )
    completed_at = Column(DateTime(timezone=True), nullable=True)

    attempts = relationship(
        "MinigameAttempt",
        back_populates="session",
        cascade="all, delete-orphan"
    )


class MinigameAttempt(Base):
    __tablename__ = "minigame_attempts"

    id = Column(Integer, primary_key=True, index=True)
    session_id = Column(
        String(36),
        ForeignKey("minigame_session_records.id"),
        nullable=False,
        index=True
    )
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, index=True)
    item_id = Column(String(150), nullable=False, index=True)
    concept_ids = Column(JSON, nullable=False)
    difficulty = Column(String(20), nullable=False)
    correct = Column(Boolean, nullable=False)
    response_time_ms = Column(Integer, nullable=False)
    attempt_number = Column(Integer, nullable=False, default=1)
    points_delta = Column(Integer, nullable=False, default=0)
    created_at = Column(
        DateTime(timezone=True),
        nullable=False,
        default=datetime.utcnow
    )

    session = relationship(
        "MinigameSessionRecord",
        back_populates="attempts"
    )

    __table_args__ = (
        UniqueConstraint(
            "session_id",
            "item_id",
            "attempt_number",
            name="uq_minigame_attempt_session_item_number"
        ),
    )


class UserConceptMastery(Base):
    __tablename__ = "user_concept_mastery"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, index=True)
    concept_id = Column(String(150), nullable=False, index=True)
    topic = Column(String(50), nullable=False)
    alpha = Column(Float, nullable=False, default=2.0)
    beta = Column(Float, nullable=False, default=2.0)
    mastery_score = Column(Float, nullable=False, default=50.0)
    attempt_count = Column(Integer, nullable=False, default=0)
    correct_count = Column(Integer, nullable=False, default=0)
    incorrect_count = Column(Integer, nullable=False, default=0)
    evidence_weight = Column(Float, nullable=False, default=0.0)
    last_practiced_at = Column(DateTime(timezone=True), nullable=True)
    created_at = Column(
        DateTime(timezone=True),
        nullable=False,
        default=datetime.utcnow
    )
    updated_at = Column(
        DateTime(timezone=True),
        nullable=False,
        default=datetime.utcnow,
        onupdate=datetime.utcnow
    )

    __table_args__ = (
        UniqueConstraint(
            "user_id",
            "concept_id",
            name="uq_user_concept_mastery_user_concept"
        ),
    )

# -------- PILOT READINESS --------
class PilotConsent(Base):
    __tablename__ = "pilot_consents"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, index=True)
    participant_code = Column(String(80), nullable=False, unique=True, index=True)
    consent_version = Column(String(50), nullable=False)
    accepted = Column(Boolean, nullable=False, default=False)
    accepted_at = Column(DateTime(timezone=True), nullable=True)
    revoked_at = Column(DateTime(timezone=True), nullable=True)

    __table_args__ = (
        UniqueConstraint(
            "user_id",
            "consent_version",
            name="uq_pilot_consents_user_version"
        ),
    )


class RecommendationEvent(Base):
    __tablename__ = "recommendation_events"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"), nullable=False, index=True)
    risk_level = Column(String(20), nullable=False)
    recommended_training = Column(String(50), nullable=False)
    recommended_scenario = Column(Integer, nullable=False)
    source = Column(String(30), nullable=False)
    evidence_count = Column(Integer, nullable=False, default=0)
    created_at = Column(
        DateTime(timezone=True),
        nullable=False,
        default=datetime.utcnow
    )

# -------- AUDIT LOGS --------
class AuditLog(Base):
    __tablename__ = "audit_logs"

    id = Column(Integer, primary_key=True, index=True)
    
    actor_user_id = Column(Integer, ForeignKey("users.id"), nullable=True)
    target_user_id = Column(Integer, ForeignKey("users.id"), nullable=True)

    action = Column(String(100))   # ej: "CHANGE_ROLE", "CREATE_DECISION"
    description = Column(Text)

    ip_address = Column(String(45), nullable=True)

    created_at = Column(TIMESTAMP, default=datetime.utcnow)
