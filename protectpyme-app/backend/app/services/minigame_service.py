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
    "VERIFICAR": ["passwords.identity_verification"],
    "IDENTIDAD": ["passwords.identity_verification"],
    "CREDENCIAL": ["passwords.credential_request"],
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
    "TRAFICO": ["wifi.suspicious_traffic"],
    "ALERTA": ["wifi.suspicious_traffic"],
    "BLOQUEAR": ["wifi.suspicious_traffic"],
    "EXFILTRACION": ["wifi.data_exfiltration"],
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


EXTRA_QUIZ_ITEMS = {
    ("phishing", "alto"): [
        (
            "phishing_alto_quiz_enlace_accion_1",
            "Un proveedor envía un enlace inesperado para actualizar datos. ¿Qué conviene hacer?",
            ["Abrirlo porque parece urgente", "Ignorarlo sin avisar", "Verificar por canal oficial antes de entrar", "Compartirlo con el equipo"],
            2,
            ["phishing.enlace", "phishing.phishing"],
        ),
        (
            "phishing_alto_quiz_urgencia_1",
            "¿Qué señal suele aumentar la sospecha en un correo fraudulento?",
            ["Que tenga saludo cordial", "Que presione para actuar de inmediato", "Que llegue por la mañana", "Que use texto corto"],
            1,
            ["phishing.phishing"],
        ),
        (
            "phishing_alto_quiz_reporte_pyme_1",
            "En una PYME, varias personas reciben el mismo correo dudoso. ¿Cuál es la mejor respuesta?",
            ["Responder al remitente", "Descargar el adjunto", "Borrar el mensaje en silencio", "Reportarlo al canal interno"],
            3,
            ["phishing.reportar"],
        ),
    ],
    ("phishing", "medio"): [
        (
            "phishing_medio_quiz_dominio_similar_1",
            "¿Cuál dominio debería revisarse con más cuidado?",
            ["empresa.com", "empresa-soporte.com", "portal interno conocido", "proveedor validado"],
            1,
            ["phishing.dominio"],
        ),
        (
            "phishing_medio_quiz_url_visible_1",
            "Un botón dice 'factura', pero la URL apunta a un sitio desconocido. ¿Qué indica?",
            ["Que siempre es seguro", "Que es un archivo local", "Que puede ser un enlace engañoso", "Que no requiere revisión"],
            2,
            ["phishing.url", "phishing.enlace"],
        ),
        (
            "phishing_medio_quiz_spam_riesgo_1",
            "¿Por qué no conviene tratar todo spam como simple publicidad?",
            ["Porque puede ocultar enlaces o adjuntos riesgosos", "Porque siempre bloquea la cuenta", "Porque elimina mensajes legítimos", "Porque cambia la contraseña"],
            0,
            ["phishing.spam"],
        ),
    ],
    ("phishing", "bajo"): [
        (
            "phishing_bajo_quiz_spf_1",
            "¿Qué valida SPF en el correo de una empresa?",
            ["El color del logotipo", "Qué servidores pueden enviar por un dominio", "La longitud del asunto", "La contraseña del usuario"],
            1,
            ["phishing.spf"],
        ),
        (
            "phishing_bajo_quiz_dkim_1",
            "¿Para qué ayuda DKIM?",
            ["Para borrar spam", "Para cifrar archivos", "Para verificar autenticidad e integridad del correo", "Para crear contraseñas"],
            2,
            ["phishing.dkim"],
        ),
        (
            "phishing_bajo_quiz_spear_contexto_1",
            "Un correo menciona datos reales de tu jefe y pide una factura urgente. ¿Qué riesgo representa?",
            ["Spam común sin riesgo", "Error de red", "Actualización normal", "Spear phishing"],
            3,
            ["phishing.spear_phishing"],
        ),
    ],
    ("passwords", "alto"): [
        (
            "passwords_alto_quiz_unica_1",
            "¿Qué práctica reduce el impacto si una cuenta externa se filtra?",
            ["Usar una contraseña única por servicio", "Compartir la clave con soporte", "Usar el nombre de la empresa", "Anotar la clave en un chat"],
            0,
            ["passwords.password", "passwords.reutilizacion"],
        ),
        (
            "passwords_alto_quiz_secreto_1",
            "¿Qué dato debe tratarse como secreto?",
            ["El horario de oficina", "El código temporal de acceso", "El nombre del área", "La marca del equipo"],
            1,
            ["passwords.secreto"],
        ),
        (
            "passwords_alto_quiz_larga_1",
            "¿Cuál opción es mejor para una contraseña laboral?",
            ["Una palabra común con 1", "La fecha de nacimiento", "Una frase larga y difícil de adivinar", "El nombre del negocio"],
            2,
            ["passwords.larga"],
        ),
    ],
    ("passwords", "medio"): [
        (
            "passwords_medio_quiz_mfa_solicitud_1",
            "Recibes una solicitud MFA que no iniciaste. ¿Qué debes hacer?",
            ["Aprobarla para cerrar la alerta", "Ignorarla sin avisar", "Compartir el código", "Rechazarla y reportarla"],
            3,
            ["passwords.mfa"],
        ),
        (
            "passwords_medio_quiz_gestor_1",
            "¿Qué ventaja aporta un gestor de contraseñas?",
            ["Generar y guardar claves únicas", "Eliminar la necesidad de MFA", "Hacer públicas las claves", "Cambiar el correo"],
            0,
            ["passwords.gestor"],
        ),
        (
            "passwords_medio_quiz_passphrase_1",
            "¿Cuándo una passphrase es más segura?",
            ["Cuando usa una frase famosa", "Cuando es larga, única y no obvia", "Cuando incluye el nombre del usuario", "Cuando se comparte con el equipo"],
            1,
            ["passwords.passphrase"],
        ),
        (
            "passwords_medio_quiz_llamada_soporte_1",
            "Una llamada dice ser de soporte y pide tu contraseña. ¿Qué haces?",
            ["Entregarla para resolver rápido", "Verificar por canal oficial antes de responder", "Dictar solo una parte", "Cambiar de tema"],
            1,
            ["passwords.credential_request", "passwords.identity_verification"],
        ),
        (
            "passwords_medio_quiz_codigo_temporal_1",
            "¿Qué debes hacer si alguien solicita un código temporal por teléfono?",
            ["Compartirlo si parece urgente", "Enviarlo por chat interno", "No compartirlo y reportar la solicitud", "Publicarlo para que soporte lo vea"],
            2,
            ["passwords.credential_request"],
        ),
        (
            "passwords_medio_quiz_canal_oficial_1",
            "¿Cómo confirmas una solicitud sensible de credenciales?",
            ["Usando el enlace que llegó en el mensaje", "Por un canal oficial independiente", "Preguntando al mismo número desconocido", "Aceptando si usa el logo correcto"],
            1,
            ["passwords.identity_verification"],
        ),
        (
            "passwords_medio_quiz_reporte_ti_1",
            "Si recibes una petición sospechosa de contraseña, ¿cuál es la respuesta segura?",
            ["Reportarla a TI", "Probar si la clave funciona", "Responder con la clave anterior", "Ignorarla sin avisar"],
            0,
            ["passwords.credential_request", "passwords.identity_verification"],
        ),
    ],
    ("passwords", "bajo"): [
        (
            "passwords_bajo_quiz_argon2id_1",
            "¿Qué describe mejor a Argon2id?",
            ["Un cifrado reversible", "Un antivirus", "Una función para almacenar contraseñas de forma resistente", "Una red WiFi"],
            2,
            ["passwords.argon2id"],
        ),
        (
            "passwords_bajo_quiz_hash_no_reversible_1",
            "¿Qué error conviene evitar al hablar de hash de contraseñas?",
            ["Compararlo con el valor almacenado", "Usarlo junto con salt", "Aplicarlo antes de guardar", "Pensar que permite recuperar la contraseña"],
            3,
            ["passwords.hash"],
        ),
        (
            "passwords_bajo_quiz_spraying_1",
            "¿Qué caracteriza al password spraying?",
            ["Probar una contraseña común en muchas cuentas", "Cifrar una USB", "Firmar un correo", "Crear una VPN"],
            0,
            ["passwords.password_spraying"],
        ),
    ],
    ("malware", "alto"): [
        (
            "malware_alto_quiz_adjunto_1",
            "Llega un adjunto inesperado de un contacto conocido. ¿Qué acción reduce el riesgo?",
            ["Abrirlo de inmediato", "Verificar el origen antes de abrir", "Reenviarlo a todos", "Desactivar el antivirus"],
            1,
            ["malware.malware"],
        ),
        (
            "malware_alto_quiz_antivirus_alerta_1",
            "El antivirus alerta sobre una descarga. ¿Qué conviene hacer?",
            ["Ignorar la alerta", "Ejecutar el archivo", "Detenerse y pedir apoyo", "Copiarlo a otra carpeta"],
            2,
            ["malware.antivirus"],
        ),
        (
            "malware_alto_quiz_usb_soporte_1",
            "Encuentras una USB en recepción. ¿Cuál es la decisión más segura?",
            ["Conectarla para revisar dueño", "Copiar su contenido", "Prestarla a un compañero", "Entregarla a soporte sin conectarla"],
            3,
            ["malware.usb"],
        ),
    ],
    ("malware", "medio"): [
        (
            "malware_medio_quiz_ransomware_impacto_1",
            "¿Cuál es un impacto probable del ransomware?",
            ["Bloquear o cifrar archivos de trabajo", "Mejorar el rendimiento", "Validar correos", "Crear contraseñas"],
            0,
            ["malware.ransomware"],
        ),
        (
            "malware_medio_quiz_spyware_credenciales_1",
            "¿Por qué el spyware es peligroso para una PYME?",
            ["Porque ordena archivos", "Porque puede recopilar credenciales o actividad", "Porque cambia el fondo de pantalla", "Porque mejora la red"],
            1,
            ["malware.spyware"],
        ),
        (
            "malware_medio_quiz_botnet_red_1",
            "Un equipo envía tráfico extraño sin que el usuario lo note. ¿Qué podría indicar?",
            ["Una impresora nueva", "Una clave larga", "Participación en una botnet", "Un correo legítimo"],
            2,
            ["malware.botnet"],
        ),
    ],
    ("malware", "bajo"): [
        (
            "malware_bajo_quiz_rootkit_oculto_1",
            "¿Qué vuelve delicado a un rootkit?",
            ["Que solo afecta correos", "Que crea contraseñas", "Que siempre es visible", "Que intenta ocultar su presencia"],
            3,
            ["malware.rootkit"],
        ),
        (
            "malware_bajo_quiz_sandbox_analisis_1",
            "¿Para qué sirve una sandbox autorizada?",
            ["Analizar archivos sospechosos de forma aislada", "Compartir claves", "Evitar respaldos", "Abrir cualquier enlace"],
            0,
            ["malware.sandbox"],
        ),
        (
            "malware_bajo_quiz_persistencia_1",
            "Si una amenaza reaparece tras reiniciar, ¿qué concepto ayuda a explicarlo?",
            ["Spam", "Persistencia", "MFA", "SSID"],
            1,
            ["malware.persistencia"],
        ),
    ],
    ("wifi", "alto"): [
        (
            "wifi_alto_quiz_publica_tarea_1",
            "¿Qué conviene evitar en una red pública?",
            ["Consultar datos sensibles sin protección", "Confirmar el SSID", "Usar HTTPS", "Activar VPN autorizada"],
            0,
            ["wifi.wifi_publica", "wifi.datos_sensibles"],
        ),
        (
            "wifi_alto_quiz_https_limite_1",
            "¿Qué límite tiene HTTPS?",
            ["Protege la comunicación, pero no prueba que el sitio sea legítimo", "Elimina todo phishing", "Cambia la contraseña", "Bloquea USB"],
            0,
            ["wifi.https"],
        ),
        (
            "wifi_alto_quiz_vpn_uso_1",
            "¿Cuándo es recomendable activar la VPN corporativa?",
            ["Al conectar una USB", "Antes de entrar a recursos internos desde una red externa", "Para abrir adjuntos sospechosos", "Para cambiar el SSID"],
            1,
            ["wifi.vpn"],
        ),
    ],
    ("wifi", "medio"): [
        (
            "wifi_medio_quiz_ssid_parecido_1",
            "Dos redes tienen nombres muy parecidos. ¿Qué conviene revisar?",
            ["El color del icono", "La hora del día", "El SSID oficial antes de conectarse", "La batería del equipo"],
            2,
            ["wifi.ssid"],
        ),
        (
            "wifi_medio_quiz_hotspot_1",
            "¿Qué debe confirmarse antes de usar un hotspot para trabajo?",
            ["Que sea autorizado y confiable", "Que no tenga nombre", "Que sea el primero de la lista", "Que no use clave"],
            0,
            ["wifi.hotspot"],
        ),
        (
            "wifi_medio_quiz_wpa2_1",
            "¿Qué indica WPA2 en una red de oficina?",
            ["Que no necesita administración", "Que usa un estándar de protección inalámbrica", "Que siempre es pública", "Que evita correos falsos"],
            1,
            ["wifi.wpa2"],
        ),
        (
            "wifi_medio_quiz_trafico_saliente_1",
            "Una alerta muestra tráfico saliente inusual desde un equipo. ¿Qué indica?",
            ["Actividad que debe revisarse", "Que todo funciona mejor", "Que la contraseña es larga", "Que el correo es legítimo"],
            0,
            ["wifi.suspicious_traffic"],
        ),
        (
            "wifi_medio_quiz_transferencia_anormal_1",
            "Un sistema intenta enviar muchos archivos a un destino no reconocido. ¿Qué riesgo existe?",
            ["Exfiltración de datos", "Mejora de señal WiFi", "Cambio de idioma", "Validación SPF"],
            0,
            ["wifi.data_exfiltration", "wifi.suspicious_traffic"],
        ),
        (
            "wifi_medio_quiz_bloquear_alerta_1",
            "Ante una conexión sospechosa con transferencia anormal, ¿qué acción es más segura?",
            ["Ignorarla si no hay quejas", "Revisar, bloquear o aislar y reportar", "Compartir la red con más usuarios", "Desactivar MFA"],
            1,
            ["wifi.suspicious_traffic", "wifi.data_exfiltration"],
        ),
        (
            "wifi_medio_quiz_conexion_no_reconocida_1",
            "¿Qué debe hacerse con una conexión no reconocida que mueve datos sensibles?",
            ["Permitirla siempre", "Revisarla antes de continuar", "Cambiar el color del portal", "Abrir un adjunto"],
            1,
            ["wifi.suspicious_traffic", "wifi.data_exfiltration"],
        ),
    ],
    ("wifi", "bajo"): [
        (
            "wifi_bajo_quiz_evil_twin_1",
            "¿Qué busca una red Evil Twin?",
            ["Mejorar la señal", "Actualizar el router", "Imitar una red legítima para engañar", "Crear una contraseña larga"],
            2,
            ["wifi.evil_twin"],
        ),
        (
            "wifi_bajo_quiz_rogue_ap_1",
            "¿Qué problema representa un Rogue AP en la empresa?",
            ["Es un punto de acceso no autorizado", "Es un antivirus", "Es una firma de correo", "Es un hash seguro"],
            0,
            ["wifi.rogue_ap"],
        ),
        (
            "wifi_bajo_quiz_wpa3_1",
            "¿Cómo debe interpretarse WPA3?",
            ["Como permiso para compartir claves", "Como reemplazo de toda política", "Como razón para ignorar actualizaciones", "Como estándar moderno que mejora la seguridad WiFi"],
            3,
            ["wifi.wpa3"],
        ),
    ],
}


EXTRA_WORD_ITEMS = {
    ("phishing", "alto"): [
        ("engano", "ENGANO", "phishing.phishing", "Señal de manipulación para robar información"),
        ("canal", "CANAL", "phishing.reportar", "Medio interno usado para avisar de un correo sospechoso"),
    ],
    ("phishing", "medio"): [
        ("pagina", "PAGINA", "phishing.url", "Destino web que debe coincidir con el servicio esperado"),
        ("remitente", "REMITENTE", "phishing.dominio", "Origen del correo que debe verificarse"),
    ],
    ("phishing", "bajo"): [
        ("politica", "POLITICA", "phishing.dmarc", "Regla de correo que decide cómo tratar mensajes sospechosos"),
        ("dirigido", "DIRIGIDO", "phishing.spear_phishing", "Ataque enfocado en una persona o área específica"),
    ],
    ("passwords", "alto"): [
        ("privada", "PRIVADA", "passwords.password", "Característica de una clave que no debe compartirse"),
        ("repetir", "REPETIR", "passwords.reutilizacion", "Acción riesgosa al usar la misma clave en varios servicios"),
    ],
    ("passwords", "medio"): [
        ("factor", "FACTOR", "passwords.mfa", "Elemento adicional para verificar un acceso"),
        ("claves", "CLAVES", "passwords.gestor", "Datos que un gestor ayuda a guardar de forma segura"),
        ("verificar", "VERIFICAR", "passwords.identity_verification", "Confirmar una solicitud sensible por canal oficial"),
        ("identidad", "IDENTIDAD", "passwords.identity_verification", "Persona solicitante que debe confirmarse por un canal oficial"),
        ("credencial", "CREDENCIAL", "passwords.credential_request", "Dato de autenticación que no debe entregarse ante una solicitud sospechosa"),
    ],
    ("passwords", "bajo"): [
        ("spraying", "SPRAYING", "passwords.password_spraying", "Prueba de una contraseña común en muchas cuentas"),
        ("memoria", "MEMORIA", "passwords.argon2id", "Recurso que Argon2id usa para resistir intentos masivos"),
    ],
    ("malware", "alto"): [
        ("usb", "USB", "malware.usb", "Dispositivo externo que puede transportar amenazas"),
        ("alerta", "ALERTA", "malware.antivirus", "Aviso que no debe ignorarse ante una descarga riesgosa"),
    ],
    ("malware", "medio"): [
        ("cifra", "CIFRA", "malware.ransomware", "Acción de bloquear archivos para exigir pago"),
        ("espia", "ESPIA", "malware.spyware", "Acción de recopilar información sin autorización"),
    ],
    ("malware", "bajo"): [
        ("ocultar", "OCULTAR", "malware.rootkit", "Acción que dificulta detectar una amenaza"),
        ("aislado", "AISLADO", "malware.sandbox", "Forma segura de analizar archivos sospechosos"),
    ],
    ("wifi", "alto"): [
        ("datos", "DATOS", "wifi.datos_sensibles", "Información que requiere protección en redes externas"),
        ("publica", "PUBLICA", "wifi.wifi_publica", "Tipo de red compartida fuera del control de la empresa"),
    ],
    ("wifi", "medio"): [
        ("red", "RED", "wifi.ssid", "Conexión cuyo nombre visible debe verificarse"),
        ("clave", "CLAVE", "wifi.wpa2", "Secreto que protege el acceso a una red inalámbrica"),
        ("trafico", "TRAFICO", "wifi.suspicious_traffic", "Flujo de red que puede mostrar actividad anómala"),
        ("alerta", "ALERTA", "wifi.suspicious_traffic", "Aviso que debe revisarse ante conexiones inusuales"),
        ("bloquear", "BLOQUEAR", "wifi.suspicious_traffic", "Acción segura ante tráfico saliente sospechoso"),
        ("exfiltracion", "EXFILTRACION", "wifi.data_exfiltration", "Salida no autorizada de información desde un sistema"),
    ],
    ("wifi", "bajo"): [
        ("falsa", "FALSA", "wifi.evil_twin", "Característica de una red que imita a otra legítima"),
        ("moderno", "MODERNO", "wifi.wpa3", "Cualidad del estándar WPA3 frente a controles anteriores"),
    ],
}


def _quiz_item(item_id, question, options, answer, concept_ids, difficulty):
    item = {
        "item_id": item_id,
        "question": question,
        "options": list(options),
        "answer": answer,
        "difficulty": difficulty,
    }
    _set_concept_metadata(item, concept_ids)
    return item


def _word_item(topic, risk, minigame, suffix, answer, concept_id, clue):
    return {
        "item_id": f"{topic}_{risk}_{minigame}_{suffix}",
        "answer": answer,
        "clue": clue,
        "difficulty": risk,
        "concept_id": concept_id,
    }


def _word_item_for_minigame(topic, risk, minigame, suffix, answer, concept_id, clue):
    if (
        minigame == "wordsearch"
        and concept_id == "wifi.data_exfiltration"
        and answer == "EXFILTRACION"
    ):
        answer = "FUGADATOS"
        clue = "Fuga de datos no autorizada desde un sistema"

    return _word_item(
        topic=topic,
        risk=risk,
        minigame=minigame,
        suffix=f"{suffix}_variant_2",
        answer=answer,
        concept_id=concept_id,
        clue=clue,
    )


def _expand_quiz_bank():
    for (topic, risk), items in EXTRA_QUIZ_ITEMS.items():
        QUIZ[topic][risk].extend(
            _quiz_item(
                item_id=item_id,
                question=question,
                options=options,
                answer=answer,
                concept_ids=concept_ids,
                difficulty=risk,
            )
            for item_id, question, options, answer, concept_ids in items
        )


def _expand_word_bank(bank, minigame):
    for (topic, risk), items in EXTRA_WORD_ITEMS.items():
        pool_items = list(items)

        if minigame == "crossword" and topic == "malware" and risk == "alto":
            pool_items[0] = (
                "virus",
                "VIRUS",
                "malware.virus",
                "Tipo de malware que puede propagarse al ejecutar archivos",
            )

        bank[topic][risk].extend(
            _word_item_for_minigame(
                topic=topic,
                risk=risk,
                minigame=minigame,
                suffix=suffix,
                answer=answer,
                concept_id=concept_id,
                clue=clue,
            )
            for suffix, answer, concept_id, clue in pool_items
        )


def _replace_word_item_answer(bank, item_id, answer, clue):
    for risks in bank.values():
        for items in risks.values():
            for item in items:
                if item["item_id"] == item_id:
                    item["answer"] = answer
                    item["clue"] = clue
                    return

    raise KeyError(f"Minigame item not found: {item_id}")


def _expand_banks():
    _replace_word_item_answer(
        WORDSEARCH,
        "malware_bajo_wordsearch_persistencia",
        "REINICIO",
        "Señal de una amenaza que vuelve después de apagar y encender",
    )
    _replace_word_item_answer(
        CROSSWORD,
        "malware_bajo_crossword_persistencia",
        "REINICIO",
        "Indicio de malware que reaparece tras reiniciar",
    )
    _expand_quiz_bank()
    _expand_word_bank(WORDSEARCH, "wordsearch")
    _expand_word_bank(CROSSWORD, "crossword")


_expand_banks()

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
