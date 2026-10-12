# Progress Architecture V1

## Objective

Fase 5 makes the backend the source of truth for narrative progression.
Unity `PlayerPrefs` remains a local cache and offline fallback only. When the
backend responds successfully, `/progress/me` overwrites the old local cache.

This phase does not solve per-user scoping of `PlayerPrefs`; that remains for
Fase 6.

## Backend Model

`UserProgress` is stored in the backend database with the minimal progression
state:

- `user_id`
- `highest_initial_stage_unlocked`
- `highest_intermediate_stage_unlocked`
- `minigames_unlocked`
- `updated_at`

The project does not use Alembic. Schema creation follows the current project
mechanism: `Base.metadata.create_all(bind=engine)` at startup and in tests.
No production or Neon migration was applied by this phase.

## Verified Scenario Mapping

The mapping was verified from Unity scene/controller usage and backend seed
data instead of assuming `1/2/3` and `4/5/6`.

| Backend scenario | Unity usage | Track | Completion unlock |
| --- | --- | --- | --- |
| `1` | `Escenario` / phishing email | initial | initial stage `2` |
| `2` | `Escenario2_Acceso` | initial | initial stage `3` |
| `3` | `Escenario 3 (USB sospechoso)` | initial | initial stage `4`, minigames |
| `4` | Seeded historical WiFi scenario | none | no narrative progress change |
| `5` | `Escenario 4` via configurable `Escenario 1 Manager` | intermediate | intermediate stage `2` |
| `6` | `Escenario 5` | intermediate | intermediate stage `3` |
| `7` | `Escenario 6` | intermediate | intermediate stage `4`, minigames |

Scenario completion is semantic: a valid `Decision` row recorded through
`POST /decisions/` counts as completion whether the selected choice was correct
or incorrect.

## Decision Transaction

`POST /decisions/` remains the only public mutation path for narrative
progression. The route calls `crud_decisions.create_decision`, which now:

1. Validates scenario, rate limit, and response time.
2. Evaluates correctness and points.
3. Inserts the `Decision`.
4. Updates legacy user metrics, badges, and category points.
5. Applies `progress_service.apply_scenario_completion(...)`.
6. Commits once.

If progression update fails, the existing exception handler rolls back the
whole transaction. The decision and progress update therefore succeed or fail
together.

## Progress API

`GET /progress/me` is authenticated and returns only the current user's
progress. It creates a default row on first read:

- initial stage `1`
- intermediate stage `1`
- `minigames_unlocked = false`

No public `POST /progress` endpoint was added. No `GET /progress/{user_id}`
endpoint was added.

## Unity Sync

Unity now syncs progression through `APIManager`:

- `GET /progress/me` is implemented as `GetUserProgress`.
- `RefreshProgressFromBackend` applies the backend response to local cache.
- Login calls `RefreshProgressFromBackend` before survey routing.
- A successful `SendDecision` refreshes `/progress/me` before scenario
  callbacks continue.
- If backend progress is unavailable, Unity keeps the existing local cache and
  logs `[ProgressSync] backend unavailable, using cached state`.
- Unity never sends `PlayerPrefs` progression back to the backend.

`ProgresoController` keeps its legacy points/awareness visual rule for the
profile panel, but it no longer writes `NivelAlcanzado`. That write would make
analytics override narrative progress, so it was removed as part of making the
backend authoritative.

## Separation From Other Systems

`UserProgress` is independent from:

- points
- awareness/security analytics
- random forest / AI risk
- leaderboard
- pilot eligibility

Pilot readiness still uses `Decision` plus `MinigameSessionRecord`, not
`UserProgress`.

## Backfill Strategy

Backfill derives progress only from existing `Decision` rows and ignores
correctness. It is conservative and contiguous:

- Scenario `1` alone unlocks initial stage `2`.
- Scenarios `1,2` unlock initial stage `3`.
- Scenarios `1,2,3` unlock initial stage `4` and minigames.
- Scenario `3` alone does not infer scenarios `1` and `2`; it is reported as
  inconsistent and leaves initial stage at `1`.
- Existing `UserProgress` is never downgraded.

See `USER_PROGRESS_BACKFILL_REPORT.md` for operational notes.
