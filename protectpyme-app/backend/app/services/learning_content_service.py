import copy
import logging


logger = logging.getLogger("protectpyme")


LEARNING_CONTENT = {
    "phishing": {
        "alto": {
            "title": "Detección de phishing",
            "vulnerability": "Phishing e ingeniería social",
            "learning_objective": "Reconocer señales básicas de correos y mensajes fraudulentos.",
            "explanation": (
                "El phishing busca engañar a una persona para que revele datos, abra enlaces "
                "o descargue archivos peligrosos. Antes de comenzar, observa quién envía el "
                "mensaje, si pide actuar con urgencia y si incluye enlaces inesperados. Una "
                "revisión simple ayuda a tomar mejores decisiones, aunque siempre conviene "
                "confirmar por canales oficiales cuando algo parezca dudoso."
            ),
            "tips": [
                "Revisa el remitente antes de abrir enlaces.",
                "Desconfía de mensajes que exigen urgencia.",
                "Reporta correos sospechosos al área responsable.",
            ],
            "recommended_action": (
                "Revisa estas recomendaciones y practica primero las decisiones esenciales "
                "para detectar phishing."
            ),
        },
        "medio": {
            "title": "Detección de phishing",
            "vulnerability": "Phishing e ingeniería social",
            "learning_objective": (
                "Analizar señales que permitan distinguir mensajes legítimos de intentos de phishing."
            ),
            "explanation": (
                "Un mensaje de phishing puede parecer legítimo, pero suele incluir dominios "
                "alterados, solicitudes inesperadas, enlaces engañosos o presión para actuar "
                "rápidamente. Antes de responder, compara el remitente con comunicaciones "
                "anteriores, revisa la dirección real del enlace y confirma por otro medio "
                "cualquier solicitud relacionada con accesos, pagos o información confidencial."
            ),
            "tips": [
                "Comprueba cuidadosamente el dominio del remitente.",
                "Evita abrir enlaces recibidos de forma inesperada.",
                "Confirma solicitudes sensibles mediante otro canal.",
            ],
            "recommended_action": (
                "Analiza todas las señales antes de seleccionar cada respuesta del minijuego."
            ),
        },
        "bajo": {
            "title": "Detección de phishing",
            "vulnerability": "Phishing e ingeniería social",
            "learning_objective": (
                "Verificar de forma crítica mensajes sospechosos y fortalecer hábitos de reporte."
            ),
            "explanation": (
                "Aunque ya reconozcas señales comunes de phishing, algunos ataques imitan "
                "procesos reales de la empresa o usan información contextual para parecer "
                "confiables. Observa inconsistencias pequeñas, valida solicitudes fuera del "
                "canal original y piensa en el impacto de compartir credenciales o documentos. "
                "La prevención mejora cuando combinas criterio personal con procedimientos de "
                "verificación."
            ),
            "tips": [
                "Valida solicitudes críticas fuera del correo recibido.",
                "Revisa enlaces completos antes de hacer clic.",
                "Documenta patrones sospechosos para mejorar reportes.",
            ],
            "recommended_action": (
                "Aplica una revisión crítica y justifica mentalmente cada decisión sobre phishing."
            ),
        },
    },
    "passwords": {
        "alto": {
            "title": "Contraseñas seguras y autenticación",
            "vulnerability": "Contraseñas y protección de cuentas",
            "learning_objective": (
                "Reconocer prácticas básicas para crear y proteger contraseñas."
            ),
            "explanation": (
                "Las contraseñas cortas, predecibles o reutilizadas facilitan el acceso no "
                "autorizado a varias cuentas. Antes de comenzar, recuerda que una contraseña "
                "debe ser extensa, diferente para cada servicio y difícil de relacionar "
                "contigo. También es recomendable utilizar un gestor de contraseñas y activar "
                "un segundo factor de autenticación cuando esté disponible."
            ),
            "tips": [
                "Utiliza una contraseña distinta para cada cuenta.",
                "Prefiere frases largas y difíciles de predecir.",
                "Activa un segundo factor de autenticación.",
            ],
            "recommended_action": (
                "Revisa estas recomendaciones y practica primero las decisiones esenciales "
                "para proteger tus cuentas."
            ),
        },
        "medio": {
            "title": "Contraseñas seguras y autenticación",
            "vulnerability": "Contraseñas y protección de cuentas",
            "learning_objective": (
                "Evaluar hábitos de autenticación para decidir cómo proteger mejor cada cuenta."
            ),
            "explanation": (
                "Una cuenta puede estar expuesta aunque la contraseña parezca fuerte si se "
                "reutiliza, se comparte o no cuenta con una verificación adicional. Antes del "
                "minijuego, analiza qué servicios contienen información sensible, qué accesos "
                "requieren mayor protección y cuándo conviene cambiar credenciales. La seguridad "
                "mejora al combinar contraseñas únicas, gestión ordenada y autenticación "
                "multifactor."
            ),
            "tips": [
                "Prioriza MFA en cuentas con información sensible.",
                "Evita compartir credenciales por chats o correos.",
                "Cambia contraseñas expuestas o reutilizadas.",
            ],
            "recommended_action": (
                "Analiza el contexto de cada cuenta antes de elegir la respuesta del minijuego."
            ),
        },
        "bajo": {
            "title": "Contraseñas seguras y autenticación",
            "vulnerability": "Contraseñas y protección de cuentas",
            "learning_objective": (
                "Fortalecer criterios para validar credenciales, accesos y métodos de autenticación."
            ),
            "explanation": (
                "Cuando los hábitos básicos ya están presentes, el reto es mantener controles "
                "consistentes y revisar excepciones. Evalúa si cada cuenta usa una contraseña "
                "única, si el segundo factor es resistente a engaños y si existen accesos antiguos "
                "que deban cerrarse. Ningún mecanismo aislado resuelve todo, pero una revisión "
                "periódica reduce oportunidades de abuso."
            ),
            "tips": [
                "Audita cuentas antiguas y elimina accesos innecesarios.",
                "Prefiere factores de autenticación resistentes al phishing.",
                "Revisa alertas de inicio de sesión sospechosas.",
            ],
            "recommended_action": (
                "Aplica una revisión crítica de autenticación antes de responder cada desafío."
            ),
        },
    },
    "malware": {
        "alto": {
            "title": "Prevención de malware y dispositivos externos",
            "vulnerability": "Malware y dispositivos USB",
            "learning_objective": (
                "Identificar acciones básicas para evitar infecciones por archivos o USB desconocidas."
            ),
            "explanation": (
                "El malware es software diseñado para dañar, espiar o controlar un equipo sin "
                "autorización. Puede llegar mediante descargas, archivos adjuntos o dispositivos "
                "USB desconocidos. Antes de jugar, observa el origen de cada archivo, evita abrir "
                "elementos inesperados y solicita apoyo si un dispositivo no pertenece a la "
                "empresa. Estas acciones reducen riesgos sin reemplazar otros controles."
            ),
            "tips": [
                "No conectes USB de origen desconocido.",
                "Evita abrir archivos adjuntos inesperados.",
                "Consulta a soporte ante alertas de seguridad.",
            ],
            "recommended_action": (
                "Revisa estas recomendaciones y practica decisiones esenciales contra malware."
            ),
        },
        "medio": {
            "title": "Prevención de malware y dispositivos externos",
            "vulnerability": "Malware y dispositivos USB",
            "learning_objective": (
                "Distinguir señales de riesgo antes de abrir archivos o conectar dispositivos."
            ),
            "explanation": (
                "Los ataques con malware suelen aprovechar descuidos: extensiones engañosas, "
                "archivos comprimidos, instaladores no autorizados o USB encontrados. Antes de "
                "aceptar una acción, analiza si el origen es confiable, si el archivo era esperado "
                "y si el equipo muestra una advertencia. La mejor decisión suele combinar prudencia, "
                "verificación y reporte oportuno."
            ),
            "tips": [
                "Verifica el origen antes de ejecutar archivos.",
                "Observa extensiones dobles o nombres extraños.",
                "Reporta dispositivos encontrados sin conectarlos.",
            ],
            "recommended_action": (
                "Analiza cada señal de riesgo antes de seleccionar una acción del minijuego."
            ),
        },
        "bajo": {
            "title": "Prevención de malware y dispositivos externos",
            "vulnerability": "Malware y dispositivos USB",
            "learning_objective": (
                "Evaluar de forma crítica controles preventivos frente a malware y medios externos."
            ),
            "explanation": (
                "La prevención avanzada de malware exige revisar contexto, permisos y señales "
                "del sistema. Un archivo puede parecer normal pero solicitar privilegios, provenir "
                "de una fuente dudosa o coincidir con una campaña reciente. Antes del minijuego, "
                "piensa cómo validarías el origen, qué evidencia observarías y cuándo aislarías "
                "el equipo o escalarías el incidente."
            ),
            "tips": [
                "Evalúa permisos solicitados por archivos o instaladores.",
                "Aísla equipos con comportamiento claramente anómalo.",
                "Contrasta alertas con fuentes internas confiables.",
            ],
            "recommended_action": (
                "Aplica una revisión crítica y justifica mentalmente cada decisión sobre malware."
            ),
        },
    },
    "wifi": {
        "alto": {
            "title": "Uso seguro de redes y conexiones",
            "vulnerability": "Redes WiFi y conexiones inseguras",
            "learning_objective": (
                "Identificar prácticas básicas para conectarse a redes de forma segura."
            ),
            "explanation": (
                "Las redes WiFi públicas o desconocidas pueden exponer información si no se usan "
                "con cuidado. Antes de conectarte, verifica el nombre de la red, evita compartir "
                "datos sensibles y prefiere conexiones protegidas por la empresa. Una red conocida "
                "reduce riesgos, pero también debes observar advertencias del navegador y no aceptar "
                "configuraciones que no entiendas."
            ),
            "tips": [
                "Verifica el nombre exacto de la red.",
                "Evita operaciones sensibles en redes públicas.",
                "Usa VPN corporativa cuando esté disponible.",
            ],
            "recommended_action": (
                "Revisa estas recomendaciones y practica decisiones esenciales sobre conexiones seguras."
            ),
        },
        "medio": {
            "title": "Uso seguro de redes y conexiones",
            "vulnerability": "Redes WiFi y conexiones inseguras",
            "learning_objective": (
                "Analizar señales de confianza antes de usar una red o servicio en línea."
            ),
            "explanation": (
                "Una conexión puede parecer normal aunque exista una red falsa, un portal cautivo "
                "engañoso o sitios sin protección adecuada. Antes de continuar, revisa si la red "
                "corresponde al lugar, si la navegación usa HTTPS y si el servicio solicita datos "
                "innecesarios. Tomar unos segundos para evaluar señales ayuda a evitar exposiciones "
                "sin bloquear el trabajo diario."
            ),
            "tips": [
                "Comprueba HTTPS antes de enviar información.",
                "Evita redes con nombres imitados o confusos.",
                "Desactiva conexión automática a redes abiertas.",
            ],
            "recommended_action": (
                "Analiza las señales de conexión antes de elegir cada respuesta del minijuego."
            ),
        },
        "bajo": {
            "title": "Uso seguro de redes y conexiones",
            "vulnerability": "Redes WiFi y conexiones inseguras",
            "learning_objective": (
                "Fortalecer la verificación de redes, cifrado y canales de conexión."
            ),
            "explanation": (
                "Con buenos hábitos de conexión, el siguiente paso es evaluar configuraciones y "
                "contextos menos evidentes. Revisa si la red usa cifrado adecuado, si el dispositivo "
                "recuerda redes abiertas y si una VPN es necesaria para recursos internos. También "
                "conviene detectar cambios inesperados en certificados o portales de acceso antes "
                "de continuar."
            ),
            "tips": [
                "Revisa redes guardadas y elimina las innecesarias.",
                "Valida certificados cuando aparezcan advertencias inesperadas.",
                "Usa conexiones corporativas para recursos internos.",
            ],
            "recommended_action": (
                "Aplica una revisión crítica de red y justifica cada decisión del minijuego."
            ),
        },
    },
}


def get_learning_content(topic: str, risk: str) -> dict:
    topic_content = LEARNING_CONTENT.get(topic)

    if topic_content is None or risk not in topic_content:
        logger.warning("Learning content fallback applied for unsupported topic/risk")
        topic = "phishing"
        risk = "alto"
        topic_content = LEARNING_CONTENT[topic]

    lesson = copy.deepcopy(topic_content[risk])
    lesson["topic"] = topic
    lesson["risk"] = risk

    return lesson
