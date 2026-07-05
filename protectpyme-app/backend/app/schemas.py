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

from typing import Optional
from pydantic import BaseModel, EmailStr
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
