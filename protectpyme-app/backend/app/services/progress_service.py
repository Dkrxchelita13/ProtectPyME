from dataclasses import dataclass
from datetime import datetime
import logging
from typing import Dict, Iterable, List, Sequence

from sqlalchemy.orm import Session

from app import models


logger = logging.getLogger("protectpyme")

INITIAL_SCENARIO_SEQUENCE = (1, 2, 3)
INTERMEDIATE_SCENARIO_SEQUENCE = (5, 6, 7)
HISTORICAL_SCENARIO_IDS = (4,)

INITIAL_UNLOCK_TARGETS = {
    1: 2,
    2: 3,
    3: 4,
}
INTERMEDIATE_UNLOCK_TARGETS = {
    5: 2,
    6: 3,
    7: 4,
}


@dataclass(frozen=True)
class ProgressDerivation:
    highest_initial_stage_unlocked: int
    highest_intermediate_stage_unlocked: int
    minigames_unlocked: bool
    inconsistent_scenario_ids: List[int]


def get_or_create_user_progress(
    db: Session,
    user_id: int
) -> models.UserProgress:
    progress = (
        db.query(models.UserProgress)
        .filter(models.UserProgress.user_id == user_id)
        .first()
    )

    if progress is not None:
        return progress

    progress = models.UserProgress(
        user_id=user_id,
        highest_initial_stage_unlocked=1,
        highest_intermediate_stage_unlocked=1,
        minigames_unlocked=False,
        updated_at=datetime.utcnow()
    )
    db.add(progress)
    db.flush()
    return progress


def apply_scenario_completion(
    db: Session,
    user_id: int,
    scenario_id: int
) -> models.UserProgress:
    progress = get_or_create_user_progress(db, user_id)
    changed = False

    if scenario_id in INITIAL_UNLOCK_TARGETS:
        target_stage = INITIAL_UNLOCK_TARGETS[scenario_id]
        changed = _advance_initial_progress(
            progress,
            scenario_id,
            target_stage
        )
    elif scenario_id in INTERMEDIATE_UNLOCK_TARGETS:
        target_stage = INTERMEDIATE_UNLOCK_TARGETS[scenario_id]
        changed = _advance_intermediate_progress(
            progress,
            scenario_id,
            target_stage
        )
    elif scenario_id in HISTORICAL_SCENARIO_IDS:
        logger.info(
            "Scenario %s is historical and does not change narrative progress",
            scenario_id
        )
    else:
        logger.info(
            "Scenario %s has no configured narrative progress mapping",
            scenario_id
        )

    should_unlock_minigames = (
        progress.highest_initial_stage_unlocked >= 4
        or progress.highest_intermediate_stage_unlocked >= 4
    )
    if should_unlock_minigames and not progress.minigames_unlocked:
        progress.minigames_unlocked = True
        changed = True

    if changed:
        progress.updated_at = datetime.utcnow()

    return progress


def derive_progress_from_scenario_ids(
    scenario_ids: Iterable[int]
) -> ProgressDerivation:
    completed_ids = {int(scenario_id) for scenario_id in scenario_ids}

    initial_stage, initial_inconsistent = _derive_contiguous_stage(
        completed_ids,
        INITIAL_SCENARIO_SEQUENCE
    )
    intermediate_stage, intermediate_inconsistent = _derive_contiguous_stage(
        completed_ids,
        INTERMEDIATE_SCENARIO_SEQUENCE
    )

    return ProgressDerivation(
        highest_initial_stage_unlocked=initial_stage,
        highest_intermediate_stage_unlocked=intermediate_stage,
        minigames_unlocked=initial_stage >= 4 or intermediate_stage >= 4,
        inconsistent_scenario_ids=sorted(
            initial_inconsistent + intermediate_inconsistent
        )
    )


def backfill_user_progress(db: Session, dry_run: bool = True) -> Dict:
    decisions_by_user: Dict[int, List[int]] = {}
    rows = (
        db.query(models.Decision.user_id, models.Decision.scenario_id)
        .order_by(models.Decision.user_id.asc(), models.Decision.created_at.asc())
        .all()
    )

    for user_id, scenario_id in rows:
        decisions_by_user.setdefault(user_id, []).append(scenario_id)

    results = []

    for user_id in sorted(decisions_by_user.keys()):
        scenario_ids = decisions_by_user[user_id]
        derivation = derive_progress_from_scenario_ids(scenario_ids)
        existing = (
            db.query(models.UserProgress)
            .filter(models.UserProgress.user_id == user_id)
            .first()
        )

        target_initial = derivation.highest_initial_stage_unlocked
        target_intermediate = derivation.highest_intermediate_stage_unlocked
        target_minigames = derivation.minigames_unlocked
        action = "noop"

        if existing is None:
            action = "create"
            before = None
        else:
            before = {
                "highest_initial_stage_unlocked": (
                    existing.highest_initial_stage_unlocked
                ),
                "highest_intermediate_stage_unlocked": (
                    existing.highest_intermediate_stage_unlocked
                ),
                "minigames_unlocked": existing.minigames_unlocked,
            }
            target_initial = max(
                existing.highest_initial_stage_unlocked,
                target_initial
            )
            target_intermediate = max(
                existing.highest_intermediate_stage_unlocked,
                target_intermediate
            )
            target_minigames = existing.minigames_unlocked or target_minigames

            if (
                target_initial != existing.highest_initial_stage_unlocked
                or target_intermediate != (
                    existing.highest_intermediate_stage_unlocked
                )
                or target_minigames != existing.minigames_unlocked
            ):
                action = "update"

        if not dry_run and action != "noop":
            progress = existing or models.UserProgress(user_id=user_id)
            progress.highest_initial_stage_unlocked = target_initial
            progress.highest_intermediate_stage_unlocked = target_intermediate
            progress.minigames_unlocked = target_minigames
            progress.updated_at = datetime.utcnow()
            db.add(progress)
            db.flush()

        results.append(
            {
                "user_id": user_id,
                "scenario_ids": sorted(set(scenario_ids)),
                "before": before,
                "derived": {
                    "highest_initial_stage_unlocked": (
                        derivation.highest_initial_stage_unlocked
                    ),
                    "highest_intermediate_stage_unlocked": (
                        derivation.highest_intermediate_stage_unlocked
                    ),
                    "minigames_unlocked": derivation.minigames_unlocked,
                },
                "target": {
                    "highest_initial_stage_unlocked": target_initial,
                    "highest_intermediate_stage_unlocked": target_intermediate,
                    "minigames_unlocked": target_minigames,
                },
                "inconsistent_scenario_ids": (
                    derivation.inconsistent_scenario_ids
                ),
                "action": action,
            }
        )

    return {
        "dry_run": dry_run,
        "users_seen": len(results),
        "users_to_create": sum(1 for result in results if result["action"] == "create"),
        "users_to_update": sum(1 for result in results if result["action"] == "update"),
        "users_unchanged": sum(1 for result in results if result["action"] == "noop"),
        "users_with_inconsistencies": sum(
            1 for result in results if result["inconsistent_scenario_ids"]
        ),
        "results": results,
    }


def _advance_initial_progress(
    progress: models.UserProgress,
    scenario_id: int,
    target_stage: int
) -> bool:
    required_stage = target_stage - 1

    if progress.highest_initial_stage_unlocked < required_stage:
        logger.warning(
            "User %s completed scenario %s out of sequence; initial progress "
            "remains at stage %s",
            progress.user_id,
            scenario_id,
            progress.highest_initial_stage_unlocked
        )
        return False

    if progress.highest_initial_stage_unlocked >= target_stage:
        return False

    progress.highest_initial_stage_unlocked = target_stage
    return True


def _advance_intermediate_progress(
    progress: models.UserProgress,
    scenario_id: int,
    target_stage: int
) -> bool:
    required_stage = target_stage - 1

    if progress.highest_intermediate_stage_unlocked < required_stage:
        logger.warning(
            "User %s completed scenario %s out of sequence; intermediate "
            "progress remains at stage %s",
            progress.user_id,
            scenario_id,
            progress.highest_intermediate_stage_unlocked
        )
        return False

    if progress.highest_intermediate_stage_unlocked >= target_stage:
        return False

    progress.highest_intermediate_stage_unlocked = target_stage
    return True


def _derive_contiguous_stage(
    completed_ids: set,
    sequence: Sequence[int]
) -> tuple:
    stage = 1
    missing_previous = False
    inconsistent_ids = []

    for index, scenario_id in enumerate(sequence, start=1):
        if scenario_id in completed_ids:
            if missing_previous:
                inconsistent_ids.append(scenario_id)
            else:
                stage = index + 1
        else:
            missing_previous = True

    return stage, inconsistent_ids
