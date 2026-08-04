import os
import re
import sys
import types
from types import SimpleNamespace

import pytest
from sqlalchemy import create_engine
from sqlalchemy.orm import declarative_base, sessionmaker
from sqlalchemy.pool import StaticPool


os.environ.setdefault("SECRET_KEY", "test-secret")

TOPICS = ("phishing", "passwords", "malware", "wifi")
RISKS = ("alto", "medio", "bajo")
MINIGAMES = ("quiz", "wordsearch", "crossword")
WORD_GRID_SIZE = 10


def install_fake_database():
    test_base = declarative_base()
    engine = create_engine(
        "sqlite://",
        connect_args={"check_same_thread": False},
        poolclass=StaticPool,
    )
    testing_session_local = sessionmaker(
        autocommit=False,
        autoflush=False,
        bind=engine,
    )

    def get_test_db():
        db = testing_session_local()

        try:
            yield db
        finally:
            db.close()

    fake_database = types.ModuleType("app.database")
    fake_database.Base = test_base
    fake_database.engine = engine
    fake_database.SessionLocal = testing_session_local
    fake_database.get_db = get_test_db
    sys.modules["app.database"] = fake_database


@pytest.fixture
def app_modules():
    managed_modules = (
        "app.database",
        "app.models",
        "app.routes.auth",
        "app.routes.minigames",
    )
    previous_modules = {
        name: sys.modules.get(name)
        for name in managed_modules
    }

    if "app.database" not in sys.modules:
        install_fake_database()

    from app import models
    from app.services import minigame_service
    from app.services import minigame_session_service
    from app.services.concept_catalog import CONCEPT_CATALOG

    try:
        yield SimpleNamespace(
            models=models,
            minigame_service=minigame_service,
            minigame_session_service=minigame_session_service,
            concept_catalog=CONCEPT_CATALOG,
        )
    finally:
        for name, module in previous_modules.items():
            if module is None:
                sys.modules.pop(name, None)
            else:
                sys.modules[name] = module


@pytest.fixture(autouse=True)
def bind_app_modules(app_modules):
    globals()["models"] = app_modules.models
    globals()["minigame_service"] = app_modules.minigame_service
    globals()["minigame_session_service"] = app_modules.minigame_session_service
    globals()["CONCEPT_CATALOG"] = app_modules.concept_catalog


@pytest.fixture
def db(app_modules):
    active_models = app_modules.models
    engine = create_engine(
        "sqlite://",
        connect_args={"check_same_thread": False},
        poolclass=StaticPool,
    )
    testing_session_local = sessionmaker(
        autocommit=False,
        autoflush=False,
        bind=engine,
    )

    active_models.Base.metadata.drop_all(bind=engine)
    active_models.Base.metadata.create_all(bind=engine)
    session = testing_session_local()

    try:
        yield session
    finally:
        session.close()
        active_models.Base.metadata.drop_all(bind=engine)


def get_bank(topic, risk, minigame):
    if minigame == "quiz":
        return minigame_service.get_quiz(topic, risk)
    if minigame == "wordsearch":
        return minigame_service.get_wordsearch(topic, risk)
    return minigame_service.get_crossword(topic, risk)


def all_bank_items():
    for minigame in MINIGAMES:
        for topic in TOPICS:
            for risk in RISKS:
                for item in get_bank(topic, risk, minigame):
                    yield topic, risk, minigame, item


def item_concept_ids(item):
    return minigame_service.get_item_concept_ids(item)


def normalize(value):
    replacements = str.maketrans("ÁÉÍÓÚÜÑáéíóúüñ", "AEIOUUNaeiouun")
    text = str(value).translate(replacements)
    return "".join(character.upper() for character in text if character.isalnum())


def concept_search_text(concept):
    parts = [
        concept["term"],
        concept["definition"],
        concept["explanation"],
        concept["practical_example"],
        concept["common_mistake"],
        concept["recognition_clue"],
        *concept["aliases"],
    ]
    return normalize(" ".join(parts))


def create_user(db, email="expanded@example.com"):
    user = models.User(
        name="Expanded User",
        email=email,
        password="not-used",
    )
    db.add(user)
    db.commit()
    db.refresh(user)
    return user


def add_mastery(db, user_id, concept_id, mastery_score):
    record = models.UserConceptMastery(
        user_id=user_id,
        concept_id=concept_id,
        topic=CONCEPT_CATALOG[concept_id]["topic"],
        alpha=2.0,
        beta=2.0,
        mastery_score=mastery_score,
        attempt_count=1,
        correct_count=0,
        incorrect_count=1,
        evidence_weight=1.0,
    )
    db.add(record)
    db.commit()
    db.refresh(record)
    return record


def create_real_session(db, user_id):
    return minigame_session_service.create_minigame_session(
        topic="passwords",
        risk="bajo",
        minigame="crossword",
        db=db,
        user_id=user_id,
    )


def complete_session(db, user_id, session_id):
    return minigame_session_service.complete_minigame_session(
        db=db,
        user_id=user_id,
        session_id=session_id,
    )


def response_item_ids(session):
    return [item["item_id"] for item in session["items"]]


def response_concept_ids(session):
    concept_ids = []

    for item in session["items"]:
        for concept_id in item["concept_ids"]:
            if concept_id not in concept_ids:
                concept_ids.append(concept_id)

    return concept_ids


def test_every_quiz_pool_has_at_least_five_items():
    for topic in TOPICS:
        for risk in RISKS:
            assert len(minigame_service.get_quiz(topic, risk)) >= 5


def test_every_wordsearch_pool_has_at_least_five_items():
    for topic in TOPICS:
        for risk in RISKS:
            assert len(minigame_service.get_wordsearch(topic, risk)) >= 5


def test_every_crossword_pool_has_at_least_five_items():
    for topic in TOPICS:
        for risk in RISKS:
            assert len(minigame_service.get_crossword(topic, risk)) >= 5


def test_total_bank_has_at_least_180_items():
    assert sum(1 for _ in all_bank_items()) >= 180


def test_all_expanded_items_have_unique_item_id():
    item_ids = [
        item["item_id"]
        for _, _, _, item in all_bank_items()
    ]

    assert len(item_ids) == len(set(item_ids))


def test_all_expanded_items_have_valid_concepts():
    missing = []

    for _, _, _, item in all_bank_items():
        for concept_id in item_concept_ids(item):
            if concept_id not in CONCEPT_CATALOG:
                missing.append((item["item_id"], concept_id))

    assert not missing


def test_all_item_difficulties_match_pool_risk():
    mismatches = [
        (item["item_id"], risk, item.get("difficulty"))
        for _, risk, _, item in all_bank_items()
        if item.get("difficulty") != risk
    ]

    assert not mismatches


def test_quiz_correct_option_is_in_range():
    invalid = []

    for _, _, minigame, item in all_bank_items():
        if minigame != "quiz":
            continue

        if item["answer"] < 0 or item["answer"] >= len(item["options"]):
            invalid.append(item["item_id"])

    assert not invalid


def test_quiz_questions_are_not_duplicated_in_pool():
    for topic in TOPICS:
        for risk in RISKS:
            questions = [
                item["question"]
                for item in minigame_service.get_quiz(topic, risk)
            ]

            assert len(questions) == len(set(questions))


def test_wordsearch_answers_are_unique_in_pool():
    for topic in TOPICS:
        for risk in RISKS:
            answers = [
                item["answer"]
                for item in minigame_service.get_wordsearch(topic, risk)
            ]

            assert len(answers) == len(set(answers))


def test_crossword_answers_are_unique_in_pool():
    for topic in TOPICS:
        for risk in RISKS:
            answers = [
                item["answer"]
                for item in minigame_service.get_crossword(topic, risk)
            ]

            assert len(answers) == len(set(answers))


def test_wordsearch_answers_fit_grid_constraints():
    too_long = [
        item["item_id"]
        for _, _, minigame, item in all_bank_items()
        if minigame == "wordsearch" and len(item["answer"]) > WORD_GRID_SIZE
    ]

    assert not too_long


def test_wordsearch_answers_are_alphanumeric():
    invalid = [
        item["item_id"]
        for _, _, minigame, item in all_bank_items()
        if minigame == "wordsearch" and not re.fullmatch(r"[A-Z0-9]+", item["answer"])
    ]

    assert not invalid


def test_crossword_answers_are_alphanumeric():
    invalid = [
        item["item_id"]
        for _, _, minigame, item in all_bank_items()
        if minigame == "crossword" and not re.fullmatch(r"[A-Z0-9]+", item["answer"])
    ]

    assert not invalid


def test_no_legacy_argon_answer_returns():
    legacy = [
        item["item_id"]
        for _, _, minigame, item in all_bank_items()
        if minigame in ("wordsearch", "crossword")
        and item["answer"] == "ARGON"
    ]

    assert not legacy


def test_all_word_puzzle_answers_have_pedagogical_coverage():
    missing = []

    for _, _, minigame, item in all_bank_items():
        if minigame not in ("wordsearch", "crossword"):
            continue

        answer = normalize(item["answer"])
        concept_text = " ".join(
            concept_search_text(CONCEPT_CATALOG[concept_id])
            for concept_id in item_concept_ids(item)
        )

        if answer not in concept_text:
            missing.append((item["item_id"], item["answer"]))

    assert not missing


def test_all_quiz_items_have_required_concepts():
    missing = [
        item["item_id"]
        for _, _, minigame, item in all_bank_items()
        if minigame == "quiz" and not item_concept_ids(item)
    ]

    assert not missing


def test_new_catalog_concepts_have_complete_content():
    for concept in CONCEPT_CATALOG.values():
        assert concept["definition"].strip()
        assert concept["explanation"].strip()
        assert concept["practical_example"].strip()
        assert concept["common_mistake"].strip()
        assert concept["recognition_clue"].strip()


def test_real_bank_selection_returns_three_of_five(db):
    user = create_user(db)

    session = create_real_session(db, user.id)

    assert len(minigame_service.get_crossword("passwords", "bajo")) == 5
    assert len(session["items"]) == 3


def test_real_bank_recent_items_rotate(db):
    user = create_user(db)
    add_mastery(db, user.id, "passwords.hash", 40)
    add_mastery(db, user.id, "passwords.salt", 60)
    add_mastery(db, user.id, "passwords.argon2id", 60)

    first = create_real_session(db, user.id)
    complete_session(db, user.id, first["session_id"])
    second = create_real_session(db, user.id)
    complete_session(db, user.id, second["session_id"])
    third = create_real_session(db, user.id)

    selected_sets = {
        frozenset(response_item_ids(session))
        for session in (first, second, third)
    }

    assert len(selected_sets) > 1


def test_real_bank_weak_concept_remains_prioritized(db):
    user = create_user(db)
    add_mastery(db, user.id, "passwords.hash", 40)
    add_mastery(db, user.id, "passwords.salt", 60)
    add_mastery(db, user.id, "passwords.argon2id", 60)

    first = create_real_session(db, user.id)
    complete_session(db, user.id, first["session_id"])
    second = create_real_session(db, user.id)

    assert any(
        "passwords.hash" in item["concept_ids"]
        for item in first["items"]
    )
    assert any(
        "passwords.hash" in item["concept_ids"]
        for item in second["items"]
    )


def test_real_bank_lesson_matches_selected_items(db):
    user = create_user(db)

    session = create_real_session(db, user.id)
    lesson_terms = [
        concept["term"]
        for concept in session["lesson"]["key_concepts"]
    ]
    selected_terms = [
        CONCEPT_CATALOG[concept_id]["term"]
        for concept_id in response_concept_ids(session)
    ]

    assert lesson_terms == selected_terms


def test_real_bank_persisted_item_ids_match_response(db):
    user = create_user(db)

    session = create_real_session(db, user.id)
    record = (
        db.query(models.MinigameSessionRecord)
        .filter(models.MinigameSessionRecord.id == session["session_id"])
        .one()
    )

    assert record.item_ids == response_item_ids(session)


def test_real_bank_does_not_return_unselected_concepts(db):
    user = create_user(db)

    session = create_real_session(db, user.id)
    selected_concepts = set(response_concept_ids(session))
    lesson_concepts = {
        concept["term"]
        for concept in session["lesson"]["key_concepts"]
    }
    unselected_terms = {
        CONCEPT_CATALOG[concept_id]["term"]
        for item in minigame_service.get_crossword("passwords", "bajo")
        for concept_id in item_concept_ids(item)
        if concept_id not in selected_concepts
    }

    assert lesson_concepts.isdisjoint(unselected_terms)
