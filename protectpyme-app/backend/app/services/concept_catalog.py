from copy import deepcopy


VALID_TOPICS = {"phishing", "passwords", "malware", "wifi"}
VALID_DIFFICULTIES = {"alto", "medio", "bajo"}
REQUIRED_FIELDS = {
    "concept_id",
    "topic",
    "term",
    "aliases",
    "difficulty",
    "definition",
    "explanation",
    "practical_example",
    "common_mistake",
    "recognition_clue",
}


def _concept(
    concept_id,
    topic,
    term,
    aliases,
    difficulty,
    definition,
    explanation,
    practical_example,
    common_mistake,
    recognition_clue,
):
    return {
        "concept_id": concept_id,
        "topic": topic,
        "term": term,
        "aliases": aliases,
        "difficulty": difficulty,
        "definition": definition,
        "explanation": explanation,
        "practical_example": practical_example,
        "common_mistake": common_mistake,
        "recognition_clue": recognition_clue,
    }


CONCEPT_CATALOG = {
    "phishing.phishing": _concept(
        "phishing.phishing",
        "phishing",
        "Phishing",
        ["PHISHING", "Phishing"],
        "alto",
        "Ataque por engano que intenta robar informacion o credenciales.",
        "En una PYME suele llegar por correo, mensaje o enlace que parece confiable. La defensa empieza al revisar remitente, urgencia y destino real del enlace.",
        "Un correo simula ser del banco y pide actualizar la clave de acceso.",
        "Creer que un mensaje es seguro solo porque usa el logo de una empresa conocida.",
        "Ataque por engano para robar informacion.",
    ),
    "phishing.reportar": _concept(
        "phishing.reportar",
        "phishing",
        "Reportar",
        ["REPORTAR", "Reportar"],
        "alto",
        "Accion de avisar a soporte o al canal responsable sobre un mensaje sospechoso.",
        "Reportar permite que la empresa bloquee campanas y alerte a otros empleados antes de que alguien caiga.",
        "Reenvias el correo sospechoso al canal interno indicado sin abrir sus enlaces.",
        "Borrar el mensaje sin avisar, dejando que otros empleados reciban el mismo ataque.",
        "Accion correcta ante un correo sospechoso.",
    ),
    "phishing.enlace": _concept(
        "phishing.enlace",
        "phishing",
        "Enlace",
        ["ENLACE", "Enlace"],
        "alto",
        "Direccion o boton dentro de un mensaje que puede llevar a un sitio falso.",
        "Los enlaces pueden esconder el destino real. Antes de hacer clic conviene revisar la URL completa y confirmar si era esperada.",
        "Un boton dice portal de nomina, pero apunta a un dominio desconocido.",
        "Confiar en el texto visible del boton sin revisar la direccion real.",
        "Elemento peligroso dentro de un correo fraudulento.",
    ),
    "phishing.dominio": _concept(
        "phishing.dominio",
        "phishing",
        "Dominio",
        ["DOMINIO", "Dominio"],
        "medio",
        "Parte de una direccion que identifica el sitio o remitente principal.",
        "Un dominio alterado puede imitar a un proveedor o a la empresa. Revisarlo ayuda a detectar suplantacion.",
        "empresa.com no es lo mismo que empresa-soporte.com.",
        "Mirar solo el nombre mostrado del remitente y no el dominio real.",
        "Parte del correo que ayuda a identificar al remitente.",
    ),
    "phishing.url": _concept(
        "phishing.url",
        "phishing",
        "URL",
        ["URL", "Url"],
        "medio",
        "Direccion completa de una pagina o recurso en internet.",
        "La URL muestra el destino real de un enlace. Si no coincide con el servicio esperado, puede ser una pagina falsa.",
        "Un enlace de pago apunta a un dominio que no pertenece al proveedor.",
        "Abrir una URL acortada o desconocida sin verificar su destino.",
        "Direccion web que debe revisarse antes de hacer clic.",
    ),
    "phishing.spam": _concept(
        "phishing.spam",
        "phishing",
        "Spam",
        ["SPAM", "Spam"],
        "medio",
        "Correo no deseado que puede ser molesto o riesgoso.",
        "No todo spam es phishing, pero algunos mensajes masivos contienen enlaces o archivos peligrosos.",
        "Un correo masivo ofrece premios y adjunta un archivo inesperado.",
        "Pensar que el spam solo es publicidad y nunca representa riesgo.",
        "Correo no deseado o sospechoso.",
    ),
    "phishing.dmarc": _concept(
        "phishing.dmarc",
        "phishing",
        "DMARC",
        ["DMARC", "Dmarc"],
        "bajo",
        "Politica de correo que usa SPF y DKIM para decidir como tratar mensajes sospechosos.",
        "DMARC ayuda a reducir la suplantacion del dominio de la empresa al indicar si mensajes que fallan validaciones deben rechazarse o marcarse.",
        "La organizacion configura DMARC para rechazar correos que no pasen SPF o DKIM.",
        "Pensar que DMARC reemplaza la revision humana de correos sospechosos.",
        "Mecanismo usado para validar correos legitimos.",
    ),
    "phishing.spf": _concept(
        "phishing.spf",
        "phishing",
        "SPF",
        ["SPF", "Spf"],
        "bajo",
        "Registro que indica que servidores pueden enviar correo por un dominio.",
        "SPF permite detectar mensajes enviados desde servidores no autorizados para representar a la empresa.",
        "El dominio define que proveedor puede mandar facturas en nombre de la empresa.",
        "Creer que SPF por si solo garantiza que el contenido del correo es seguro.",
        "Registro que ayuda a validar servidores de correo.",
    ),
    "phishing.dkim": _concept(
        "phishing.dkim",
        "phishing",
        "DKIM",
        ["DKIM", "Dkim"],
        "bajo",
        "Firma digital que ayuda a verificar autenticidad e integridad del correo.",
        "DKIM permite comprobar que el mensaje no fue alterado y que esta asociado al dominio que lo firma.",
        "El servidor receptor revisa la firma antes de confiar en el mensaje.",
        "Pensar que DKIM confirma que el enlace dentro del correo es seguro.",
        "Firma usada para verificar autenticidad del correo.",
    ),
    "phishing.spear_phishing": _concept(
        "phishing.spear_phishing",
        "phishing",
        "Spear phishing",
        ["SPEARPHISHING", "Spear phishing"],
        "bajo",
        "Phishing dirigido a una persona, area u organizacion especifica.",
        "Usa datos reales del contexto laboral para parecer mas confiable y presionar una accion concreta.",
        "Un mensaje menciona a tu jefe y solicita revisar una factura urgente.",
        "Confiar porque el mensaje contiene nombres reales de la empresa.",
        "Phishing dirigido a una persona u organizacion especifica.",
    ),
    "passwords.password": _concept(
        "passwords.password",
        "passwords",
        "Contrasena",
        ["PASSWORD", "Contrasena"],
        "alto",
        "Clave usada para entrar a una cuenta o servicio.",
        "Es una barrera inicial contra accesos no autorizados. Debe ser unica, privada y dificil de adivinar.",
        "La cuenta de ventas requiere una clave distinta a la del correo.",
        "Usar la misma contrasena para varias cuentas.",
        "Clave usada para entrar a una cuenta.",
    ),
    "passwords.secreto": _concept(
        "passwords.secreto",
        "passwords",
        "Secreto",
        ["SECRETO", "Secreto"],
        "alto",
        "Dato privado que no debe compartirse por chat, correo o telefono.",
        "Incluye claves, codigos temporales y tokens. Compartirlo rompe el control de acceso aunque la contrasena sea fuerte.",
        "Un codigo temporal de acceso no se manda a otra persona.",
        "Enviar codigos o claves por mensajeria para resolver algo rapido.",
        "Codigo o clave que no debe compartirse.",
    ),
    "passwords.larga": _concept(
        "passwords.larga",
        "passwords",
        "Larga",
        ["LARGA", "Larga"],
        "alto",
        "Caracteristica de una contrasena con suficientes caracteres.",
        "La longitud aumenta el esfuerzo necesario para probar combinaciones y reduce el riesgo de adivinacion.",
        "Una frase extensa y unica es mejor que una palabra corta.",
        "Pensar que una palabra comun con un numero al final es suficiente.",
        "Caracteristica recomendada en una contrasena.",
    ),
    "passwords.reutilizacion": _concept(
        "passwords.reutilizacion",
        "passwords",
        "Reutilizacion",
        ["REUTILIZACION", "Reutilizacion"],
        "alto",
        "Uso de la misma contrasena en varias cuentas.",
        "Si una cuenta se filtra, un atacante puede probar la misma clave en otros servicios de la empresa.",
        "La clave filtrada de una tienda tambien abre el correo corporativo.",
        "Creer que repetir la contrasena es seguro si parece fuerte.",
        "Riesgo de usar una contrasena en mas de una cuenta.",
    ),
    "passwords.mfa": _concept(
        "passwords.mfa",
        "passwords",
        "MFA",
        ["MFA", "Mfa"],
        "medio",
        "Autenticacion que pide mas de un factor para entrar.",
        "MFA reduce el riesgo si una contrasena queda expuesta porque exige una verificacion adicional.",
        "Ademas de la clave, el acceso se aprueba en una app autorizada.",
        "Aprobar solicitudes MFA que no se iniciaron personalmente.",
        "Autenticacion con mas de un factor.",
    ),
    "passwords.gestor": _concept(
        "passwords.gestor",
        "passwords",
        "Gestor",
        ["GESTOR", "Gestor"],
        "medio",
        "Programa para guardar y generar contrasenas de forma segura.",
        "Ayuda a usar claves unicas sin memorizarlas ni anotarlas en lugares inseguros.",
        "El gestor crea una clave distinta para cada proveedor.",
        "Guardar contrasenas en notas, hojas de calculo o chats.",
        "Programa que almacena contrasenas seguras.",
    ),
    "passwords.passphrase": _concept(
        "passwords.passphrase",
        "passwords",
        "Passphrase",
        ["PASSPHRASE", "Passphrase"],
        "medio",
        "Frase larga usada como contrasena.",
        "Combina longitud con facilidad de recuerdo si no contiene datos personales o frases obvias.",
        "Una frase interna extensa protege mejor que una palabra corta.",
        "Usar una frase famosa o facil de adivinar.",
        "Frase usada como contrasena larga.",
    ),
    "passwords.salt": _concept(
        "passwords.salt",
        "passwords",
        "Salt",
        ["SALT", "Salt"],
        "bajo",
        "Valor aleatorio unico agregado antes de generar el hash de una contrasena.",
        "El salt hace que contrasenas iguales produzcan valores almacenados distintos. Normalmente se combina con funciones como Argon2id.",
        "Dos empleados con la misma clave no tendrian el mismo valor guardado porque cada cuenta usa un salt diferente.",
        "Confundir salt con una parte secreta que el usuario debe recordar.",
        "Valor aleatorio agregado antes del hash.",
    ),
    "passwords.hash": _concept(
        "passwords.hash",
        "passwords",
        "Hash",
        ["HASH", "Hash"],
        "bajo",
        "Resultado de aplicar una funcion criptografica para no guardar la contrasena original.",
        "El sistema compara valores derivados para verificar la clave. Un hash de contrasena no debe presentarse como cifrado reversible.",
        "Al iniciar sesion, el sistema calcula un nuevo valor y lo compara con el almacenado.",
        "Pensar que desde el hash se puede recuperar directamente la contrasena.",
        "Resultado de aplicar una funcion criptografica.",
    ),
    "passwords.argon2id": _concept(
        "passwords.argon2id",
        "passwords",
        "Argon2id",
        ["ARGON2ID", "Argon2id"],
        "bajo",
        "Funcion de derivacion de claves disenada para almacenar contrasenas de manera segura.",
        "Argon2id no almacena la contrasena original. Produce un valor derivado para verificarla despues, usa memoria y tiempo de procesamiento para dificultar intentos masivos, normalmente se combina con un salt y no es cifrado reversible.",
        "Cuando una persona crea una cuenta, el sistema aplica Argon2id a su contrasena junto con un salt y almacena el resultado, no la contrasena original.",
        "Confundir Argon2id con cifrado y pensar que la contrasena puede recuperarse directamente desde el valor almacenado.",
        "Funcion moderna y resistente para almacenar contrasenas.",
    ),
    "passwords.password_spraying": _concept(
        "passwords.password_spraying",
        "passwords",
        "Password spraying",
        ["PASSWORDSPRAYING", "Password spraying"],
        "bajo",
        "Ataque que prueba una contrasena comun en muchas cuentas.",
        "Busca evitar bloqueos por demasiados intentos en una sola cuenta. MFA y contrasenas unicas reducen su impacto.",
        "Un atacante prueba 'Empresa2026' contra muchos usuarios.",
        "Creer que solo importa bloquear intentos repetidos contra una misma cuenta.",
        "Probar una contrasena comun en muchas cuentas.",
    ),
    "malware.malware": _concept(
        "malware.malware",
        "malware",
        "Malware",
        ["MALWARE", "Malware"],
        "alto",
        "Software disenado para danar, espiar o controlar un equipo sin autorizacion.",
        "Puede llegar por archivos adjuntos, descargas o dispositivos externos y afectar continuidad, datos y credenciales.",
        "Un adjunto falso instala un programa no autorizado.",
        "Abrir archivos inesperados porque parecen venir de un contacto conocido.",
        "Software malicioso.",
    ),
    "malware.virus": _concept(
        "malware.virus",
        "malware",
        "Virus",
        ["VIRUS", "Virus"],
        "alto",
        "Tipo de malware que puede propagarse al ejecutar archivos infectados.",
        "Reconocerlo ayuda a entender que un archivo aparentemente normal puede afectar otros documentos o equipos.",
        "Un archivo compartido infecta otros documentos al abrirse.",
        "Pensar que todos los problemas de malware son simplemente virus.",
        "Tipo de software malicioso.",
    ),
    "malware.antivirus": _concept(
        "malware.antivirus",
        "malware",
        "Antivirus",
        ["ANTIVIRUS", "Antivirus"],
        "alto",
        "Herramienta que ayuda a detectar y bloquear software malicioso.",
        "Es un apoyo importante, pero no reemplaza la verificacion del usuario ni los canales de soporte.",
        "Una alerta indica que no abras un archivo descargado.",
        "Ignorar una alerta porque el archivo parece urgente.",
        "Programa que protege contra virus.",
    ),
    "malware.usb": _concept(
        "malware.usb",
        "malware",
        "USB",
        ["USB", "Usb"],
        "alto",
        "Dispositivo externo que puede transportar archivos o amenazas.",
        "Una USB desconocida puede iniciar una infeccion o copiar informacion si se conecta sin revision.",
        "Una memoria encontrada se entrega a soporte sin conectarla.",
        "Conectar una USB para ver de quien es.",
        "Dispositivo que puede propagar amenazas.",
    ),
    "malware.ransomware": _concept(
        "malware.ransomware",
        "malware",
        "Ransomware",
        ["RANSOMWARE", "Ransomware"],
        "medio",
        "Malware que cifra o bloquea archivos para exigir un pago.",
        "Puede detener operaciones y afectar informacion critica de la empresa.",
        "Las carpetas compartidas quedan bloqueadas y aparece una nota de rescate.",
        "Pagar sin reportar ni aislar el equipo afectado.",
        "Malware que cifra archivos.",
    ),
    "malware.spyware": _concept(
        "malware.spyware",
        "malware",
        "Spyware",
        ["SPYWARE", "Spyware"],
        "medio",
        "Software que recopila informacion del usuario sin autorizacion.",
        "Puede robar credenciales, informacion de clientes o actividad del equipo.",
        "Un programa falso registra lo que se escribe en el equipo.",
        "Instalar herramientas desconocidas porque prometen mejorar el rendimiento.",
        "Programa que espia al usuario.",
    ),
    "malware.botnet": _concept(
        "malware.botnet",
        "malware",
        "Botnet",
        ["BOTNET", "Botnet"],
        "medio",
        "Red de equipos infectados controlados por un atacante.",
        "Un equipo de la empresa puede usarse para enviar trafico malicioso sin que el usuario lo note.",
        "Una computadora envia trafico extrano en segundo plano.",
        "Ignorar actividad de red anormal porque el equipo aun funciona.",
        "Red de equipos infectados.",
    ),
    "malware.rootkit": _concept(
        "malware.rootkit",
        "malware",
        "Rootkit",
        ["ROOTKIT", "Rootkit"],
        "bajo",
        "Malware que intenta ocultar su presencia en el sistema.",
        "Puede dificultar la deteccion y limpieza porque esconde procesos o cambios internos.",
        "El equipo parece normal aunque mantiene procesos ocultos.",
        "Asumir que no hay infeccion solo porque no se ve una ventana sospechosa.",
        "Malware que intenta ocultarse en el sistema.",
    ),
    "malware.sandbox": _concept(
        "malware.sandbox",
        "malware",
        "Sandbox",
        ["SANDBOX", "Sandbox"],
        "bajo",
        "Entorno aislado y autorizado para analizar archivos sospechosos.",
        "Permite observar un archivo sin exponer sistemas reales de la empresa.",
        "Soporte analiza un adjunto en un laboratorio controlado.",
        "Abrir archivos sospechosos en el equipo personal para probarlos.",
        "Entorno aislado para analizar archivos.",
    ),
    "malware.persistencia": _concept(
        "malware.persistencia",
        "malware",
        "Persistencia",
        ["PERSISTENCIA", "Persistencia"],
        "bajo",
        "Capacidad de una amenaza para mantenerse activa despues de reiniciar.",
        "Indica que el malware puede volver si no se elimina correctamente.",
        "Un programa reaparece cada vez que se prende la computadora.",
        "Reiniciar el equipo y asumir que la amenaza desaparecio.",
        "Capacidad de mantenerse activo tras reinicio.",
    ),
    "wifi.wifi_publica": _concept(
        "wifi.wifi_publica",
        "wifi",
        "WiFi publica",
        ["WIFI", "WiFi publica"],
        "alto",
        "Red inalambrica disponible para muchas personas o fuera del control directo de la empresa.",
        "Puede exponer datos si se usa sin verificar el nombre de la red y sin protecciones adicionales.",
        "Una red de cafeteria se usa solo para tareas no sensibles o con VPN.",
        "Creer que una red publica es segura porque no pide contrasena.",
        "Red inalambrica de uso compartido.",
    ),
    "wifi.datos_sensibles": _concept(
        "wifi.datos_sensibles",
        "wifi",
        "Datos sensibles",
        ["DATOSSENSIBLES", "Datos sensibles"],
        "alto",
        "Informacion que requiere proteccion, como datos de clientes, pagos o accesos internos.",
        "En redes publicas, enviar datos sensibles sin HTTPS o VPN aumenta la exposicion.",
        "Se evita abrir un sistema de clientes desde una red desconocida.",
        "Ingresar informacion confidencial solo porque la pagina cargo correctamente.",
        "Ingresar datos sensibles sin proteccion.",
    ),
    "wifi.vpn": _concept(
        "wifi.vpn",
        "wifi",
        "VPN",
        ["VPN", "Vpn"],
        "alto",
        "Servicio que protege la conexion hacia recursos autorizados.",
        "Agrega una capa de proteccion al trabajar fuera de la oficina, especialmente para sistemas internos.",
        "Se activa la VPN antes de consultar un sistema interno desde una red de visitas.",
        "Creer que la VPN vuelve confiable cualquier sitio o descarga.",
        "Servicio que protege la conexion.",
    ),
    "wifi.https": _concept(
        "wifi.https",
        "wifi",
        "HTTPS",
        ["HTTPS", "Https"],
        "alto",
        "Protocolo que protege la comunicacion con un sitio web.",
        "Ayuda a evitar que otros vean o alteren informacion enviada, aunque no garantiza que el sitio sea legitimo.",
        "El navegador muestra conexion segura al entrar al portal del proveedor.",
        "Confiar en cualquier pagina solo porque muestra HTTPS.",
        "Protocolo seguro para navegar.",
    ),
    "wifi.ssid": _concept(
        "wifi.ssid",
        "wifi",
        "SSID",
        ["SSID", "Ssid"],
        "medio",
        "Nombre visible de una red inalambrica.",
        "Comparar el SSID con el nombre oficial ayuda a evitar redes imitadas.",
        "Oficina_Invitados no es igual a Oficina-Invitados.",
        "Conectarse a la red con nombre parecido sin confirmar.",
        "Nombre visible de una red inalambrica.",
    ),
    "wifi.hotspot": _concept(
        "wifi.hotspot",
        "wifi",
        "Hotspot",
        ["HOTSPOT", "Hotspot"],
        "medio",
        "Punto de acceso que ofrece conexion WiFi.",
        "Puede ser legitimo o falso segun quien lo controle. Debe validarse antes de usarlo para trabajo.",
        "Un celular corporativo comparte internet como hotspot temporal autorizado.",
        "Usar cualquier hotspot cercano porque tiene buena senal.",
        "Punto de acceso inalambrico.",
    ),
    "wifi.wpa2": _concept(
        "wifi.wpa2",
        "wifi",
        "WPA2",
        ["WPA2", "Wpa2"],
        "medio",
        "Estandar de seguridad usado para proteger redes inalambricas.",
        "Indica mejor proteccion que una red abierta, aunque aun requiere claves y configuracion adecuadas.",
        "La red de oficina usa WPA2 con una clave administrada.",
        "Compartir la clave de la red protegida con personas no autorizadas.",
        "Estandar de seguridad inalambrica.",
    ),
    "wifi.evil_twin": _concept(
        "wifi.evil_twin",
        "wifi",
        "Evil Twin",
        ["EVILTWIN", "Evil Twin"],
        "bajo",
        "Red falsa que imita una red legitima.",
        "Engana al usuario para conectarse a un punto controlado por otra persona.",
        "Una red copia el nombre del WiFi del hotel para capturar accesos.",
        "Confiar solo porque el nombre de la red coincide con el esperado.",
        "Red falsa que imita una legitima.",
    ),
    "wifi.rogue_ap": _concept(
        "wifi.rogue_ap",
        "wifi",
        "Rogue AP",
        ["ROGUEAP", "Rogue AP"],
        "bajo",
        "Punto de acceso no autorizado dentro o cerca de la organizacion.",
        "Puede abrir una entrada insegura a la red o confundir a empleados.",
        "Alguien conecta un router personal sin permiso en la oficina.",
        "Permitir puntos de acceso porque facilitan conexiones temporales.",
        "Punto de acceso no autorizado.",
    ),
    "wifi.wpa3": _concept(
        "wifi.wpa3",
        "wifi",
        "WPA3",
        ["WPA3", "Wpa3"],
        "bajo",
        "Estandar moderno que mejora la seguridad de redes WiFi.",
        "Reduce riesgos frente a ataques comunes contra redes inalambricas cuando los equipos lo soportan.",
        "Un router nuevo de la empresa se configura con WPA3 cuando es posible.",
        "Pensar que WPA3 elimina la necesidad de buenas claves y administracion.",
        "Estandar moderno de seguridad WiFi.",
    ),
}


def _alias_key(alias):
    return str(alias).strip().lower()


def validate_concept_catalog():
    errors = []
    seen_aliases = {}

    for concept_id, concept in CONCEPT_CATALOG.items():
        missing_fields = REQUIRED_FIELDS - set(concept)
        if missing_fields:
            errors.append(f"{concept_id}: missing fields {sorted(missing_fields)}")
            continue

        if concept["concept_id"] != concept_id:
            errors.append(f"{concept_id}: concept_id does not match dictionary key")

        if concept["topic"] not in VALID_TOPICS:
            errors.append(f"{concept_id}: invalid topic {concept['topic']}")

        if concept["difficulty"] not in VALID_DIFFICULTIES:
            errors.append(f"{concept_id}: invalid difficulty {concept['difficulty']}")

        for field in REQUIRED_FIELDS - {"aliases"}:
            if not str(concept[field]).strip():
                errors.append(f"{concept_id}: empty field {field}")

        aliases = concept["aliases"]
        if not isinstance(aliases, list) or not aliases:
            errors.append(f"{concept_id}: aliases must be a non-empty list")
            continue

        for alias in aliases:
            alias_key = _alias_key(alias)
            if not alias_key:
                errors.append(f"{concept_id}: empty alias")
                continue

            previous_concept_id = seen_aliases.get(alias_key)
            if previous_concept_id and previous_concept_id != concept_id:
                errors.append(
                    f"{concept_id}: alias {alias} also used by {previous_concept_id}"
                )
            seen_aliases[alias_key] = concept_id

    if errors:
        raise ValueError("Invalid concept catalog: " + "; ".join(errors))

    return True


def get_concept(concept_id):
    try:
        return deepcopy(CONCEPT_CATALOG[concept_id])
    except KeyError as exc:
        raise KeyError(f"Concept id not found: {concept_id}") from exc


def get_concepts(concept_ids):
    selected = []
    seen = set()

    for concept_id in concept_ids:
        if concept_id in seen:
            continue

        selected.append(get_concept(concept_id))
        seen.add(concept_id)

    return selected


validate_concept_catalog()
