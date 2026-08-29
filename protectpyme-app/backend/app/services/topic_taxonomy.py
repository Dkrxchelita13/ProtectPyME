from dataclasses import dataclass
import unicodedata


CANONICAL_TOPICS = ("phishing", "passwords", "malware", "wifi")
FINAL_FALLBACK_TOPIC = "phishing"

TOPIC_ALIASES = {
    "password": "passwords",
    "passwords": "passwords",
    "contrasena": "passwords",
    "contrasenas": "passwords",
    "contrasenas_seguras": "passwords",
    "malicious_software": "malware",
    "network": "wifi",
    "redes_wifi": "wifi",
    "wifi_publico": "wifi",
    "social_engineering": "phishing",
}

PLAYABLE_SCENARIOS_BY_TOPIC = {
    "phishing": (1, 5),
    "passwords": (2, 6),
    "malware": (3,),
    "wifi": (7,),
}

PLAYABLE_SCENARIO_IDS = frozenset(
    scenario_id
    for scenario_ids in PLAYABLE_SCENARIOS_BY_TOPIC.values()
    for scenario_id in scenario_ids
)

HISTORICAL_NON_PLAYABLE_SCENARIO_IDS = frozenset({4})

SCENARIO_TOPIC_MAP = {
    1: "phishing",
    2: "passwords",
    3: "malware",
    4: "wifi",
    5: "phishing",
    6: "passwords",
    7: "wifi",
}

RF_CATEGORY_BY_TOPIC = {
    "phishing": "phishing",
    "passwords": "password",
    "wifi": "wifi",
}

RF_FALLBACK_CATEGORY = "phishing"


@dataclass(frozen=True)
class RFCategoryMapping:
    original_topic: str | None
    canonical_topic: str | None
    rf_category: str
    used_fallback: bool
    reason: str


def normalize_topic(value: str | None) -> str | None:
    normalized = normalize_topic_text(value)

    if not normalized or normalized in ("general", "unknown", "none"):
        return None

    if normalized in CANONICAL_TOPICS:
        return normalized

    return TOPIC_ALIASES.get(normalized)


def normalize_topic_text(value: str | None) -> str:
    raw = str(value or "").strip().lower()
    normalized = unicodedata.normalize("NFKD", raw)

    return "".join(
        character
        for character in normalized
        if not unicodedata.combining(character)
    )


def get_playable_scenarios(topic: str | None) -> tuple[int, ...]:
    canonical_topic = normalize_topic(topic)

    if canonical_topic is None:
        return ()

    return PLAYABLE_SCENARIOS_BY_TOPIC.get(canonical_topic, ())


def get_default_playable_scenario(topic: str | None) -> int | None:
    candidates = get_playable_scenarios(topic)

    if not candidates:
        return None

    return min(candidates)


def get_topic_for_scenario(scenario_id) -> str | None:
    try:
        scenario_id = int(scenario_id)
    except (TypeError, ValueError):
        return None

    return SCENARIO_TOPIC_MAP.get(scenario_id)


def is_playable_scenario(scenario_id) -> bool:
    try:
        scenario_id = int(scenario_id)
    except (TypeError, ValueError):
        return False

    return scenario_id in PLAYABLE_SCENARIO_IDS


def to_rf_category(value: str | None) -> RFCategoryMapping:
    canonical_topic = normalize_topic(value)

    if canonical_topic in RF_CATEGORY_BY_TOPIC:
        return RFCategoryMapping(
            original_topic=value,
            canonical_topic=canonical_topic,
            rf_category=RF_CATEGORY_BY_TOPIC[canonical_topic],
            used_fallback=False,
            reason="mapped",
        )

    if canonical_topic == "malware":
        return RFCategoryMapping(
            original_topic=value,
            canonical_topic=canonical_topic,
            rf_category=RF_FALLBACK_CATEGORY,
            used_fallback=True,
            reason="malware_without_rf_category",
        )

    return RFCategoryMapping(
        original_topic=value,
        canonical_topic=canonical_topic,
        rf_category=RF_FALLBACK_CATEGORY,
        used_fallback=True,
        reason="unknown_topic",
    )
