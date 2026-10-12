# Phase 5 Test Report

## Backend

Environment:

- Python: `C:\Users\luzgr\Documents\ProtectPyME\.codex_build\phase4-py312\Scripts\python.exe`
- Working directory for full suite: `C:\Users\luzgr\Documents\ProtectPyME\protectpyme-app\backend`
- Test DBs: isolated SQLite fixtures
- `DATABASE_URL` and `TEST_DATABASE_URL`: removed for test runs
- `SECRET_KEY`: `test-secret`
- `GOOGLE_CLIENT_IDS`: test client id

Results:

| Command | Result |
| --- | --- |
| `python -m py_compile app/services/progress_service.py app/routes/progress.py scripts/backfill_user_progress.py tests/test_user_progress.py` | passed |
| `pytest tests/test_user_progress.py -vv --tb=short` | 17 passed |
| `pytest -q --tb=short --durations=25` | 459 passed |

Warnings remain from existing project patterns: Pydantic v2 class `Config`,
`datetime.utcnow()` deprecations, and one existing pytest collection warning in
`test_survey.py`. No backend test failed in the full suite.

## Mandatory Progress Cases

Covered by `tests/test_user_progress.py`:

- new user `GET /progress/me` returns defaults
- scenario `1` correct unlocks initial stage `2`
- scenario `1` incorrect unlocks initial stage `2`
- repeating scenario `1` is idempotent
- scenario `2` after scenario `1` unlocks initial stage `3`
- scenario `3` after `1,2` unlocks stage `4` and minigames
- scenario `3` out of sequence does not infer missing stages
- intermediate track uses playable backend ids `5,6,7`
- users have independent progress rows
- authentication is required
- no public `GET /progress/{user_id}` and no public progress `POST`
- progress is monotonic when an older scenario repeats
- decision/progress rollback together when progress update fails
- backfill infers contiguous progress
- backfill does not infer scenario `3` only
- backfill ignores correctness and uses `Decision` existence
- backfill does not downgrade existing progress

## Unity

Unity Editor found:

```text
C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe
```

Batchmode compile command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -batchmode -quit -nographics -projectPath 'C:\Users\luzgr\Documents\ProtectPyME\ProtectPYME' -logFile 'C:\Users\luzgr\Documents\ProtectPyME\ProtectPYME\phase5-unity-compile.log'
```

Result:

- Unity exited with return code `0`.
- Script compilation completed.
- No `error CS` compiler errors.
- Remaining warning: pre-existing `QuizController.usandoBackend` assigned but
  not used.

`dotnet build --no-restore` was intentionally not used as proof, because this
Unity project relies on Unity-generated project assets.

## Notes

One custom forced-order regression command that ran `test_pilot_assessment.py`
before `test_minigame_session.py` exposed an existing module identity issue in
the tests. The same minigame contract passes isolated, and the full suite in
normal backend order passes.
