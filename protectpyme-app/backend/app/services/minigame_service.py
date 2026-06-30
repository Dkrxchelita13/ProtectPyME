CROSSWORD = {

    "phishing": [

        {
            "clue": "Correo fraudulento que busca robar información",
            "answer": "PHISHING"
        },
        {
            "clue": "Información secreta que nunca debes compartir",
            "answer": "PASSWORD"
        },
        {
            "clue": "Programa para navegar de forma segura en redes públicas",
            "answer": "VPN"
        },
        {
            "clue": "Acción recomendada ante un correo sospechoso",
            "answer": "REPORTAR"
        }

    ],

    "passwords": [

        {
            "clue": "Método de autenticación con dos pasos",
            "answer": "MFA"
        },
        {
            "clue": "Conjunto de caracteres para acceder a una cuenta",
            "answer": "PASSWORD"
        },
        {
            "clue": "Programa que almacena contraseñas de forma segura",
            "answer": "GESTOR"
        },
        {
            "clue": "Caracteres usados para fortalecer una contraseña",
            "answer": "SIMBOLOS"
        }

    ],

    "malware": [

        {
            "clue": "Software diseñado para dañar un sistema",
            "answer": "MALWARE"
        },
        {
            "clue": "Tipo de malware que cifra archivos",
            "answer": "RANSOMWARE"
        },
        {
            "clue": "Programa que detecta y elimina virus",
            "answer": "ANTIVIRUS"
        },
        {
            "clue": "Dispositivo que puede propagar malware si es desconocido",
            "answer": "USB"
        }

    ]
}
def get_crossword(topic):

    return CROSSWORD.get(
        topic,
        CROSSWORD["phishing"]
    )

#}
# def get_crossword():
#     return [
#         {
#             "clue": "Programa que protege contra virus",
#             "answer": "ANTIVIRUS"
#         },
#         {
#             "clue": "Red privada usada para navegar seguro",
#             "answer": "VPN"
#         },
#         {
#             "clue": "Clave secreta para acceder a una cuenta",
#             "answer": "PASSWORD"
#         },
#         {
#             "clue": "Ataque que bloquea un sistema",
#             "answer": "RANSOMWARE"
#         }

#     ]
QUIZ = {

    "phishing": [

        {
            "question": "¿Qué es phishing?",
            "options": [
                "Ataque",
                "Antivirus",
                "Firewall",
                "Pescar"
            ],
            "answer": 0
        },

        {
            "question": "¿Qué debes hacer con un correo sospechoso?",
            "options": [
                "Reportarlo",
                "Abrir enlaces",
                "Responder",
                "Descargar archivos"
            ],
            "answer": 0
        }

    ],

    "passwords": [

        {
            "question": "¿Cómo debe ser una contraseña segura?",
            "options": [
                "123456",
                "Larga y compleja",
                "Nombre",
                "Fecha"
            ],
            "answer": 1
        },

        {
            "question": "¿Qué significa MFA?",
            "options": [
                "Autenticación multifactor",
                "Más fácil acceso",
                "Firewall",
                "Modo administrador"
            ],
            "answer": 0
        }

    ],

    "malware": [

        {
            "question": "¿Qué es ransomware?",
            "options": [
                "Malware que secuestra archivos",
                "Firewall",
                "Correo",
                "VPN"
            ],
            "answer": 0
        },

        {
            "question": "¿Qué hacer con un USB desconocido?",
            "options": [
                "Conectarlo",
                "Ignorarlo",
                "Escanearlo",
                "Abrirlo"
            ],
            "answer": 2
        }

    ]

}
def get_quiz(topic):

    return QUIZ.get(
        topic,
        QUIZ["phishing"]
    )
# def get_quiz():
#     return [
#         {
#             "question": "¿Qué es phishing?",
#             "options": ["Ataque", "Antivirus", "Firewall", "Pescar"],
#             "answer": 0
#         },
#         {
#             "question": "¿Qué hace un firewall?",
#             "options": ["Protege red", "Borra archivos", "Hackea", "Hecha fuego"],
#             "answer": 0
#         },
#         {
#             "question": "¿Qué es malware?",
#             "options": ["Software malicioso", "Juego", "Correo", "Nada"],
#             "answer": 0
#         },
#         {
#             "question": "¿Cómo es una contraseña segura?",
#             "options": ["123456", "abc", "Larga y compleja", "Cumpleaños"],
#             "answer": 2
#         }
#     ]
WORDSEARCH = {

    "phishing": [

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
        },
        {
            "clue": "Servicio que protege la conexión en redes públicas",
            "answer": "VPN"
        }

    ],

    "passwords": [

        {
            "clue": "Clave para acceder a una cuenta",
            "answer": "PASSWORD"
        },
        {
            "clue": "Autenticación en dos pasos",
            "answer": "MFA"
        },
        {
            "clue": "Programa para almacenar contraseñas",
            "answer": "GESTOR"
        },
        {
            "clue": "Característica de una contraseña segura",
            "answer": "COMPLEJA"
        }

    ],

    "malware": [

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
        },
        {
            "clue": "Dispositivo que puede propagar malware",
            "answer": "USB"
        }

    ]

}
def get_wordsearch(topic):

    return WORDSEARCH.get(
        topic,
        WORDSEARCH["phishing"]
    )
# def get_wordsearch():    
#     return [
#         {
#             "clue": "Ataque para robar datos",
#             "answer": "PHISHING"
#         },
#         {
#             "clue": "Software malicioso",
#             "answer": "MALWARE"
#         },
#         {
#             "clue": "Acción ante correo sospechoso",
#             "answer": "REPORTAR"
#         },
#         {
#             "clue": "Evitar contacto sospechoso",
#             "answer": "IGNORAR"
#         }
#     ]