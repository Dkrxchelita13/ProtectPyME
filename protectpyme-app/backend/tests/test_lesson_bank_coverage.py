import json
import unicodedata

import pytest

from app.services import learning_content_service, minigame_service


TOPICS = ("phishing", "passwords", "malware", "wifi")
RISKS = ("alto", "medio", "bajo")
MINIGAMES = ("quiz", "wordsearch", "crossword")

PEDAGOGICAL_ALIASES = {
    "ARGON": "ARGON2ID",
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


@pytest.mark.parametrize("minigame", ("wordsearch", "crossword"))
@pytest.mark.parametrize("topic", TOPICS)
@pytest.mark.parametrize("risk", RISKS)
def test_word_puzzle_bank_answers_are_taught_or_controlled_aliases(topic, risk, minigame):
    lesson = learning_content_service.get_learning_content(topic, risk, minigame)
    terms = concept_terms(lesson)
    bank = (
        minigame_service.get_wordsearch(topic, risk)
        if minigame == "wordsearch"
        else minigame_service.get_crossword(topic, risk)
    )

    missing = [
        item["answer"]
        for item in bank
        if not is_answer_covered(item["answer"], terms)
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
