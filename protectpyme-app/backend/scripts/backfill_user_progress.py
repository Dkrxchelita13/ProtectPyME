import argparse
import json

from app.database import SessionLocal
from app.services.progress_service import backfill_user_progress


def main():
    parser = argparse.ArgumentParser(
        description="Backfill UserProgress from recorded Decision rows."
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Persist changes. Without this flag the script runs as dry-run."
    )
    args = parser.parse_args()

    db = SessionLocal()
    try:
        report = backfill_user_progress(db, dry_run=not args.apply)

        if args.apply:
            db.commit()
        else:
            db.rollback()

        print(json.dumps(report, indent=2, sort_keys=True))
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


if __name__ == "__main__":
    main()
