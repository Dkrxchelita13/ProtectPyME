def get_words():
    return [
        {"word": "PHISHING"},
        {"word": "MALWARE"}
    ]

def get_quiz():
    return [
        {
            "question": "¿Qué es phishing?",
            "options": ["Ataque", "Antivirus", "Firewall", "Pescar"],
            "answer": 0
        },
        {
            "question": "¿Qué hace un firewall?",
            "options": ["Protege red", "Borra archivos", "Hackea", "Hecha fuego"],
            "answer": 0
        },
        {
            "question": "¿Qué es malware?",
            "options": ["Software malicioso", "Juego", "Correo", "Nada"],
            "answer": 0
        },
        {
            "question": "Cómo es una contraseña segura?",
            "options": ["123456", "abc", "Larga y compleja", "Cumpleaños"],
            "answer": 2
        }
    ]

def get_crossword():
    return [
        {
            "clue": "Ataque para robar datos",
            "answer": "PHISHING"
        },
        {
            "clue": "Software malicioso",
            "answer": "MALWARE"
        },
        {
            "clue": "Acción ante correo sospechoso",
            "answer": "REPORTAR"
        },
        {
            "clue": "Evitar contacto sospechoso",
            "answer": "IGNORAR"
        }
    ]