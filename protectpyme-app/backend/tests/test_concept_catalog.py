import pytest

from app.services import concept_catalog


def test_concept_catalog_is_valid():
    assert concept_catalog.validate_concept_catalog() is True


def test_all_catalog_entries_have_required_fields():
    for concept in concept_catalog.CONCEPT_CATALOG.values():
        assert concept_catalog.REQUIRED_FIELDS == set(concept)
        for field in concept_catalog.REQUIRED_FIELDS - {"aliases"}:
            assert str(concept[field]).strip()
        assert concept["aliases"]
        assert all(str(alias).strip() for alias in concept["aliases"])


def test_concept_ids_are_unique():
    keys = list(concept_catalog.CONCEPT_CATALOG)
    values = [
        concept["concept_id"]
        for concept in concept_catalog.CONCEPT_CATALOG.values()
    ]

    assert len(keys) == len(set(keys))
    assert keys == values


def test_aliases_are_not_ambiguous():
    owners = {}

    for concept_id, concept in concept_catalog.CONCEPT_CATALOG.items():
        for alias in concept["aliases"]:
            key = alias.strip().lower()
            previous_owner = owners.get(key)

            assert previous_owner in (None, concept_id)
            owners[key] = concept_id


def test_get_concepts_preserves_order_and_removes_duplicates():
    concepts = concept_catalog.get_concepts([
        "passwords.hash",
        "passwords.salt",
        "passwords.hash",
        "passwords.argon2id",
    ])

    assert [
        concept["concept_id"]
        for concept in concepts
    ] == [
        "passwords.hash",
        "passwords.salt",
        "passwords.argon2id",
    ]


def test_get_concept_fails_clearly_for_unknown_id():
    with pytest.raises(KeyError, match="Concept id not found"):
        concept_catalog.get_concept("passwords.argon")


def test_passwords_argon2id_has_complete_teaching_content():
    concept = concept_catalog.get_concept("passwords.argon2id")
    searchable = " ".join(
        str(concept[field]).lower()
        for field in (
            "definition",
            "explanation",
            "practical_example",
            "common_mistake",
            "recognition_clue",
        )
    )

    assert concept["definition"].strip()
    assert concept["explanation"].strip()
    assert "almacenar contrasenas" in searchable
    assert "salt" in searchable
    assert "cifrado reversible" in searchable
    assert concept["common_mistake"].strip()
    assert "funcion moderna" in concept["recognition_clue"].lower()
