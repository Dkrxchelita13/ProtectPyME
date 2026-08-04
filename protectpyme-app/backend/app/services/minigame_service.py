# backend/app/services/minigame_service.py

from copy import deepcopy


# ============================================================
# HELPERS
# ============================================================

def normalize_topic(topic: str) -> str:
    topic = (topic or "phishing").lower().strip()

    aliases = {
        "password": "passwords",
        "passwords": "passwords",
        "malware": "malware",
        "phishing": "phishing",
        "wifi": "wifi",
        "network": "wifi"
    }

    return aliases.get(topic, "phishing")


def normalize_risk(risk: str) -> str:
    risk = (risk or "alto").lower().strip()

    aliases = {
        "alto": "alto",
        "high": "alto",

        "medio": "medio",
        "medium": "medio",

        "bajo": "bajo",
        "low": "bajo"
    }

    return aliases.get(risk, "alto")


# ============================================================
# QUIZ
# ============================================================

QUIZ = {
    "phishing": {
        "alto": [
            {
                "question": "¿Qué es phishing?",
                "options": [
                    "Un ataque por engaño",
                    "Un antivirus",
                    "Un firewall",
                    "Una red segura"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué debes hacer con un correo sospechoso?",
                "options": [
                    "Reportarlo",
                    "Abrir sus enlaces",
                    "Responderlo",
                    "Descargar sus archivos"
                ],
                "answer": 0
            }
        ],

        "medio": [
            {
                "question": "¿Qué elemento debes revisar en un correo sospechoso?",
                "options": [
                    "El dominio del remitente",
                    "El color del logotipo",
                    "La hora del mensaje",
                    "El tamaño del texto"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué indica un enlace sospechoso?",
                "options": [
                    "Puede dirigir a una página falsa",
                    "Siempre es seguro",
                    "Es obligatorio abrirlo",
                    "Es un archivo local"
                ],
                "answer": 0
            }
        ],

        "bajo": [
            {
                "question": "¿Qué protocolo ayuda a validar correos legítimos?",
                "options": [
                    "DMARC",
                    "FTP",
                    "USB",
                    "HTTP"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué es spear phishing?",
                "options": [
                    "Phishing dirigido a una persona u organización específica",
                    "Un antivirus avanzado",
                    "Una contraseña segura",
                    "Un tipo de red WiFi"
                ],
                "answer": 0
            }
        ]
    },

    "passwords": {
        "alto": [
            {
                "question": "¿Cómo debe ser una contraseña segura?",
                "options": [
                    "Larga y difícil de adivinar",
                    "123456",
                    "Tu nombre",
                    "Tu fecha de nacimiento"
                ],
                "answer": 0
            },
            {
                "question": "¿Por qué no debes reutilizar contraseñas?",
                "options": [
                    "Porque si una cuenta se filtra, otras también quedan en riesgo",
                    "Porque ocupan más espacio",
                    "Porque cambian solas",
                    "Porque bloquean el correo"
                ],
                "answer": 0
            }
        ],

        "medio": [
            {
                "question": "¿Qué es MFA?",
                "options": [
                    "Autenticación multifactor",
                    "Modo fácil de acceso",
                    "Malware falso activo",
                    "Módulo de firewall automático"
                ],
                "answer": 0
            },
            {
                "question": "¿Para qué sirve un gestor de contraseñas?",
                "options": [
                    "Para guardar contraseñas de forma segura",
                    "Para eliminar antivirus",
                    "Para abrir redes públicas",
                    "Para enviar spam"
                ],
                "answer": 0
            }
        ],

        "bajo": [
            {
                "question": "¿Qué es salting en contraseñas?",
                "options": [
                    "Agregar un valor aleatorio antes de generar el hash",
                    "Compartir la clave con otro usuario",
                    "Usar la misma contraseña",
                    "Eliminar la autenticación"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué es password spraying?",
                "options": [
                    "Probar una contraseña común en muchas cuentas",
                    "Cambiar el idioma del teclado",
                    "Usar un token físico",
                    "Crear una copia de seguridad"
                ],
                "answer": 0
            }
        ]
    },

    "malware": {
        "alto": [
            {
                "question": "¿Qué es malware?",
                "options": [
                    "Software malicioso",
                    "Una red segura",
                    "Un respaldo",
                    "Una contraseña"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué debes hacer con un USB desconocido?",
                "options": [
                    "No conectarlo sin autorización",
                    "Abrirlo inmediatamente",
                    "Copiar sus archivos",
                    "Compartirlo"
                ],
                "answer": 0
            }
        ],

        "medio": [
            {
                "question": "¿Qué es ransomware?",
                "options": [
                    "Malware que cifra o bloquea archivos",
                    "Un tipo de firewall",
                    "Una red WiFi",
                    "Un correo seguro"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué es spyware?",
                "options": [
                    "Software que recopila información del usuario",
                    "Un gestor de claves",
                    "Un protocolo web",
                    "Un cable de red"
                ],
                "answer": 0
            }
        ],

        "bajo": [
            {
                "question": "¿Qué es un rootkit?",
                "options": [
                    "Malware que busca ocultar su presencia en el sistema",
                    "Una copia de seguridad",
                    "Un correo legítimo",
                    "Una contraseña temporal"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué es una sandbox?",
                "options": [
                    "Entorno aislado para analizar archivos o programas",
                    "Un tipo de contraseña",
                    "Un cable USB",
                    "Un correo falso"
                ],
                "answer": 0
            }
        ]
    },

    "wifi": {
        "alto": [
            {
                "question": "¿Qué es una red WiFi pública?",
                "options": [
                    "Una red disponible para muchas personas",
                    "Una contraseña privada",
                    "Un antivirus",
                    "Un archivo seguro"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué debes evitar en una red pública?",
                "options": [
                    "Ingresar datos sensibles sin protección",
                    "Cerrar sesión",
                    "Usar HTTPS",
                    "Verificar el nombre de la red"
                ],
                "answer": 0
            }
        ],

        "medio": [
            {
                "question": "¿Para qué sirve una VPN?",
                "options": [
                    "Para proteger la conexión",
                    "Para crear virus",
                    "Para eliminar contraseñas",
                    "Para abrir correos sospechosos"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué es un hotspot falso?",
                "options": [
                    "Una red creada para engañar usuarios",
                    "Un antivirus",
                    "Un respaldo",
                    "Una contraseña larga"
                ],
                "answer": 0
            }
        ],

        "bajo": [
            {
                "question": "¿Qué es Evil Twin?",
                "options": [
                    "Un punto de acceso falso que imita una red legítima",
                    "Un gestor de contraseñas",
                    "Un firewall físico",
                    "Una copia segura"
                ],
                "answer": 0
            },
            {
                "question": "¿Qué mejora aporta WPA3?",
                "options": [
                    "Mayor seguridad en redes inalámbricas",
                    "Elimina la necesidad de contraseñas",
                    "Convierte correos en seguros",
                    "Bloquea todos los USB"
                ],
                "answer": 0
            }
        ]
    }
}


# ============================================================
# CROSSWORD
# ============================================================

CROSSWORD = {
    "phishing": {
        "alto": [
            {
                "clue": "Ataque por engaño para robar información",
                "answer": "PHISHING"
            },
            {
                "clue": "Acción correcta ante un correo sospechoso",
                "answer": "REPORTAR"
            },
            {
                "clue": "Elemento peligroso dentro de un correo fraudulento",
                "answer": "ENLACE"
            }
        ],

        "medio": [
            {
                "clue": "Parte del correo que ayuda a identificar al remitente",
                "answer": "DOMINIO"
            },
            {
                "clue": "Dirección web que debe revisarse antes de hacer clic",
                "answer": "URL"
            },
            {
                "clue": "Correo no deseado o sospechoso",
                "answer": "SPAM"
            }
        ],

        "bajo": [
            {
                "clue": "Mecanismo usado para validar correos legítimos",
                "answer": "DMARC"
            },
            {
                "clue": "Registro que ayuda a validar servidores de correo",
                "answer": "SPF"
            },
            {
                "clue": "Firma usada para verificar autenticidad del correo",
                "answer": "DKIM"
            }
        ]
    },

    "passwords": {
        "alto": [
            {
                "clue": "Clave usada para entrar a una cuenta",
                "answer": "PASSWORD"
            },
            {
                "clue": "Código o clave que no debe compartirse",
                "answer": "SECRETO"
            },
            {
                "clue": "Característica recomendada en una contraseña",
                "answer": "LARGA"
            }
        ],

        "medio": [
            {
                "clue": "Autenticación con más de un factor",
                "answer": "MFA"
            },
            {
                "clue": "Programa que almacena contraseñas seguras",
                "answer": "GESTOR"
            },
            {
                "clue": "Frase usada como contraseña larga",
                "answer": "PASSPHRASE"
            }
        ],

        "bajo": [
            {
                "clue": "Valor aleatorio agregado antes del hash",
                "answer": "SALT"
            },
            {
                "clue": "Resultado de aplicar una función criptográfica",
                "answer": "HASH"
            },
            {
                "clue": "Función moderna y resistente para almacenar contraseñas",
                "answer": "ARGON2ID"
            }
        ]
    },

    "malware": {
        "alto": [
            {
                "clue": "Software malicioso",
                "answer": "MALWARE"
            },
            {
                "clue": "Programa que protege contra virus",
                "answer": "ANTIVIRUS"
            },
            {
                "clue": "Dispositivo que puede propagar amenazas",
                "answer": "USB"
            }
        ],

        "medio": [
            {
                "clue": "Malware que cifra archivos",
                "answer": "RANSOMWARE"
            },
            {
                "clue": "Programa que espía al usuario",
                "answer": "SPYWARE"
            },
            {
                "clue": "Red de equipos infectados",
                "answer": "BOTNET"
            }
        ],

        "bajo": [
            {
                "clue": "Malware que intenta ocultarse en el sistema",
                "answer": "ROOTKIT"
            },
            {
                "clue": "Entorno aislado para analizar archivos",
                "answer": "SANDBOX"
            },
            {
                "clue": "Capacidad de mantenerse activo tras reinicio",
                "answer": "PERSISTENCIA"
            }
        ]
    },

    "wifi": {
        "alto": [
            {
                "clue": "Red inalámbrica de uso compartido",
                "answer": "WIFI"
            },
            {
                "clue": "Servicio que protege la conexión",
                "answer": "VPN"
            },
            {
                "clue": "Protocolo seguro para navegar",
                "answer": "HTTPS"
            }
        ],

        "medio": [
            {
                "clue": "Nombre visible de una red inalámbrica",
                "answer": "SSID"
            },
            {
                "clue": "Punto de acceso inalámbrico",
                "answer": "HOTSPOT"
            },
            {
                "clue": "Estándar de seguridad inalámbrica",
                "answer": "WPA2"
            }
        ],

        "bajo": [
            {
                "clue": "Red falsa que imita una legítima",
                "answer": "EVILTWIN"
            },
            {
                "clue": "Punto de acceso no autorizado",
                "answer": "ROGUEAP"
            },
            {
                "clue": "Estándar moderno de seguridad WiFi",
                "answer": "WPA3"
            }
        ]
    }
}


# ============================================================
# WORDSEARCH
# ============================================================

WORDSEARCH = {
    "phishing": {
        "alto": [
            {
                "clue": "Ataque para robar credenciales mediante engaños",
                "answer": "PHISHING"
            },
            {
                "clue": "Acción correcta ante un correo sospechoso",
                "answer": "REPORTAR"
            },
            {
                "clue": "Elemento peligroso dentro de un correo fraudulento",
                "answer": "ENLACE"
            }
        ],

        "medio": [
            {
                "clue": "Parte de la dirección que ayuda a identificar un sitio",
                "answer": "DOMINIO"
            },
            {
                "clue": "Dirección que debe verificarse antes de hacer clic",
                "answer": "URL"
            },
            {
                "clue": "Correo no deseado",
                "answer": "SPAM"
            }
        ],

        "bajo": [
            {
                "clue": "Validación avanzada de correos",
                "answer": "DMARC"
            },
            {
                "clue": "Registro de autorización de correo",
                "answer": "SPF"
            },
            {
                "clue": "Firma para correo electrónico",
                "answer": "DKIM"
            }
        ]
    },

    "passwords": {
        "alto": [
            {
                "clue": "Clave para acceder a una cuenta",
                "answer": "PASSWORD"
            },
            {
                "clue": "Contraseña con muchos caracteres",
                "answer": "LARGA"
            },
            {
                "clue": "Dato privado que no debe compartirse",
                "answer": "SECRETO"
            }
        ],

        "medio": [
            {
                "clue": "Autenticación en más de un paso",
                "answer": "MFA"
            },
            {
                "clue": "Programa para almacenar contraseñas",
                "answer": "GESTOR"
            },
            {
                "clue": "Frase usada como contraseña",
                "answer": "PASSPHRASE"
            }
        ],

        "bajo": [
            {
                "clue": "Valor aleatorio agregado antes de un hash",
                "answer": "SALT"
            },
            {
                "clue": "Resultado de una función criptográfica",
                "answer": "HASH"
            },
            {
                "clue": "Función moderna y resistente para almacenar contraseñas",
                "answer": "ARGON2ID"
            }
        ]
    },

    "malware": {
        "alto": [
            {
                "clue": "Software diseñado para dañar sistemas",
                "answer": "MALWARE"
            },
            {
                "clue": "Tipo de software malicioso",
                "answer": "VIRUS"
            },
            {
                "clue": "Programa que elimina software malicioso",
                "answer": "ANTIVIRUS"
            }
        ],

        "medio": [
            {
                "clue": "Malware que cifra archivos",
                "answer": "RANSOMWARE"
            },
            {
                "clue": "Programa que espía al usuario",
                "answer": "SPYWARE"
            },
            {
                "clue": "Red de equipos infectados",
                "answer": "BOTNET"
            }
        ],

        "bajo": [
            {
                "clue": "Malware oculto en el sistema",
                "answer": "ROOTKIT"
            },
            {
                "clue": "Entorno aislado de análisis",
                "answer": "SANDBOX"
            },
            {
                "clue": "Mantenerse activo en el sistema",
                "answer": "PERSISTENCIA"
            }
        ]
    },

    "wifi": {
        "alto": [
            {
                "clue": "Red inalámbrica",
                "answer": "WIFI"
            },
            {
                "clue": "Protege la conexión",
                "answer": "VPN"
            },
            {
                "clue": "Protocolo seguro de navegación",
                "answer": "HTTPS"
            }
        ],

        "medio": [
            {
                "clue": "Nombre de una red inalámbrica",
                "answer": "SSID"
            },
            {
                "clue": "Punto de acceso",
                "answer": "HOTSPOT"
            },
            {
                "clue": "Seguridad inalámbrica",
                "answer": "WPA2"
            }
        ],

        "bajo": [
            {
                "clue": "Red falsa que imita una legítima",
                "answer": "EVILTWIN"
            },
            {
                "clue": "Punto de acceso no autorizado",
                "answer": "ROGUEAP"
            },
            {
                "clue": "Estándar moderno de WiFi seguro",
                "answer": "WPA3"
            }
        ]
    }
}


# ============================================================
# SERVICE FUNCTIONS
# ============================================================

ANSWER_CONCEPT_IDS = {
    "PHISHING": ["phishing.phishing"],
    "REPORTAR": ["phishing.reportar"],
    "ENLACE": ["phishing.enlace"],
    "DOMINIO": ["phishing.dominio"],
    "URL": ["phishing.url"],
    "SPAM": ["phishing.spam"],
    "DMARC": ["phishing.dmarc"],
    "SPF": ["phishing.spf"],
    "DKIM": ["phishing.dkim"],
    "PASSWORD": ["passwords.password"],
    "SECRETO": ["passwords.secreto"],
    "LARGA": ["passwords.larga"],
    "MFA": ["passwords.mfa"],
    "GESTOR": ["passwords.gestor"],
    "PASSPHRASE": ["passwords.passphrase"],
    "SALT": ["passwords.salt"],
    "HASH": ["passwords.hash"],
    "ARGON2ID": ["passwords.argon2id"],
    "MALWARE": ["malware.malware"],
    "VIRUS": ["malware.virus"],
    "ANTIVIRUS": ["malware.antivirus"],
    "USB": ["malware.usb"],
    "RANSOMWARE": ["malware.ransomware"],
    "SPYWARE": ["malware.spyware"],
    "BOTNET": ["malware.botnet"],
    "ROOTKIT": ["malware.rootkit"],
    "SANDBOX": ["malware.sandbox"],
    "PERSISTENCIA": ["malware.persistencia"],
    "WIFI": ["wifi.wifi_publica"],
    "VPN": ["wifi.vpn"],
    "HTTPS": ["wifi.https"],
    "SSID": ["wifi.ssid"],
    "HOTSPOT": ["wifi.hotspot"],
    "WPA2": ["wifi.wpa2"],
    "EVILTWIN": ["wifi.evil_twin"],
    "ROGUEAP": ["wifi.rogue_ap"],
    "WPA3": ["wifi.wpa3"],
}


QUIZ_CONCEPT_IDS = {
    "phishing": {
        "alto": [
            ["phishing.phishing"],
            ["phishing.reportar"],
        ],
        "medio": [
            ["phishing.dominio"],
            ["phishing.enlace", "phishing.url"],
        ],
        "bajo": [
            ["phishing.dmarc", "phishing.spf", "phishing.dkim"],
            ["phishing.spear_phishing"],
        ],
    },
    "passwords": {
        "alto": [
            ["passwords.password", "passwords.larga"],
            ["passwords.reutilizacion"],
        ],
        "medio": [
            ["passwords.mfa"],
            ["passwords.gestor"],
        ],
        "bajo": [
            ["passwords.salt", "passwords.hash"],
            ["passwords.password_spraying"],
        ],
    },
    "malware": {
        "alto": [
            ["malware.malware"],
            ["malware.usb"],
        ],
        "medio": [
            ["malware.ransomware"],
            ["malware.spyware"],
        ],
        "bajo": [
            ["malware.rootkit"],
            ["malware.sandbox"],
        ],
    },
    "wifi": {
        "alto": [
            ["wifi.wifi_publica"],
            ["wifi.datos_sensibles"],
        ],
        "medio": [
            ["wifi.vpn"],
            ["wifi.hotspot"],
        ],
        "bajo": [
            ["wifi.evil_twin"],
            ["wifi.wpa3"],
        ],
    },
}


def _slug(value):
    return "".join(
        character.lower()
        for character in str(value)
        if character.isalnum()
    )


def _set_concept_metadata(item, concept_ids):
    if len(concept_ids) == 1:
        item["concept_id"] = concept_ids[0]
        item.pop("concept_ids", None)
    else:
        item["concept_ids"] = list(concept_ids)
        item.pop("concept_id", None)


def get_item_concept_ids(item):
    concept_ids = item.get("concept_ids")

    if concept_ids is None:
        concept_id = item.get("concept_id")
        concept_ids = [concept_id] if concept_id else []

    if not concept_ids:
        raise ValueError(f"Minigame item has no concept ids: {item.get('item_id')}")

    return list(concept_ids)


def _annotate_quiz_bank():
    for topic, risks in QUIZ.items():
        for risk, items in risks.items():
            for index, item in enumerate(items):
                concept_ids = QUIZ_CONCEPT_IDS[topic][risk][index]
                item["item_id"] = f"{topic}_{risk}_quiz_{index + 1}"
                item["difficulty"] = risk
                _set_concept_metadata(item, concept_ids)


def _annotate_word_bank(bank, minigame):
    for topic, risks in bank.items():
        for risk, items in risks.items():
            for item in items:
                answer = item["answer"]
                concept_ids = ANSWER_CONCEPT_IDS[answer]
                item["item_id"] = f"{topic}_{risk}_{minigame}_{_slug(answer)}"
                item["difficulty"] = risk
                _set_concept_metadata(item, concept_ids)


def _annotate_banks():
    _annotate_quiz_bank()
    _annotate_word_bank(CROSSWORD, "crossword")
    _annotate_word_bank(WORDSEARCH, "wordsearch")


_annotate_banks()

ITEM_INDEX = {}


def _build_item_index():
    index = {}

    banks = (
        ("quiz", QUIZ),
        ("crossword", CROSSWORD),
        ("wordsearch", WORDSEARCH),
    )

    for minigame, bank in banks:
        for topic, risks in bank.items():
            for risk, items in risks.items():
                for item in items:
                    item_id = item["item_id"]

                    if item_id in index:
                        raise ValueError(f"Duplicated minigame item_id: {item_id}")

                    index[item_id] = {
                        "topic": topic,
                        "risk": risk,
                        "minigame": minigame,
                        "item": deepcopy(item),
                    }

    return index


def get_item_by_id(item_id):
    if not ITEM_INDEX:
        ITEM_INDEX.update(_build_item_index())

    try:
        return deepcopy(ITEM_INDEX[item_id])
    except KeyError as exc:
        raise KeyError(f"Minigame item not found: {item_id}") from exc


def get_quiz(topic, risk):
    topic = normalize_topic(topic)
    risk = normalize_risk(risk)

    return QUIZ.get(
        topic,
        QUIZ["phishing"]
    ).get(
        risk,
        QUIZ["phishing"]["alto"]
    )


def get_crossword(topic, risk):
    topic = normalize_topic(topic)
    risk = normalize_risk(risk)

    return CROSSWORD.get(
        topic,
        CROSSWORD["phishing"]
    ).get(
        risk,
        CROSSWORD["phishing"]["alto"]
    )


def get_wordsearch(topic, risk):
    topic = normalize_topic(topic)
    risk = normalize_risk(risk)

    return WORDSEARCH.get(
        topic,
        WORDSEARCH["phishing"]
    ).get(
        risk,
        WORDSEARCH["phishing"]["alto"]
    )
