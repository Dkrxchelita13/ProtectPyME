# User Progress Backfill Report

## Status

No production database and no Neon database were inspected or modified during
Fase 5.

Backfill support was implemented as a dry-run-first script:

```powershell
cd C:\Users\luzgr\Documents\ProtectPyME\protectpyme-app\backend
python scripts\backfill_user_progress.py
```

To persist changes after reviewing the dry-run output:

```powershell
python scripts\backfill_user_progress.py --apply
```

The script uses the current backend `DATABASE_URL` configuration. It should be
run against production only after backup/export and after the dry-run report is
reviewed.

## Inference Rules

Backfill reads `Decision` rows only. `Decision.is_correct`, points, analytics,
RF risk, leaderboard, badges, and minigame records are not used.

Initial progression:

| Recorded decisions | Backfilled initial stage | Minigames |
| --- | ---: | --- |
| none | `1` | false |
| `1` | `2` | false |
| `1,2` | `3` | false |
| `1,2,3` | `4` | true |
| `3` only | `1` | false, inconsistent `3` |

Intermediate progression:

| Recorded decisions | Backfilled intermediate stage | Minigames |
| --- | ---: | --- |
| `5` | `2` | false |
| `5,6` | `3` | false |
| `5,6,7` | `4` | true |
| `7` only | `1` | false, inconsistent `7` |

Scenario `4` is historical in the current Unity mapping and does not update
narrative progress.

## Safety Properties

- Dry-run is the default.
- Existing progress is monotonic; backfill never lowers a stored stage.
- Missing previous scenarios are not silently inferred.
- Output includes only user ids and scenario ids, not PII.
- The script does not commit unless `--apply` is provided.

## Test Coverage

Backfill behavior is covered in isolated SQLite tests:

- contiguous initial sequence
- scenario `3` only does not infer missing previous scenarios
- incorrect decisions count as scenario completion
- existing higher progress is not downgraded

No users were changed outside test databases.
