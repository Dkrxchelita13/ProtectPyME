import json
import unicodedata

import pytest

from app.services import learning_content_service, minigame_service
from app.services import minigame_session_service
from app.services.concept_catalog import CONCEPT_CATALOG


TOPICS = ("phishing", "passwords", "malware", "wifi")
RISKS = ("alto", "medio", "bajo")
MINIGAMES = ("quiz", "wordsearch", "crossword")

NEW_SCENARIO_CONCEPTS = {
    "passwords": {
        "risk": "medio",
        "concepts": {
            "passwords.credential_request",
            "passwords.identity_verification",
        },
        "coverage_terms": ("credencial", "identidad", "canaloficial", "report"),
    },
    "wifi": {
        "risk": "medio",
        "concepts": {
            "wifi.suspicious_traffic",
            "wifi.data_exfiltration",
        },
        "coverage_terms": ("trafico", "exfiltracion", "bloque", "report"),
    },
}

PEDAGOGICAL_ALIASES = {
    "PASSWORD": "CONTRASENA",
}

QUIZ_REQUIRED_CONCEPTS = {
    ("phishing", "alto"): ("engano", "reporte"),
    ("phishing", "medio"): ("dominio", "url"),
    ("phishing", "bajo"): ("spearphishing", "spf"),
    ("passwords", "alto"): ("contrasena", "reutilizacion"),
    ("passwords", "medio"): ("mfa", "gestor"),
    ("passwords", "bajo"): ("salt", "passwordspraying"),
    ("malware", "alto"): ("malware", "usb"),
    ("malware", "medio"): ("ransomware", "spyware"),
    ("malware", "bajo"): ("rootkit", "sandbox"),
    ("wifi", "alto"): ("redpublica", "datossensibles"),
    ("wifi", "medio"): ("vpn", "hotspotfalso"),
    ("wifi", "bajo"): ("eviltwin", "wpa3"),
}


def normalize(value):
    text = unicodedata.normalize("NFKD", str(value))
    text = "".join(character for character in text if not unicodedata.combining(character))
    return "".join(character for character in text.upper() if character.isalnum())


def normalize_for_text(value):
    return normalize(value).lower()


def concept_terms(lesson):
    return {
        normalize(concept["term"])
        for concept in lesson["key_concepts"]
    }


def all_lesson_text(lesson):
    parts = [
        lesson["explanation"],
        lesson["learning_objective"],
        lesson["recommended_action"],
        *lesson["tips"],
        lesson["practical_example"]["title"],
        *lesson["practical_example"]["steps"],
        lesson["common_mistake"]["title"],
        lesson["common_mistake"]["explanation"],
        lesson["quick_check"]["question"],
        lesson["quick_check"]["explanation"],
        *lesson["quick_check"]["options"],
    ]

    for concept in lesson["key_concepts"]:
        parts.extend(concept.values())

    return normalize_for_text(" ".join(parts))


def is_answer_covered(answer, terms):
    normalized_answer = normalize(answer)

    if normalized_answer in terms:
        return True

    alias = PEDAGOGICAL_ALIASES.get(normalized_answer)
    return alias in terms if alias else False


def item_concept_search_text(item):
    parts = []

    for concept_id in item_concept_ids(item):
        concept = CONCEPT_CATALOG[concept_id]
        parts.extend(
            [
                concept["term"],
                concept["definition"],
                concept["explanation"],
                concept["practical_example"],
                concept["common_mistake"],
                concept["recognition_clue"],
                *concept["aliases"],
            ]
        )

    return normalize(" ".join(parts))


def is_answer_covered_by_item_concepts(item):
    normalized_answer = normalize(item["answer"])

    if normalized_answer in item_concept_search_text(item):
        return True

    alias = PEDAGOGICAL_ALIASES.get(normalized_answer)
    return alias in item_concept_search_text(item) if alias else False


def all_bank_items():
    banks = {
        "quiz": minigame_service.QUIZ,
        "wordsearch": minigame_service.WORDSEARCH,
        "crossword": minigame_service.CROSSWORD,
    }

    for minigame, bank in banks.items():
        for topic, risks in bank.items():
            for risk, items in risks.items():
                for item in items:
                    yield minigame, topic, risk, item


def item_concept_ids(item):
    if "concept_ids" in item:
        return list(item["concept_ids"])
    if "concept_id" in item:
        return [item["concept_id"]]
    return []


def test_all_minigame_items_have_unique_item_id():
    item_ids = [
        item.get("item_id")
        for _, _, _, item in all_bank_items()
    ]

    assert all(item_ids)
    assert len(item_ids) == len(set(item_ids))


def test_all_minigame_items_have_concept_ids():
    missing = [
        item.get("item_id", item)
        for _, _, _, item in all_bank_items()
        if not item_concept_ids(item)
    ]

    assert not missing


def test_all_item_concept_ids_exist_in_catalog():
    missing = []

    for _, _, _, item in all_bank_items():
        for concept_id in item_concept_ids(item):
            if concept_id not in CONCEPT_CATALOG:
                missing.append((item["item_id"], concept_id))

    assert not missing


def test_new_scenario_concepts_exist_with_expected_topics():
    for topic, expected in NEW_SCENARIO_CONCEPTS.items():
        for concept_id in expected["concepts"]:
            concept = CONCEPT_CATALOG[concept_id]

            assert concept["topic"] == topic
            assert concept["difficulty"] == expected["risk"]


@pytest.mark.parametrize("topic", ("passwords", "wifi"))
@pytest.mark.parametrize("minigame", MINIGAMES)
def test_new_scenario_concepts_are_taught_in_lessons(topic, minigame):
    expected = NEW_SCENARIO_CONCEPTS[topic]
    lesson = learning_content_service.get_learning_content(
        topic,
        expected["risk"],
        minigame,
    )
    searchable_text = all_lesson_text(lesson)

    missing = [
        term
        for term in expected["coverage_terms"]
        if term not in searchable_text
    ]

    assert not missing


@pytest.mark.parametrize("topic", ("passwords", "wifi"))
@pytest.mark.parametrize("minigame", MINIGAMES)
def test_new_scenario_concepts_are_available_in_minigame_banks(topic, minigame):
    expected = NEW_SCENARIO_CONCEPTS[topic]
    bank = {
        "quiz": minigame_service.get_quiz,
        "wordsearch": minigame_service.get_wordsearch,
        "crossword": minigame_service.get_crossword,
    }[minigame](topic, expected["risk"])
    concepts = {
        concept_id
        for item in bank
        for concept_id in item_concept_ids(item)
    }

    assert expected["concepts"].issubset(concepts)


@pytest.mark.parametrize("topic", ("passwords", "wifi"))
@pytest.mark.parametrize("minigame", MINIGAMES)
def test_new_scenario_concept_ids_reach_minigame_sessions(topic, minigame):
    expected = NEW_SCENARIO_CONCEPTS[topic]
    session = minigame_session_service.create_minigame_session(
        topic=topic,
        risk=expected["risk"],
        minigame=minigame,
    )
    concepts = {
        concept_id
        for item in session["items"]
        for concept_id in item["concept_ids"]
    }

    assert expected["concepts"].issubset(concepts)


def test_item_difficulty_matches_bank_risk():
    mismatches = [
        (item["item_id"], risk, item.get("difficulty"))
        for _, _, risk, item in all_bank_items()
        if item.get("difficulty") != risk
    ]

    assert not mismatches


def test_passwords_bajo_puzzles_reference_argon2id():
    for minigame, getter in (
        ("wordsearch", minigame_service.get_wordsearch),
        ("crossword", minigame_service.get_crossword),
    ):
        concepts = {
            concept_id
            for item in getter("passwords", "bajo")
            for concept_id in item_concept_ids(item)
        }

        assert "passwords.argon2id" in concepts, minigame


def test_no_item_uses_legacy_argon_concept():
    legacy = [
        item["item_id"]
        for _, _, _, item in all_bank_items()
        for concept_id in item_concept_ids(item)
        if concept_id == "passwords.argon"
    ]

    assert not legacy


@pytest.mark.parametrize("minigame", ("wordsearch", "crossword"))
@pytest.mark.parametrize("topic", TOPICS)
@pytest.mark.parametrize("risk", RISKS)
def test_word_puzzle_bank_answers_are_taught_or_controlled_aliases(topic, risk, minigame):
    bank = (
        minigame_service.get_wordsearch(topic, risk)
        if minigame == "wordsearch"
        else minigame_service.get_crossword(topic, risk)
    )

    missing = [
        (item["item_id"], item["answer"])
        for item in bank
        if not is_answer_covered_by_item_concepts(item)
    ]

    assert not missing, (
        f"Respuestas no cubiertas para {topic}/{risk}/{minigame}: {missing}"
    )


@pytest.mark.parametrize("topic", TOPICS)
@pytest.mark.parametrize("risk", RISKS)
def test_quiz_required_concepts_are_taught(topic, risk):
    lesson = learning_content_service.get_learning_content(topic, risk, "quiz")
    searchable_text = all_lesson_text(lesson)

    missing = [
        concept
        for concept in QUIZ_REQUIRED_CONCEPTS[(topic, risk)]
        if concept not in searchable_text
    ]

    assert not missing, (
        f"Conceptos de quiz no cubiertos para {topic}/{risk}: {missing}"
    )


@pytest.mark.parametrize("topic", TOPICS)
@pytest.mark.parametrize("risk", RISKS)
def test_lessons_are_specific_by_minigame(topic, risk):
    lessons = {
        minigame: learning_content_service.get_learning_content(topic, risk, minigame)
        for minigame in MINIGAMES
    }

    compared_fields = (
        "key_concepts",
        "practical_example",
        "common_mistake",
        "quick_check",
        "visual_key",
    )

    pairs = (
        ("quiz", "wordsearch"),
        ("quiz", "crossword"),
        ("wordsearch", "crossword"),
    )

    for left, right in pairs:
        differences = sum(
            json.dumps(lessons[left][field], sort_keys=True, ensure_ascii=False)
            != json.dumps(lessons[right][field], sort_keys=True, ensure_ascii=False)
            for field in compared_fields
        )

        assert differences >= 2, (
            f"Lecciones demasiado parecidas para {topic}/{risk}: {left} vs {right}"
        )


def test_passwords_bajo_has_distinct_quick_checks_and_examples():
    quiz = learning_content_service.get_learning_content("passwords", "bajo", "quiz")
    wordsearch = learning_content_service.get_learning_content("passwords", "bajo", "wordsearch")
    crossword = learning_content_service.get_learning_content("passwords", "bajo", "crossword")

    assert quiz["quick_check"] != wordsearch["quick_check"]
    assert quiz["quick_check"] != crossword["quick_check"]
    assert wordsearch["quick_check"] != crossword["quick_check"]

    titles = {
        quiz["practical_example"]["title"],
        wordsearch["practical_example"]["title"],
        crossword["practical_example"]["title"],
    }

    assert len(titles) == 3


def test_all_valid_combinations_use_specific_supplements_without_fallback():
    for topic in TOPICS:
        for risk in RISKS:
            for minigame in MINIGAMES:
                lesson = learning_content_service.get_learning_content(topic, risk, minigame)

                assert lesson["topic"] == topic
                assert lesson["risk"] == risk
                assert lesson["minigame"] == minigame
                assert lesson["key_concepts"]
                assert lesson["practical_example"]["title"]
                assert lesson["common_mistake"]["title"]
                assert lesson["quick_check"]["question"]
                assert lesson["visual_key"]


def test_passwords_bajo_wordsearch_uses_argon2id_directly():
    answers = {
        item["answer"]
        for item in minigame_service.get_wordsearch("passwords", "bajo")
    }

    assert "ARGON2ID" in answers
    assert "ARGON" not in answers


def test_passwords_bajo_crossword_uses_argon2id_directly():
    answers = {
        item["answer"]
        for item in minigame_service.get_crossword("passwords", "bajo")
    }

    assert "ARGON2ID" in answers
    assert "ARGON" not in answers


def test_argon_alias_is_removed():
    assert "ARGON" not in PEDAGOGICAL_ALIASES


def test_passwords_bajo_word_puzzles_cover_argon2id_directly():
    expected = {"SALT", "HASH", "ARGON2ID"}

    for minigame in ("wordsearch", "crossword"):
        lesson = learning_content_service.get_learning_content(
            "passwords",
            "bajo",
            minigame,
        )
        terms = concept_terms(lesson)

        assert expected.issubset(terms)
