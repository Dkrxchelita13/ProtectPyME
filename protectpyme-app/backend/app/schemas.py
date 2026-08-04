""" from pydantic import BaseModel


class UserCreate(BaseModel):
    name: str
    email: str
    password: str


"" class DecisionCreate(BaseModel):
    user_id: int
    scenario_id: int
    choice: str ""

class DecisionCreate(BaseModel):
    scenario_id: int
    choice: str


"" class ScenarioBase(BaseModel):
    title: str
    description: str | None = None
    difficulty: str = "easy"

class ScenarioCreate(ScenarioBase):
    pass

class Scenario(ScenarioBase):
    id: int

    class Config:
        from_attributes = True ""
"" class ScenarioBase(BaseModel):
    title: str
    description: str
    risk_level: str ""

class ScenarioBase(BaseModel):
    title: str
    description: str
    difficulty: str = "easy"

class ScenarioCreate(ScenarioBase):
    pass

class Scenario(ScenarioBase):
    id: int

    class Config:
        from_attributes = True


class ScoreBase(BaseModel):
    user_id: int
    scenario_id: int
    points: int

class ScoreCreate(ScoreBase):
    pass

class Score(ScoreBase):
    id: int

    class Config:
        from_attributes = True
 """

from typing import Dict, List, Optional
from uuid import UUID
from pydantic import BaseModel, ConfigDict, EmailStr, Field, field_validator
from typing import Literal

class RoleUpdate(BaseModel):
    role: Literal["user", "admin"]

# -------- USERS --------

class UserCreate(BaseModel):
    name: str
    email: EmailStr
    password: str

class GoogleLoginRequest(BaseModel):
    id_token: str

from datetime import datetime
class UserOut(BaseModel):
    id: int
    name: str
    email: str
    total_points: int
    level: str
    risk_score: int
    total_decisions: int
    correct_decisions: int
    role: str
    created_at: datetime

    class Config:
        from_attributes = True

# class UserOut(BaseModel):
#     id: int
#     name: str
#     email: str
#     total_points: int
#     created_at: datetime

#     class Config:
#         from_attributes = True
# -------- DECISIONS --------

class DecisionCreate(BaseModel):
    scenario_id: int
    choice: str
    response_time: Optional[int] = None


# -------- SCENARIOS --------

class ScenarioBase(BaseModel):
    title: str
    description: str
    difficulty: str = "easy"
    category: Optional[str] = None

    correct_choice: str   # respuesta

    points_correct: int = 10
    points_incorrect: int = 0


class ScenarioCreate(ScenarioBase):
    pass


class Scenario(ScenarioBase):
    id: int

    class Config:
        from_attributes = True

# -------- LEADERBOARD --------

class LeaderboardUser(BaseModel):
    rank: int
    id: int
    name: str
    total_points: int

    class Config:
        from_attributes = True




from pydantic import BaseModel
from datetime import datetime


class DecisionOut(BaseModel):
    id: int
    scenario_id: int
    choice: str
    points_awarded: int
    created_at: datetime

    class Config:
        #orm_mode = True
        from_attributes = True

from datetime import datetime
from pydantic import BaseModel

class AuditLogOut(BaseModel):
    id: int
    actor_user_id: int
    target_user_id: int | None
    action: str
    description: str | None
    ip_address: str | None
    created_at: datetime

    class Config:
        from_attributes = True


# -------- DIAGNOSTIC SURVEY --------

class SurveyAnswerSubmit(BaseModel):
    question_id: str
    category: str
    selected_option: str


class SurveySubmitRequest(BaseModel):
    survey_version: str
    answers: List[SurveyAnswerSubmit]


class SurveyCategoryScore(BaseModel):
    safe_score: int
    max_score: int
    risk_score: int


class SurveySubmitResponse(BaseModel):
    submitted: bool
    survey_version: str
    primary_weakness: str
    initial_risk: str
    total_risk_score: int
    category_scores: Dict[str, SurveyCategoryScore]


class SurveyStatusResponse(BaseModel):
    has_submitted: bool
    survey_version: Optional[str] = None
    submitted_at: Optional[datetime] = None
    primary_weakness: Optional[str] = None
    initial_risk: Optional[str] = None


class SurveyAnswerOut(BaseModel):
    question_id: str
    category: str
    selected_option: str
    safe_score: int
    risk_score: int

    class Config:
        from_attributes = True


class SurveySubmissionOut(BaseModel):
    id: int
    survey_version: str
    submitted_at: datetime
    primary_weakness: str
    initial_risk: str
    total_risk_score: int
    category_scores: Dict[str, SurveyCategoryScore]
    answers: List[SurveyAnswerOut]


# -------- MINIGAME LEARNING CONTENT --------

class LessonConcept(BaseModel):
    term: str
    definition: str
    why_it_matters: str
    example: str


class LessonPracticalExample(BaseModel):
    title: str
    steps: List[str]


class LessonCommonMistake(BaseModel):
    title: str
    explanation: str


class LessonQuickCheck(BaseModel):
    question: str
    options: List[str]
    correct_option: int
    explanation: str


class MinigameLessonResponse(BaseModel):
    topic: str
    risk: str
    minigame: str
    title: str
    vulnerability: str
    learning_objective: str
    explanation: str
    tips: List[str]
    recommended_action: str
    key_concepts: List[LessonConcept]
    practical_example: LessonPracticalExample
    common_mistake: LessonCommonMistake
    quick_check: LessonQuickCheck
    visual_key: str


class MinigameSessionRequest(BaseModel):
    topic: Literal["phishing", "passwords", "malware", "wifi"]
    risk: Literal["alto", "medio", "bajo"]
    minigame: Literal["quiz", "wordsearch", "crossword"]


class MinigameSessionItem(BaseModel):
    item_id: str
    concept_ids: List[str]
    difficulty: Literal["alto", "medio", "bajo"]
    question: Optional[str] = None
    options: Optional[List[str]] = None
    clue: Optional[str] = None
    answer_text: str
    correct_option: int


class MinigameSessionResponse(BaseModel):
    session_id: str
    topic: str
    risk: str
    minigame: str
    lesson: MinigameLessonResponse
    items: List[MinigameSessionItem]


class MinigameAttemptRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    session_id: str
    item_id: str = Field(..., min_length=1, max_length=150)
    correct: bool
    response_time_ms: int = Field(..., ge=0, le=3_600_000)
    attempt_number: int = Field(1, ge=1, le=100)
    points_delta: int = Field(0, ge=-1000, le=1000)

    @field_validator("session_id")
    @classmethod
    def session_id_must_be_uuid(cls, value):
        try:
            return str(UUID(str(value)))
        except ValueError as exc:
            raise ValueError("session_id must be a valid UUID") from exc


class MinigameAttemptResponse(BaseModel):
    id: int
    session_id: str
    item_id: str
    concept_ids: List[str]
    difficulty: str
    correct: bool
    response_time_ms: int
    attempt_number: int
    points_delta: int
    created_at: datetime


class MinigameSessionSummaryResponse(BaseModel):
    session_id: str
    status: str
    topic: str
    risk: str
    minigame: str
    total_items: int
    attempted_items: int
    total_attempts: int
    correct_attempts: int
    incorrect_attempts: int
    points_earned: int
    accuracy: float
    total_response_time_ms: int
    started_at: datetime
    completed_at: Optional[datetime]
