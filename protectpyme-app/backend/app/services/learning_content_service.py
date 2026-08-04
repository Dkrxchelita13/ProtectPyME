import copy
import logging

from app.services.concept_catalog import get_concepts


logger = logging.getLogger("protectpyme")


BASE_CONTENT = {
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


MINIGAME_ALIASES = {
    "quiz": "quiz",
    "kahoot": "quiz",
    "wordsearch": "wordsearch",
    "sopa": "wordsearch",
    "sopa_letras": "wordsearch",
    "sopaletras": "wordsearch",
    "crossword": "crossword",
    "crucigrama": "crossword",
}


CONCEPT_SETS = {
    "phishing": {
        "alto": [
            {
                "term": "Engaño",
                "definition": "Técnica para hacer que una persona confíe en un mensaje falso.",
                "why_it_matters": "El ataque funciona cuando alguien actúa sin verificar.",
                "example": "Un correo simula ser del banco y pide actualizar datos de acceso.",
            },
            {
                "term": "Enlace",
                "definition": "Dirección dentro del mensaje que puede llevar a un sitio falso.",
                "why_it_matters": "Un enlace puede capturar credenciales aunque el mensaje parezca real.",
                "example": "El botón dice 'portal de nómina', pero abre un dominio desconocido.",
            },
            {
                "term": "Reporte",
                "definition": "Aviso al área responsable para revisar un correo sospechoso.",
                "why_it_matters": "Reportar ayuda a proteger a otros empleados de la misma campaña.",
                "example": "Reenvías el mensaje sospechoso al canal interno de soporte.",
            },
        ],
        "medio": [
            {
                "term": "Dominio",
                "definition": "Parte de la dirección que identifica a la organización real.",
                "why_it_matters": "Dominios alterados son una señal frecuente de fraude.",
                "example": "empresa.com es distinto de empresa-soporte.com.",
            },
            {
                "term": "URL",
                "definition": "Dirección completa de una página o recurso en internet.",
                "why_it_matters": "Revisarla antes de hacer clic ayuda a detectar sitios falsos.",
                "example": "Un enlace de pago apunta a un dominio que no pertenece al proveedor.",
            },
            {
                "term": "Spam",
                "definition": "Correo no deseado que puede ser molesto o riesgoso.",
                "why_it_matters": "No todo spam es phishing, pero puede ocultar engaños.",
                "example": "Un correo masivo ofrece descuentos y adjunta un archivo inesperado.",
            },
        ],
        "bajo": [
            {
                "term": "Spear phishing",
                "definition": "Phishing dirigido a una persona, área o empresa específica.",
                "why_it_matters": "Usa contexto real para parecer más confiable.",
                "example": "Un mensaje menciona a tu jefe y solicita revisar una factura urgente.",
            },
            {
                "term": "SPF",
                "definition": "Registro que indica qué servidores pueden enviar correo por un dominio.",
                "why_it_matters": "Ayuda a detectar mensajes enviados desde servidores no autorizados.",
                "example": "Un dominio corporativo define qué proveedor puede mandar sus correos.",
            },
            {
                "term": "DKIM",
                "definition": "Firma digital que ayuda a verificar que el correo no fue alterado.",
                "why_it_matters": "Aporta evidencia técnica sobre la autenticidad del mensaje.",
                "example": "El servidor receptor revisa la firma antes de confiar en el correo.",
            },
            {
                "term": "DMARC",
                "definition": "Política que usa SPF y DKIM para decidir qué hacer con correos sospechosos.",
                "why_it_matters": "Reduce la suplantación de dominios de la empresa.",
                "example": "La organización pide rechazar mensajes que no pasen validaciones.",
            },
        ],
    },
    "passwords": {
        "alto": [
            {
                "term": "Contraseña larga",
                "definition": "Clave con suficientes caracteres para ser difícil de adivinar.",
                "why_it_matters": "La longitud aumenta el esfuerzo necesario para probar combinaciones.",
                "example": "Una frase de varias palabras es mejor que '123456'.",
            },
            {
                "term": "Contraseña única",
                "definition": "Clave usada solo en una cuenta o servicio.",
                "why_it_matters": "Si una cuenta se filtra, las demás no quedan expuestas automáticamente.",
                "example": "El correo, la banca y el sistema de ventas usan claves distintas.",
            },
            {
                "term": "Reutilización",
                "definition": "Usar la misma contraseña en varios servicios.",
                "why_it_matters": "Una filtración externa puede abrir accesos internos.",
                "example": "La misma clave de una tienda se usa también en el correo laboral.",
            },
            {
                "term": "Secreto",
                "definition": "Dato privado que no debe compartirse por chat, correo o teléfono.",
                "why_it_matters": "Compartirlo rompe el control sobre quién accede a una cuenta.",
                "example": "Un código temporal de acceso no se envía a otra persona.",
            },
        ],
        "medio": [
            {
                "term": "MFA",
                "definition": "Autenticación que pide más de un factor para entrar.",
                "why_it_matters": "Reduce el riesgo cuando una contraseña queda expuesta.",
                "example": "Además de la clave, se aprueba el inicio de sesión en una app.",
            },
            {
                "term": "Gestor de contraseñas",
                "definition": "Herramienta para guardar y generar contraseñas únicas de forma segura.",
                "why_it_matters": "Evita anotar claves o repetirlas por memoria.",
                "example": "El gestor crea una clave distinta para cada proveedor.",
            },
            {
                "term": "Passphrase",
                "definition": "Frase larga usada como contraseña fácil de recordar y difícil de adivinar.",
                "why_it_matters": "Combina longitud con menor carga para el usuario.",
                "example": "Una frase interna sin datos personales puede proteger una cuenta.",
            },
        ],
        "bajo": [
            {
                "term": "Hash",
                "definition": "Resultado de aplicar una función a una contraseña para no guardar el texto original.",
                "why_it_matters": "Permite verificar la contraseña sin almacenarla en claro; no es cifrado reversible.",
                "example": "El sistema compara hashes en lugar de guardar 'MiClave2026'.",
            },
            {
                "term": "Salt",
                "definition": "Valor aleatorio único agregado antes de generar el hash.",
                "why_it_matters": "Hace que contraseñas iguales produzcan hashes distintos.",
                "example": "Dos empleados con la misma clave no tendrían el mismo hash guardado.",
            },
            {
                "term": "Argon2id",
                "definition": "Función moderna para almacenar contraseñas usando memoria y tiempo.",
                "why_it_matters": "Dificulta intentos masivos contra hashes robados.",
                "example": "Un sistema nuevo puede usar Argon2id para proteger credenciales.",
            },
            {
                "term": "Password spraying",
                "definition": "Ataque que prueba una contraseña común en muchas cuentas.",
                "why_it_matters": "Evita bloqueos rápidos y explota claves débiles reutilizadas.",
                "example": "Un atacante prueba 'Empresa2026' contra todos los correos.",
            },
        ],
    },
    "malware": {
        "alto": [
            {
                "term": "Malware",
                "definition": "Software diseñado para dañar, espiar o controlar un equipo sin permiso.",
                "why_it_matters": "Puede afectar archivos, credenciales y continuidad del negocio.",
                "example": "Un adjunto falso instala un programa no autorizado.",
            },
            {
                "term": "Virus",
                "definition": "Tipo de malware que puede propagarse al ejecutar archivos infectados.",
                "why_it_matters": "Ayuda a distinguir un tipo específico dentro del malware.",
                "example": "Un archivo compartido infecta otros documentos al abrirse.",
            },
            {
                "term": "Antivirus",
                "definition": "Herramienta que ayuda a detectar y bloquear software malicioso.",
                "why_it_matters": "Es un apoyo, pero no reemplaza la verificación del usuario.",
                "example": "Una alerta del antivirus indica que no abras el archivo.",
            },
            {
                "term": "USB desconocido",
                "definition": "Dispositivo externo cuyo origen no está autorizado o confirmado.",
                "why_it_matters": "Puede contener archivos maliciosos o activar riesgos al conectarse.",
                "example": "Una memoria encontrada en recepción se entrega a soporte.",
            },
        ],
        "medio": [
            {
                "term": "Ransomware",
                "definition": "Malware que cifra o bloquea archivos para exigir un pago.",
                "why_it_matters": "Puede detener operaciones y afectar información crítica.",
                "example": "Las carpetas compartidas quedan bloqueadas y aparece una nota de rescate.",
            },
            {
                "term": "Spyware",
                "definition": "Software que recopila información del usuario sin autorización.",
                "why_it_matters": "Puede robar credenciales, hábitos de navegación o datos de clientes.",
                "example": "Un programa falso registra lo que se escribe en el equipo.",
            },
            {
                "term": "Botnet",
                "definition": "Red de equipos infectados controlados por un atacante.",
                "why_it_matters": "Un equipo de la empresa puede usarse para ataques sin que se note.",
                "example": "Una computadora infectada envía tráfico extraño en segundo plano.",
            },
        ],
        "bajo": [
            {
                "term": "Rootkit",
                "definition": "Malware que intenta ocultar su presencia en el sistema.",
                "why_it_matters": "Puede dificultar la detección y la limpieza del equipo.",
                "example": "El equipo parece normal aunque mantiene procesos ocultos.",
            },
            {
                "term": "Sandbox",
                "definition": "Entorno aislado y autorizado para analizar archivos sospechosos.",
                "why_it_matters": "Permite observar riesgos sin exponer sistemas reales.",
                "example": "Soporte analiza un adjunto en un laboratorio controlado.",
            },
            {
                "term": "Persistencia",
                "definition": "Capacidad de mantenerse activo después de reiniciar el equipo.",
                "why_it_matters": "Indica que la amenaza puede volver si no se elimina correctamente.",
                "example": "Un programa reaparece cada vez que se prende la computadora.",
            },
        ],
    },
    "wifi": {
        "alto": [
            {
                "term": "Red pública",
                "definition": "WiFi disponible para muchas personas y con control limitado.",
                "why_it_matters": "Puede exponer datos si se usa sin protección.",
                "example": "La red abierta de una cafetería no es equivalente a la red corporativa.",
            },
            {
                "term": "HTTPS",
                "definition": "Protocolo que protege la comunicación con un sitio web.",
                "why_it_matters": "Ayuda a evitar que otros vean o alteren información enviada.",
                "example": "El navegador muestra candado al entrar al portal del proveedor.",
            },
            {
                "term": "VPN",
                "definition": "Servicio que protege la conexión hacia recursos autorizados.",
                "why_it_matters": "Agrega una capa de protección al trabajar fuera de la oficina.",
                "example": "Se activa la VPN antes de consultar un sistema interno.",
            },
            {
                "term": "Datos sensibles",
                "definition": "Información que puede causar daño si se expone.",
                "why_it_matters": "Incluye credenciales, datos de clientes, pagos o documentos internos.",
                "example": "No se envía una lista de clientes desde una red abierta.",
            },
        ],
        "medio": [
            {
                "term": "SSID",
                "definition": "Nombre visible de una red inalámbrica.",
                "why_it_matters": "Los atacantes pueden imitar nombres conocidos para confundir.",
                "example": "Oficina_Invitados no es igual a Oficina-Invitados.",
            },
            {
                "term": "Hotspot",
                "definition": "Punto de acceso que ofrece conexión WiFi.",
                "why_it_matters": "Puede ser legítimo o falso según quién lo controle.",
                "example": "Un celular comparte internet como hotspot temporal autorizado.",
            },
            {
                "term": "WPA2",
                "definition": "Estándar de seguridad usado para proteger redes inalámbricas.",
                "why_it_matters": "Indica mejor protección que una red abierta sin clave.",
                "example": "La red de oficina usa WPA2 con una clave administrada.",
            },
            {
                "term": "Red falsa",
                "definition": "Red creada para parecer confiable y atraer usuarios.",
                "why_it_matters": "Puede capturar tráfico o credenciales si se usa sin verificar.",
                "example": "Un atacante crea 'Empresa Gratis' cerca de la oficina.",
            },
        ],
        "bajo": [
            {
                "term": "Evil Twin",
                "definition": "Red falsa que imita una red legítima.",
                "why_it_matters": "Engaña al usuario para conectarse a un punto controlado por otro.",
                "example": "Una red copia el nombre del WiFi del hotel para robar accesos.",
            },
            {
                "term": "Rogue AP",
                "definition": "Punto de acceso no autorizado dentro o cerca de la organización.",
                "why_it_matters": "Puede abrir una entrada insegura a la red o confundir empleados.",
                "example": "Alguien conecta un router personal sin permiso en la oficina.",
            },
            {
                "term": "WPA3",
                "definition": "Estándar moderno que mejora la seguridad de redes WiFi.",
                "why_it_matters": "Reduce riesgos frente a ataques comunes contra redes inalámbricas.",
                "example": "Un router nuevo de la empresa se configura con WPA3 cuando es posible.",
            },
            {
                "term": "Red oficial",
                "definition": "Red confirmada por la organización como segura para trabajar.",
                "why_it_matters": "Validarla evita conectarse a imitaciones o puntos no autorizados.",
                "example": "El nombre de la red se confirma con TI antes de ingresar credenciales.",
            },
        ],
    },
}


VISUAL_KEYS = {
    "phishing": {
        "alto": "phishing_email_signals",
        "medio": "phishing_email_signals",
        "bajo": "phishing_email_authentication",
    },
    "passwords": {
        "alto": "password_unique_accounts",
        "medio": "password_unique_accounts",
        "bajo": "password_hash_flow",
    },
    "malware": {
        "alto": "malware_usb_decision",
        "medio": "malware_analysis_flow",
        "bajo": "malware_analysis_flow",
    },
    "wifi": {
        "alto": "wifi_public_check",
        "medio": "wifi_fake_network",
        "bajo": "wifi_fake_network",
    },
}


QUICK_CHECKS = {
    "phishing": {
        "alto": ("Un proveedor pide abrir un enlace inesperado. ¿Qué haces primero?", ["Abrirlo para comprobar", "Revisar remitente y reportar si parece sospechoso", "Responder con tus datos"], 1, "Primero se revisan señales básicas y se reporta si hay duda."),
        "medio": ("Un enlace dice una cosa pero apunta a otro dominio. ¿Qué señal es?", ["Posible sitio falso", "Confirmación automática", "Archivo local"], 0, "La URL real debe coincidir con el sitio esperado."),
        "bajo": ("Un correo falla SPF y DKIM. ¿Qué ayuda a decidir la política?", ["DMARC", "USB", "WPA3"], 0, "DMARC combina esas validaciones para reducir suplantación."),
    },
    "passwords": {
        "alto": ("Una clave se usa en correo y ventas. ¿Cuál es el riesgo?", ["Solo ocupa memoria", "Una filtración puede afectar ambas cuentas", "Hace más rápida la sesión"], 1, "Cada cuenta debe tener una contraseña única."),
        "medio": ("Una cuenta crítica ya tiene clave fuerte. ¿Qué control agrega protección?", ["MFA", "Compartir la clave", "Usar una red abierta"], 0, "MFA reduce el riesgo si la clave se expone."),
        "bajo": ("Dos usuarios tienen la misma contraseña. ¿Qué evita que el hash guardado sea igual?", ["Salt único", "SSID", "Spam"], 0, "El salt único cambia el resultado del hash."),
    },
    "malware": {
        "alto": ("Encuentras un USB en recepción. ¿Qué decisión reduce el riesgo?", ["Conectarlo para ver archivos", "Entregarlo a soporte sin conectarlo", "Copiar su contenido"], 1, "Los dispositivos desconocidos deben verificarse por canales autorizados."),
        "medio": ("Un archivo cifra carpetas compartidas y pide pago. ¿Qué concepto describe el caso?", ["Ransomware", "SSID", "MFA"], 0, "Ransomware bloquea o cifra archivos para presionar a la víctima."),
        "bajo": ("Soporte quiere revisar un adjunto sin exponer equipos reales. ¿Qué usa?", ["Sandbox autorizada", "Red pública", "Password spraying"], 0, "La sandbox aísla el análisis del entorno productivo."),
    },
    "wifi": {
        "alto": ("Necesitas enviar datos de clientes fuera de la oficina. ¿Qué revisas?", ["HTTPS/VPN y red confiable", "Solo el color del portal", "Que la red no pida clave"], 0, "Datos sensibles requieren conexión protegida."),
        "medio": ("Ves dos redes con nombres casi iguales. ¿Qué debes validar?", ["SSID oficial", "El tamaño del texto", "La marca del celular"], 0, "El SSID ayuda a identificar la red correcta."),
        "bajo": ("Una red copia el nombre exacto de la red oficial. ¿Qué ataque puede ser?", ["Evil Twin", "Hash", "Botnet"], 0, "Evil Twin imita una red legítima para engañar usuarios."),
    },
}


def normalize_minigame(value: str) -> str:
    normalized = (value or "").lower().strip()

    if normalized in MINIGAME_ALIASES:
        return MINIGAME_ALIASES[normalized]

    raise ValueError("Minigame must be quiz, wordsearch or crossword.")


def _legacy_build_practical_example(topic: str, risk: str, minigame: str) -> dict:
    labels = {
        "quiz": "Decidir antes de responder",
        "wordsearch": "Reconocer términos clave",
        "crossword": "Relacionar pistas con conceptos",
    }

    return {
        "title": f"{labels[minigame]} en una PYME",
        "steps": [
            "Observa el caso como si ocurriera durante una jornada normal.",
            "Identifica los términos o señales que aparecen en la situación.",
            "Piensa por qué importan para proteger cuentas, equipos o datos.",
            "Elige la respuesta o palabra solo después de verificar el concepto.",
        ],
    }


def _legacy_build_common_mistake(minigame: str) -> dict:
    mistakes = {
        "quiz": (
            "Responder por intuición",
            "Elegir la opción que suena familiar sin revisar las señales puede reforzar hábitos inseguros.",
        ),
        "wordsearch": (
            "Buscar palabras sin entenderlas",
            "Reconocer un término ayuda poco si no se entiende cuándo aplicarlo en el trabajo.",
        ),
        "crossword": (
            "Memorizar la pista literalmente",
            "El objetivo es conectar definición y concepto, no repetir una frase exacta.",
        ),
    }
    title, explanation = mistakes[minigame]
    return {"title": title, "explanation": explanation}


def _legacy_build_supplement(topic: str, risk: str, minigame: str) -> dict:
    question, options, correct_option, explanation = QUICK_CHECKS[topic][risk]

    return {
        "key_concepts": copy.deepcopy(CONCEPT_SETS[topic][risk]),
        "practical_example": _legacy_build_practical_example(topic, risk, minigame),
        "common_mistake": _legacy_build_common_mistake(minigame),
        "quick_check": {
            "question": question,
            "options": options,
            "correct_option": correct_option,
            "explanation": explanation,
        },
        "visual_key": VISUAL_KEYS[topic][risk],
    }


WORD_PUZZLE_CONCEPT_SETS = {
    "phishing": {
        "alto": [
            {"term": "Phishing", "definition": "Ataque que usa engaños para robar credenciales o información.", "why_it_matters": "Reconocerlo ubica el tipo de riesgo del mensaje.", "example": "Un correo falso pide entrar a un portal para confirmar la cuenta."},
            {"term": "Reportar", "definition": "Avisar al área responsable cuando un mensaje parece sospechoso.", "why_it_matters": "Permite revisar el caso antes de que otros empleados caigan.", "example": "Se envía el correo dudoso al canal interno de soporte."},
            {"term": "Enlace", "definition": "Dirección dentro del correo que puede llevar a un sitio falso.", "why_it_matters": "Muchos fraudes empiezan cuando alguien abre un enlace sin validar.", "example": "El botón dice nómina, pero apunta a un dominio extraño."},
        ],
        "medio": [
            {"term": "Dominio", "definition": "Parte de una dirección que identifica al sitio o remitente.", "why_it_matters": "Un dominio imitado puede revelar un intento de suplantación.", "example": "empresa.com no es igual a empresa-soporte.com."},
            {"term": "URL", "definition": "Dirección completa que debe revisarse antes de abrir un enlace.", "why_it_matters": "La URL real puede ser distinta al texto mostrado.", "example": "Un enlace de pago apunta a un sitio que no pertenece al proveedor."},
            {"term": "Spam", "definition": "Correo no deseado que puede contener mensajes irrelevantes o riesgosos.", "why_it_matters": "No todo spam es phishing, pero puede ocultar fraudes.", "example": "Un correo masivo ofrece premios y adjunta un archivo inesperado."},
        ],
        "bajo": [
            {"term": "DMARC", "definition": "Política que usa SPF y DKIM para tratar correos sospechosos.", "why_it_matters": "Ayuda a reducir la suplantación del dominio de la empresa.", "example": "La organización pide rechazar correos que no pasen validaciones."},
            {"term": "SPF", "definition": "Registro que indica qué servidores pueden enviar correo por un dominio.", "why_it_matters": "Permite detectar mensajes enviados desde servidores no autorizados.", "example": "El dominio define qué proveedor puede mandar facturas."},
            {"term": "DKIM", "definition": "Firma digital que ayuda a verificar que el correo no fue alterado.", "why_it_matters": "Aporta evidencia técnica sobre autenticidad e integridad.", "example": "El servidor receptor revisa la firma antes de confiar en el mensaje."},
        ],
    },
    "passwords": {
        "alto": [
            {"term": "Contraseña", "definition": "Clave usada para entrar a una cuenta o servicio.", "why_it_matters": "Es una barrera inicial contra accesos no autorizados.", "example": "La cuenta de ventas requiere una contraseña propia."},
            {"term": "Larga", "definition": "Característica de una contraseña con suficientes caracteres.", "why_it_matters": "La longitud dificulta ataques por adivinación.", "example": "Una frase extensa es mejor que una palabra corta."},
            {"term": "Secreto", "definition": "Dato privado que no debe compartirse por chat, correo o teléfono.", "why_it_matters": "Compartir secretos rompe el control de acceso.", "example": "Un código temporal no se manda a otra persona."},
        ],
        "medio": [
            {"term": "MFA", "definition": "Autenticación que pide más de un factor para entrar.", "why_it_matters": "Reduce el riesgo si una contraseña queda expuesta.", "example": "Además de la clave se aprueba el acceso en una app."},
            {"term": "Gestor", "definition": "Programa para guardar y generar contraseñas seguras.", "why_it_matters": "Evita repetir claves o anotarlas en lugares inseguros.", "example": "El gestor crea una clave distinta para cada proveedor."},
            {"term": "Passphrase", "definition": "Frase larga usada como contraseña.", "why_it_matters": "Combina longitud con facilidad de recuerdo.", "example": "Una frase interna sin datos personales protege una cuenta."},
        ],
        "bajo": [
            {"term": "Salt", "definition": "Valor aleatorio único agregado antes de generar el hash.", "why_it_matters": "Hace que contraseñas iguales produzcan hashes distintos.", "example": "Dos empleados con la misma clave no tendrían el mismo hash guardado."},
            {"term": "Hash", "definition": "Resultado de aplicar una función a una contraseña para no guardar el texto original.", "why_it_matters": "Permite verificar la contraseña sin almacenarla en claro; no es cifrado reversible.", "example": "El sistema compara hashes en lugar de guardar la contraseña real."},
            {"term": "Argon2id", "definition": "Función moderna para almacenar contraseñas usando memoria y tiempo.", "why_it_matters": "Dificulta intentos masivos contra hashes robados.", "example": "Un sistema nuevo puede usar Argon2id para proteger credenciales."},
        ],
    },
    "malware": {
        "alto": [
            {"term": "Malware", "definition": "Software diseñado para dañar, espiar o controlar un equipo.", "why_it_matters": "Puede afectar archivos, credenciales y continuidad del negocio.", "example": "Un adjunto falso instala un programa no autorizado."},
            {"term": "Virus", "definition": "Tipo de malware que puede propagarse al ejecutar archivos infectados.", "why_it_matters": "Ayuda a reconocer una categoría frecuente dentro del software malicioso.", "example": "Un archivo compartido infecta otros documentos al abrirse."},
            {"term": "Antivirus", "definition": "Herramienta que ayuda a detectar y bloquear software malicioso.", "why_it_matters": "Es un apoyo importante, aunque no reemplaza la verificación del usuario.", "example": "Una alerta indica que no abras el archivo."},
            {"term": "USB", "definition": "Dispositivo externo que puede transportar archivos o amenazas.", "why_it_matters": "Una USB desconocida puede iniciar una infección.", "example": "Una memoria encontrada se entrega a soporte sin conectarla."},
        ],
        "medio": [
            {"term": "Ransomware", "definition": "Malware que cifra o bloquea archivos para exigir un pago.", "why_it_matters": "Puede detener operaciones y afectar información crítica.", "example": "Las carpetas compartidas quedan bloqueadas y aparece una nota de rescate."},
            {"term": "Spyware", "definition": "Software que recopila información del usuario sin autorización.", "why_it_matters": "Puede robar credenciales o datos de clientes.", "example": "Un programa falso registra lo que se escribe en el equipo."},
            {"term": "Botnet", "definition": "Red de equipos infectados controlados por un atacante.", "why_it_matters": "Un equipo de la empresa puede usarse para ataques sin que se note.", "example": "Una computadora envía tráfico extraño en segundo plano."},
        ],
        "bajo": [
            {"term": "Rootkit", "definition": "Malware que intenta ocultar su presencia en el sistema.", "why_it_matters": "Puede dificultar la detección y limpieza del equipo.", "example": "El equipo parece normal aunque mantiene procesos ocultos."},
            {"term": "Sandbox", "definition": "Entorno aislado y autorizado para analizar archivos sospechosos.", "why_it_matters": "Permite observar riesgos sin exponer sistemas reales.", "example": "Soporte analiza un adjunto en un laboratorio controlado."},
            {"term": "Persistencia", "definition": "Capacidad de mantenerse activo después de reiniciar el equipo.", "why_it_matters": "Indica que la amenaza puede volver si no se elimina correctamente.", "example": "Un programa reaparece cada vez que se prende la computadora."},
        ],
    },
    "wifi": {
        "alto": [
            {"term": "WiFi", "definition": "Red inalámbrica usada para conectar dispositivos.", "why_it_matters": "Su configuración influye en la exposición de datos.", "example": "La red de invitados no equivale a la red corporativa."},
            {"term": "VPN", "definition": "Servicio que protege la conexión hacia recursos autorizados.", "why_it_matters": "Agrega una capa de protección al trabajar fuera de la oficina.", "example": "Se activa la VPN antes de consultar un sistema interno."},
            {"term": "HTTPS", "definition": "Protocolo que protege la comunicación con un sitio web.", "why_it_matters": "Ayuda a evitar que otros vean o alteren información enviada.", "example": "El navegador muestra candado al entrar al portal del proveedor."},
        ],
        "medio": [
            {"term": "SSID", "definition": "Nombre visible de una red inalámbrica.", "why_it_matters": "Los atacantes pueden imitar nombres conocidos para confundir.", "example": "Oficina_Invitados no es igual a Oficina-Invitados."},
            {"term": "Hotspot", "definition": "Punto de acceso que ofrece conexión WiFi.", "why_it_matters": "Puede ser legítimo o falso según quién lo controle.", "example": "Un celular comparte internet como hotspot temporal autorizado."},
            {"term": "WPA2", "definition": "Estándar de seguridad usado para proteger redes inalámbricas.", "why_it_matters": "Indica mejor protección que una red abierta sin clave.", "example": "La red de oficina usa WPA2 con una clave administrada."},
        ],
        "bajo": [
            {"term": "Evil Twin", "definition": "Red falsa que imita una red legítima.", "why_it_matters": "Engaña al usuario para conectarse a un punto controlado por otro.", "example": "Una red copia el nombre del WiFi del hotel para robar accesos."},
            {"term": "Rogue AP", "definition": "Punto de acceso no autorizado dentro o cerca de la organización.", "why_it_matters": "Puede abrir una entrada insegura a la red o confundir empleados.", "example": "Alguien conecta un router personal sin permiso en la oficina."},
            {"term": "WPA3", "definition": "Estándar moderno que mejora la seguridad de redes WiFi.", "why_it_matters": "Reduce riesgos frente a ataques comunes contra redes inalámbricas.", "example": "Un router nuevo de la empresa se configura con WPA3 cuando es posible."},
        ],
    },
}


QUIZ_CONCEPT_OVERRIDES = {
    "wifi": {
        "medio": [
            {
                "term": "VPN",
                "definition": "Servicio que protege la conexión hacia recursos autorizados.",
                "why_it_matters": "Ayuda a trabajar con menor exposición cuando se usa una red fuera de la oficina.",
                "example": "Se activa la VPN antes de abrir un sistema interno desde una red de visitas.",
            },
            {
                "term": "Hotspot falso",
                "definition": "Punto de acceso creado para parecer confiable y engañar usuarios.",
                "why_it_matters": "Puede capturar tráfico o credenciales si se usa sin verificar.",
                "example": "Una red con nombre parecido al del negocio aparece cerca de la oficina.",
            },
            {
                "term": "SSID",
                "definition": "Nombre visible de una red inalámbrica.",
                "why_it_matters": "Compararlo con el nombre oficial ayuda a evitar redes imitadas.",
                "example": "Oficina_Invitados no es igual a Oficina-Invitados.",
            },
        ],
    },
}


MINIGAME_QUICK_CHECKS = {
    "quiz": QUICK_CHECKS,
    "wordsearch": {
        "phishing": {
            "alto": ("¿Qué término buscarías si la pista habla de avisar un correo sospechoso?", ["Reportar", "WPA3", "Hash"], 0, "En sopa de letras conviene asociar la acción correcta con el término visible."),
            "medio": ("Si la pista menciona la dirección completa de una página, ¿qué palabra debes reconocer?", ["URL", "MFA", "USB"], 0, "URL es la palabra breve que representa esa dirección."),
            "bajo": ("¿Qué término combina políticas con SPF y DKIM?", ["DMARC", "Botnet", "Salt"], 0, "DMARC es el término que debes reconocer junto a SPF y DKIM."),
        },
        "passwords": {
            "alto": ("¿Qué palabra representa una clave con muchos caracteres?", ["Larga", "Spam", "SSID"], 0, "Larga ayuda a reconocer la característica visual del banco."),
            "medio": ("¿Qué término corto representa autenticación en más de un paso?", ["MFA", "URL", "USB"], 0, "MFA es el vocabulario que aparecerá como respuesta."),
            "bajo": ("¿Qué término nombra el valor aleatorio usado antes del hash?", ["Salt", "DMARC", "VPN"], 0, "Salt se reconoce como palabra clave del proceso de hashing."),
        },
        "malware": {
            "alto": ("¿Qué palabra buscarías para software diseñado para dañar sistemas?", ["Malware", "HTTPS", "Gestor"], 0, "Malware es el término general del banco."),
            "medio": ("¿Qué término corresponde a malware que espía al usuario?", ["Spyware", "Passphrase", "DMARC"], 0, "Spyware es la palabra que debes ubicar visualmente."),
            "bajo": ("¿Qué palabra nombra un entorno aislado de análisis?", ["Sandbox", "Spear", "WPA2"], 0, "Sandbox es el término de reconocimiento para análisis aislado."),
        },
        "wifi": {
            "alto": ("¿Qué término buscarías para un servicio que protege la conexión?", ["VPN", "Hash", "Spam"], 0, "VPN es la palabra corta asociada a protección de conexión."),
            "medio": ("¿Qué término representa el nombre visible de una red?", ["SSID", "Botnet", "Salt"], 0, "SSID es el vocabulario que debes reconocer."),
            "bajo": ("¿Qué término identifica el estándar moderno de seguridad WiFi?", ["WPA3", "DKIM", "MFA"], 0, "WPA3 es una palabra clave del banco de redes."),
        },
    },
    "crossword": {
        "phishing": {
            "alto": ("La pista dice 'acción correcta ante correo sospechoso'. ¿Qué concepto encaja?", ["Reportar", "Rogue AP", "Salt"], 0, "La definición apunta a la acción, no al canal."),
            "medio": ("La pista habla de la parte que identifica al remitente. ¿Qué concepto relacionas?", ["Dominio", "Sandbox", "MFA"], 0, "Dominio conecta con identificación del sitio o remitente."),
            "bajo": ("La pista menciona una firma para verificar autenticidad del correo. ¿Qué término corresponde?", ["DKIM", "VPN", "Rootkit"], 0, "DKIM se asocia con firma digital de correo."),
        },
        "passwords": {
            "alto": ("La pista dice 'dato privado que no debe compartirse'. ¿Qué concepto corresponde?", ["Secreto", "Hotspot", "Spam"], 0, "Secreto se relaciona directamente con protección de claves o códigos."),
            "medio": ("La pista indica 'programa que almacena contraseñas seguras'. ¿Qué concepto encaja?", ["Gestor", "DKIM", "Botnet"], 0, "Gestor es el concepto asociado a almacenar claves."),
            "bajo": ("La pista dice 'resultado de aplicar una función criptográfica'. ¿Qué concepto corresponde?", ["Hash", "SSID", "Spam"], 0, "Hash es el resultado que se compara sin guardar la contraseña en claro."),
        },
        "malware": {
            "alto": ("La pista habla de un dispositivo que puede propagar amenazas. ¿Qué concepto encaja?", ["USB", "SPF", "Salt"], 0, "USB es el dispositivo externo evaluado."),
            "medio": ("La pista dice 'red de equipos infectados'. ¿Qué concepto corresponde?", ["Botnet", "URL", "MFA"], 0, "Botnet define esa red controlada por un atacante."),
            "bajo": ("La pista menciona mantenerse activo tras reinicio. ¿Qué concepto relacionas?", ["Persistencia", "HTTPS", "Gestor"], 0, "Persistencia describe la permanencia de la amenaza."),
        },
        "wifi": {
            "alto": ("La pista dice 'protocolo seguro para navegar'. ¿Qué concepto corresponde?", ["HTTPS", "Hash", "DKIM"], 0, "HTTPS se relaciona con navegación protegida."),
            "medio": ("La pista habla de un punto de acceso inalámbrico. ¿Qué término encaja?", ["Hotspot", "Rootkit", "Salt"], 0, "Hotspot es el punto de acceso descrito."),
            "bajo": ("La pista menciona un punto de acceso no autorizado. ¿Qué concepto corresponde?", ["Rogue AP", "MFA", "Virus"], 0, "Rogue AP define ese acceso no autorizado."),
        },
    },
}


PRACTICAL_EXAMPLES = {
    "quiz": {
        "title": "Decidir una respuesta segura en una PYME",
        "steps": [
            "Lee el caso completo antes de mirar las opciones.",
            "Identifica qué riesgo aparece y qué consecuencia tendría actuar sin verificar.",
            "Descarta opciones que pidan compartir datos, ignorar alertas o saltar controles.",
            "Elige la acción que reduzca el riesgo sin prometer seguridad absoluta.",
        ],
    },
    "wordsearch": {
        "title": "Reconocer vocabulario de seguridad",
        "steps": [
            "Revisa las pistas y detecta la palabra técnica que resumen.",
            "Busca el término visualmente en mayúsculas, sin depender de memorizar frases largas.",
            "Relaciona cada palabra encontrada con una situación real de trabajo.",
            "Si aparece una abreviatura, confirma qué concepto completo representa.",
        ],
    },
    "crossword": {
        "title": "Relacionar pista y concepto",
        "steps": [
            "Lee la definición de la pista y detecta la idea central.",
            "Piensa qué término técnico corresponde a esa definición.",
            "Usa cruces de letras para confirmar, pero no ignores el significado.",
            "Antes de escribir, verifica que el concepto encaje con la pista completa.",
        ],
    },
}


COMMON_MISTAKES = {
    "quiz": {
        "title": "Responder por familiaridad",
        "explanation": "Elegir la opción que suena conocida sin analizar consecuencias puede reforzar decisiones inseguras.",
    },
    "wordsearch": {
        "title": "Encontrar palabras sin comprenderlas",
        "explanation": "Ubicar un término ayuda poco si no se recuerda qué significa en una jornada de trabajo.",
    },
    "crossword": {
        "title": "Forzar una palabra por sus letras",
        "explanation": "Las letras cruzadas ayudan, pero la respuesta debe coincidir con la definición de la pista.",
    },
}


VISUAL_FOCUS = {
    "quiz": "quiz_decision",
    "wordsearch": "wordsearch_terms",
    "crossword": "crossword_definitions",
}


MINIGAME_CONCEPT_NOTES = {
    "quiz": "En el quiz importa decidir qué acción es más segura y por qué.",
    "wordsearch": "En la sopa de letras debes reconocer el término tal como podría aparecer en el banco.",
    "crossword": "En el crucigrama debes unir una definición breve con el concepto correcto.",
}


def _copy_quick_check(topic: str, risk: str, minigame: str) -> dict:
    question, options, correct_option, explanation = (
        MINIGAME_QUICK_CHECKS[minigame][topic][risk]
    )

    return {
        "question": question,
        "options": list(options),
        "correct_option": correct_option,
        "explanation": explanation,
    }


def _build_key_concepts(topic: str, risk: str, minigame: str) -> list:
    source = (
        QUIZ_CONCEPT_OVERRIDES.get(topic, {}).get(risk, CONCEPT_SETS[topic][risk])
        if minigame == "quiz"
        else WORD_PUZZLE_CONCEPT_SETS[topic][risk]
    )
    concepts = copy.deepcopy(source)
    note = MINIGAME_CONCEPT_NOTES[minigame]

    for concept in concepts:
        concept["why_it_matters"] = f"{concept['why_it_matters']} {note}"

    return concepts


def _build_supplement(topic: str, risk: str, minigame: str) -> dict:
    return {
        "key_concepts": _build_key_concepts(topic, risk, minigame),
        "practical_example": copy.deepcopy(PRACTICAL_EXAMPLES[minigame]),
        "common_mistake": copy.deepcopy(COMMON_MISTAKES[minigame]),
        "quick_check": _copy_quick_check(topic, risk, minigame),
        "visual_key": f"{topic}_{risk}_{VISUAL_FOCUS[minigame]}",
    }


MINIGAME_SUPPLEMENTS = {
    topic: {
        risk: {
            minigame: _build_supplement(topic, risk, minigame)
            for minigame in ("quiz", "wordsearch", "crossword")
        }
        for risk in ("alto", "medio", "bajo")
    }
    for topic in ("phishing", "passwords", "malware", "wifi")
}


LEARNING_CONTENT = BASE_CONTENT


def get_learning_content_for_concepts(
    topic: str,
    risk: str,
    minigame: str,
    concept_ids: list,
) -> dict:
    normalized_minigame = normalize_minigame(minigame)
    concepts = get_concepts(concept_ids)

    if not concepts:
        raise ValueError("At least one concept_id is required.")

    for concept in concepts:
        if concept["topic"] != topic:
            raise ValueError(
                f"Concept {concept['concept_id']} does not belong to topic {topic}."
            )

    if topic not in BASE_CONTENT or risk not in BASE_CONTENT[topic]:
        raise ValueError(f"Unsupported topic/risk combination: {topic}/{risk}.")

    lesson = copy.deepcopy(BASE_CONTENT[topic][risk])
    concept_terms = [concept["term"] for concept in concepts]
    concept_summary = ", ".join(concept_terms)

    lesson["explanation"] = (
        f"Esta microleccion prepara exactamente los conceptos que evaluara el "
        f"{_minigame_label(normalized_minigame)}: {concept_summary}. "
        f"{_build_concept_relation(concepts)}"
    )
    lesson["key_concepts"] = [
        _lesson_concept_from_catalog(concept, normalized_minigame)
        for concept in concepts
    ]
    lesson["practical_example"] = _build_catalog_practical_example(concepts)
    lesson["common_mistake"] = _build_catalog_common_mistake(concepts)
    lesson["quick_check"] = _build_catalog_quick_check(concepts)
    lesson["visual_key"] = (
        f"{topic}_{risk}_{normalized_minigame}_session_concepts"
    )
    lesson["topic"] = topic
    lesson["risk"] = risk
    lesson["minigame"] = normalized_minigame

    return lesson


def _minigame_label(minigame: str) -> str:
    labels = {
        "quiz": "quiz",
        "wordsearch": "sopa de letras",
        "crossword": "crucigrama",
    }
    return labels[minigame]


def _build_concept_relation(concepts: list) -> str:
    if len(concepts) == 1:
        return (
            "El objetivo es reconocer el concepto, entender para que sirve y "
            "aplicarlo en una situacion real de trabajo."
        )

    terms = ", ".join(concept["term"] for concept in concepts)
    return (
        f"Estos conceptos se estudian juntos porque las pistas o preguntas "
        f"pueden diferenciarlos por su funcion: {terms}."
    )


def _lesson_concept_from_catalog(concept: dict, minigame: str) -> dict:
    note = MINIGAME_CONCEPT_NOTES[minigame]
    return {
        "term": concept["term"],
        "definition": concept["definition"],
        "why_it_matters": f"{concept['explanation']} {note}",
        "example": (
            f"{concept['practical_example']} "
            f"Pista de reconocimiento: {concept['recognition_clue']} "
            f"Error frecuente: {concept['common_mistake']}"
        ),
    }


def _build_catalog_practical_example(concepts: list) -> dict:
    steps = [
        f"{concept['term']}: {concept['practical_example']}"
        for concept in concepts[:4]
    ]

    while len(steps) < 3:
        steps.append(
            "Relaciona cada pista con el significado del concepto antes de responder."
        )

    return {
        "title": "Aplicar los conceptos seleccionados en una PYME",
        "steps": steps,
    }


def _build_catalog_common_mistake(concepts: list) -> dict:
    mistakes = [
        f"{concept['term']}: {concept['common_mistake']}"
        for concept in concepts
    ]

    return {
        "title": "Errores frecuentes al reconocer estos conceptos",
        "explanation": " ".join(mistakes),
    }


def _build_catalog_quick_check(concepts: list) -> dict:
    concept = concepts[0]
    return {
        "question": (
            f"Si la pista dice: '{concept['recognition_clue']}', "
            "que concepto debes reconocer?"
        ),
        "options": [
            concept["term"],
            "Una accion no relacionada",
            "Un dato sin verificar",
        ],
        "correct_option": 0,
        "explanation": (
            f"La pista corresponde a {concept['term']}: "
            f"{concept['definition']}"
        ),
    }


def get_learning_content(topic: str, risk: str, minigame: str) -> dict:
    topic_content = BASE_CONTENT.get(topic)

    if topic_content is None or risk not in topic_content:
        logger.warning("Learning content fallback applied for unsupported topic/risk")
        topic = "phishing"
        risk = "alto"
        topic_content = BASE_CONTENT[topic]

    normalized_minigame = normalize_minigame(minigame)

    lesson = copy.deepcopy(topic_content[risk])
    supplement = copy.deepcopy(
        MINIGAME_SUPPLEMENTS[topic][risk][normalized_minigame]
    )

    lesson.update(supplement)
    lesson["topic"] = topic
    lesson["risk"] = risk
    lesson["minigame"] = normalized_minigame

    return lesson
