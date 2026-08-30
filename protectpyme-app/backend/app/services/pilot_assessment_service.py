from datetime import datetime
import uuid

from sqlalchemy.exc import IntegrityError
from sqlalchemy import func
from sqlalchemy.orm import Session

from app import models, schemas
from app.services import pilot_service


INSTRUMENT_VERSION = "pilot_assessment_v1"
TOPICS = ("phishing", "passwords", "malware", "wifi")
PHASE_FORMS = {
    "PRE": "A",
    "POST": "B",
}
OPTION_KEYS = ("A", "B", "C", "D")
QUESTIONS_PER_TOPIC = 3
TOTAL_QUESTIONS = 12
REQUIRED_DISTINCT_SCENARIOS_FOR_POST = 3
REQUIRED_MINIGAME_SESSIONS_FOR_POST = 1


class PilotAssessmentPermissionError(Exception):
    pass


class PilotAssessmentNotFoundError(Exception):
    pass


class PilotAssessmentValidationError(Exception):
    pass


class PilotAssessmentConflictError(Exception):
    pass


QUESTION_BANK = {
    INSTRUMENT_VERSION: {
        "A": [
            {
                "question_id": "pre_phishing_01",
                "form": "A",
                "topic": "phishing",
                "construct": "senales_phishing",
                "prompt": "Recibes un correo de un proveedor que pide pagar hoy para evitar una multa. Incluye un enlace inesperado. ¿Qué haces primero?",
                "options": [
                    "Abrir el enlace porque la multa parece urgente",
                    "Revisar remitente, dominio y contexto antes de actuar",
                    "Responder con los datos de pago para ganar tiempo",
                    "Reenviar el enlace a todo el equipo sin comentario",
                ],
                "correct_option": "B",
            },
            {
                "question_id": "pre_phishing_02",
                "form": "A",
                "topic": "phishing",
                "construct": "dominio_url",
                "prompt": "Un boton dice 'portal de nomina', pero la direccion apunta a empresa-soporte.com. ¿Cuál es la mejor decision?",
                "options": [
                    "Entrar porque el texto del boton menciona nomina",
                    "Compartir la pagina para que alguien mas pruebe",
                    "Verificar el dominio por un canal oficial antes de ingresar datos",
                    "Guardar la contraseña en el portal para no repetirla",
                ],
                "correct_option": "C",
            },
            {
                "question_id": "pre_phishing_03",
                "form": "A",
                "topic": "phishing",
                "construct": "reporte_accion_segura",
                "prompt": "Varias personas de la empresa reciben el mismo correo dudoso con un archivo adjunto. ¿Qué accion protege mejor a la organizacion?",
                "options": [
                    "Borrar el correo sin avisar a nadie",
                    "Abrir el archivo en un equipo personal",
                    "Responder al remitente para pedir explicaciones",
                    "Reportarlo al canal interno responsable sin abrir el adjunto",
                ],
                "correct_option": "D",
            },
            {
                "question_id": "pre_passwords_01",
                "form": "A",
                "topic": "passwords",
                "construct": "secreto_credenciales",
                "prompt": "Una persona por telefono dice ser de soporte y pide tu codigo temporal para resolver una urgencia. ¿Qué haces?",
                "options": [
                    "No compartir el codigo y reportar la solicitud",
                    "Dictar solo los ultimos numeros del codigo",
                    "Enviar el codigo por chat interno",
                    "Compartirlo si conoce el nombre de la empresa",
                ],
                "correct_option": "A",
            },
            {
                "question_id": "pre_passwords_02",
                "form": "A",
                "topic": "passwords",
                "construct": "mfa_verificacion",
                "prompt": "Aparece una solicitud MFA que no iniciaste. ¿Cuál es la respuesta mas segura?",
                "options": [
                    "Aprobarla para cerrar la notificacion",
                    "Rechazarla y avisar al canal oficial de TI",
                    "Ignorarla siempre que no vuelva a salir",
                    "Compartir una captura con un grupo abierto",
                ],
                "correct_option": "B",
            },
            {
                "question_id": "pre_passwords_03",
                "form": "A",
                "topic": "passwords",
                "construct": "contrasena_unica_larga",
                "prompt": "Un sistema de ventas usa la misma contraseña que una cuenta externa filtrada. ¿Qué practica reduce mejor el riesgo?",
                "options": [
                    "Mantenerla si nadie ha entrado todavia",
                    "Cambiar solo el nombre de usuario",
                    "Usar una contraseña unica y larga para cada servicio",
                    "Compartir la clave con el equipo para monitorear",
                ],
                "correct_option": "C",
            },
            {
                "question_id": "pre_malware_01",
                "form": "A",
                "topic": "malware",
                "construct": "usb_archivo_desconocido",
                "prompt": "Encuentras una USB en recepcion con una etiqueta de 'clientes'. ¿Qué accion es mas segura?",
                "options": [
                    "Conectarla para identificar al dueño",
                    "Copiar los archivos importantes",
                    "Entregarla a soporte sin conectarla",
                    "Abrirla solo unos segundos",
                ],
                "correct_option": "C",
            },
            {
                "question_id": "pre_malware_02",
                "form": "A",
                "topic": "malware",
                "construct": "ransomware_malware",
                "prompt": "Un equipo muestra archivos bloqueados y una nota que exige pago. ¿Qué riesgo describe mejor la situacion?",
                "options": [
                    "Ransomware o malware que puede afectar la operacion",
                    "Un cambio normal de contraseña",
                    "Una actualizacion pendiente que conviene instalar",
                    "Un error temporal que se resuelve reiniciando",
                ],
                "correct_option": "A",
            },
            {
                "question_id": "pre_malware_03",
                "form": "A",
                "topic": "malware",
                "construct": "respuesta_segura",
                "prompt": "El antivirus alerta sobre una descarga inesperada que ibas a ejecutar. ¿Qué decision reduce el riesgo?",
                "options": [
                    "Ejecutarla para confirmar si funciona",
                    "Detenerse, no abrirla y pedir apoyo al area responsable",
                    "Moverla a otra carpeta para revisarla despues",
                    "Desactivar el antivirus temporalmente",
                ],
                "correct_option": "B",
            },
            {
                "question_id": "pre_wifi_01",
                "form": "A",
                "topic": "wifi",
                "construct": "conexion_segura",
                "prompt": "Necesitas consultar datos de clientes desde una red publica. ¿Qué decision es mas segura?",
                "options": [
                    "Enviar los datos si la red tiene buena señal",
                    "Evitar datos sensibles o usar conexion protegida autorizada",
                    "Usar cualquier portal que cargue rapido",
                    "Desactivar la VPN para navegar mas rapido",
                ],
                "correct_option": "B",
            },
            {
                "question_id": "pre_wifi_02",
                "form": "A",
                "topic": "wifi",
                "construct": "red_falsa_ssid",
                "prompt": "Ves dos redes con nombres casi iguales al de la oficina. ¿Qué haces antes de conectarte?",
                "options": [
                    "Elegir la primera que aparece",
                    "Conectarte a la que no pide clave",
                    "Confirmar el SSID oficial por un canal confiable",
                    "Probar ambas y quedarte con la mas rapida",
                ],
                "correct_option": "C",
            },
            {
                "question_id": "pre_wifi_03",
                "form": "A",
                "topic": "wifi",
                "construct": "trafico_exfiltracion",
                "prompt": "Una alerta indica trafico saliente inusual desde un equipo con archivos internos. ¿Qué accion corresponde?",
                "options": [
                    "Ignorarla si internet sigue funcionando",
                    "Esperar a que se repita antes de avisar",
                    "Revisar, bloquear o aislar y reportar el caso",
                    "Compartir la red con otros dispositivos",
                ],
                "correct_option": "C",
            },
        ],
        "B": [
            {
                "question_id": "post_phishing_01",
                "form": "B",
                "topic": "phishing",
                "construct": "senales_phishing",
                "prompt": "Un mensaje de compras menciona un pedido que no reconoces y pide validar datos en una pagina externa. ¿Qué señal justifica detenerse y verificar?",
                "options": [
                    "La combinacion de pedido no reconocido y solicitud de datos externa",
                    "Que el mensaje tenga un asunto breve",
                    "Que el texto incluya el nombre del departamento",
                    "Que llegue antes de terminar la jornada",
                ],
                "correct_option": "A",
            },
            {
                "question_id": "post_phishing_02",
                "form": "B",
                "topic": "phishing",
                "construct": "dominio_url",
                "prompt": "Un enlace visible parece ser del proveedor, pero la URL real apunta a un sitio desconocido. ¿Qué haces?",
                "options": [
                    "Ingresar porque el proveedor es conocido",
                    "Verificar la direccion real por un canal oficial",
                    "Enviar tus credenciales para confirmar acceso",
                    "Abrirlo desde otro navegador sin revisar",
                ],
                "correct_option": "B",
            },
            {
                "question_id": "post_phishing_03",
                "form": "B",
                "topic": "phishing",
                "construct": "reporte_accion_segura",
                "prompt": "Un compañero recibe un documento compartido que pide habilitar macros para ver una supuesta cotizacion. ¿Cuál es la mejor accion?",
                "options": [
                    "Reportarlo al canal interno indicado sin abrir ni habilitar el contenido",
                    "Abrirlo en tu equipo para confirmar si la cotizacion existe",
                    "Responder al remitente para pedir que reenvie el archivo",
                    "Reenviarlo al area completa para que alguien lo revise",
                ],
                "correct_option": "A",
            },
            {
                "question_id": "post_passwords_01",
                "form": "B",
                "topic": "passwords",
                "construct": "secreto_credenciales",
                "prompt": "Un chat aparentemente interno pide reenviar una contraseña temporal para revisar una cuenta. ¿Qué haces?",
                "options": [
                    "Compartirla si el mensaje parece amable",
                    "Enviar solo una parte de la contraseña",
                    "No entregarla y escalar la solicitud sospechosa",
                    "Publicarla en el chat del area",
                ],
                "correct_option": "C",
            },
            {
                "question_id": "post_passwords_02",
                "form": "B",
                "topic": "passwords",
                "construct": "mfa_verificacion",
                "prompt": "Mientras no intentas entrar a ninguna cuenta, aparece una solicitud de aprobacion MFA en tu celular. ¿Qué respuesta es mas segura?",
                "options": [
                    "Rechazar la solicitud y verificar por el canal oficial",
                    "Aprobarla para no interrumpir el trabajo",
                    "Esperar a que desaparezca sin avisar",
                    "Compartir el aviso con personas externas",
                ],
                "correct_option": "A",
            },
            {
                "question_id": "post_passwords_03",
                "form": "B",
                "topic": "passwords",
                "construct": "contrasena_unica_larga",
                "prompt": "Debes crear acceso para una cuenta nueva del negocio. ¿Qué opcion es mejor?",
                "options": [
                    "Usar el nombre del negocio con el año",
                    "Repetir una contraseña que recuerdas bien",
                    "Usar una frase unica, larga y dificil de adivinar",
                    "Usar la misma clave compartida del equipo",
                ],
                "correct_option": "C",
            },
            {
                "question_id": "post_malware_01",
                "form": "B",
                "topic": "malware",
                "construct": "usb_archivo_desconocido",
                "prompt": "Encuentras en una carpeta compartida un instalador con nombre parecido a una herramienta de trabajo, pero nadie aviso su publicacion. ¿Qué haces primero?",
                "options": [
                    "Ejecutarlo porque esta dentro de una carpeta de la empresa",
                    "Verificar su procedencia por un canal autorizado antes de abrirlo",
                    "Copiarlo a tu escritorio para probarlo con calma",
                    "Pedir a otra persona que lo ejecute primero",
                ],
                "correct_option": "B",
            },
            {
                "question_id": "post_malware_02",
                "form": "B",
                "topic": "malware",
                "construct": "ransomware_malware",
                "prompt": "Un programa desconocido bloquea documentos y solicita permisos que no necesita. ¿Qué interpretacion es mas segura?",
                "options": [
                    "Puede ser software malicioso y debe revisarse",
                    "Puede ser una actualizacion si tiene un icono conocido",
                    "Conviene conceder permisos solo una vez para terminar",
                    "Debe instalarse para terminar el trabajo",
                ],
                "correct_option": "A",
            },
            {
                "question_id": "post_malware_03",
                "form": "B",
                "topic": "malware",
                "construct": "respuesta_segura",
                "prompt": "Un equipo empieza a comportarse de forma anomala despues de abrir una descarga. ¿Qué respuesta protege mejor?",
                "options": [
                    "Seguir trabajando para no perder tiempo",
                    "Copiar archivos a otros equipos",
                    "Ocultar la alerta hasta terminar la tarea",
                    "Detener la actividad y reportar para aislar o revisar el equipo",
                ],
                "correct_option": "D",
            },
            {
                "question_id": "post_wifi_01",
                "form": "B",
                "topic": "wifi",
                "construct": "conexion_segura",
                "prompt": "Desde un hotel necesitas entrar a un sistema interno de la empresa. ¿Qué debes priorizar?",
                "options": [
                    "Usar conexion autorizada y protegida antes de acceder",
                    "Entrar desde cualquier red abierta cercana",
                    "Desactivar HTTPS si la pagina tarda",
                    "Compartir datos por un portal desconocido",
                ],
                "correct_option": "A",
            },
            {
                "question_id": "post_wifi_02",
                "form": "B",
                "topic": "wifi",
                "construct": "red_falsa_ssid",
                "prompt": "Aparece un hotspot con nombre parecido al de la empresa, pero nadie lo anuncio. ¿Qué haces?",
                "options": [
                    "Conectarte porque el nombre se parece",
                    "Confirmar si es autorizado antes de usarlo",
                    "Usarlo solo para tareas que parezcan poco sensibles",
                    "Recomendarlo al equipo si la señal es mas fuerte",
                ],
                "correct_option": "B",
            },
            {
                "question_id": "post_wifi_03",
                "form": "B",
                "topic": "wifi",
                "construct": "trafico_exfiltracion",
                "prompt": "Una aplicacion de escritorio comienza a sincronizar muchos archivos fuera del horario normal hacia un servicio cloud no autorizado. ¿Qué decision reduce el riesgo?",
                "options": [
                    "Dejarla terminar porque podria ser una copia automatica",
                    "Bloquear o aislar segun procedimiento y reportar",
                    "Cambiar la contraseña de la red y continuar trabajando",
                    "Cerrar la ventana visible sin revisar la transferencia",
                ],
                "correct_option": "B",
            },
        ],
    },
}


def get_assessment_status(db: Session, user_id: int) -> dict:
    consent_active = pilot_service.has_active_pilot_consent(db, user_id)
    pre = _get_assessment(db, user_id, "PRE")
    post = _get_assessment(db, user_id, "POST")
    intervention_progress = _intervention_progress(db, user_id, pre)
    post_eligible = _is_post_eligible(pre, intervention_progress)

    return {
        "instrument_version": INSTRUMENT_VERSION,
        "consent_active": consent_active,
        "pre": _status_item(pre),
        "post": _status_item(post),
        "next_phase": _next_phase(pre, post, consent_active),
        "post_eligible": post_eligible,
        "intervention_progress": intervention_progress,
    }


def start_assessment(
    db: Session,
    user_id: int,
    request: schemas.PilotAssessmentStartRequest,
) -> dict:
    _require_active_consent(db, user_id)
    phase = request.phase
    form = PHASE_FORMS[phase]

    existing = _get_assessment(db, user_id, phase)

    if existing is not None:
        if existing.status == "started":
            return _start_response(existing)

        raise PilotAssessmentConflictError(
            f"{phase} assessment is already completed."
        )

    if phase == "POST":
        pre = _get_completed_assessment(db, user_id, "PRE")
        intervention_progress = _intervention_progress(db, user_id, pre)

        if not _is_post_eligible(pre, intervention_progress):
            raise PilotAssessmentConflictError(
                "POST assessment requires completed PRE and minimum intervention."
            )

    assessment = models.PilotAssessment(
        id=str(uuid.uuid4()),
        user_id=user_id,
        phase=phase,
        form=form,
        instrument_version=INSTRUMENT_VERSION,
        status="started",
    )
    db.add(assessment)
    _commit_and_refresh(db, assessment)

    return _start_response(assessment)


def record_answer(
    db: Session,
    user_id: int,
    assessment_id: str,
    request: schemas.PilotAssessmentAnswerRequest,
) -> dict:
    _require_active_consent(db, user_id)
    _validate_uuid(assessment_id)
    assessment = _get_user_assessment_by_id(db, user_id, assessment_id)

    if assessment is None:
        raise PilotAssessmentNotFoundError("Pilot assessment not found.")

    if assessment.status != "started":
        raise PilotAssessmentConflictError("Pilot assessment is already completed.")

    question = _get_question_for_assessment(assessment, request.question_id)
    selected_option = _normalize_selected_option(request.selected_option)

    if selected_option not in OPTION_KEYS:
        raise PilotAssessmentValidationError("Invalid selected_option.")

    if _answer_exists(db, assessment.id, question["question_id"]):
        raise PilotAssessmentConflictError("Question already answered.")

    answer = models.PilotAssessmentAnswer(
        assessment_id=assessment.id,
        question_id=question["question_id"],
        topic=question["topic"],
        selected_option=selected_option,
        is_correct=selected_option == question["correct_option"],
        response_time_ms=request.response_time_ms,
    )
    db.add(answer)
    _commit_and_refresh(db, answer)

    return {
        "assessment_id": assessment.id,
        "question_id": question["question_id"],
        "recorded": True,
        "answered_count": _answered_count(db, assessment.id),
        "total_questions": TOTAL_QUESTIONS,
    }


def complete_assessment(db: Session, user_id: int, assessment_id: str) -> dict:
    _require_active_consent(db, user_id)
    _validate_uuid(assessment_id)
    assessment = _get_user_assessment_by_id(db, user_id, assessment_id)

    if assessment is None:
        raise PilotAssessmentNotFoundError("Pilot assessment not found.")

    if assessment.status != "started":
        raise PilotAssessmentConflictError("Pilot assessment is already completed.")

    answers = list(assessment.answers)

    if len(answers) != TOTAL_QUESTIONS:
        raise PilotAssessmentConflictError(
            "Pilot assessment requires 12 answers before completion."
        )

    scores = _calculate_scores(answers)
    assessment.status = "completed"
    assessment.completed_at = datetime.utcnow()
    assessment.total_score = scores["total_score"]
    assessment.phishing_score = scores["topic_scores"]["phishing"]
    assessment.passwords_score = scores["topic_scores"]["passwords"]
    assessment.malware_score = scores["topic_scores"]["malware"]
    assessment.wifi_score = scores["topic_scores"]["wifi"]

    _commit_and_refresh(db, assessment)

    return _assessment_result(assessment)


def get_assessment_results(db: Session, user_id: int) -> dict:
    _require_active_consent(db, user_id)
    pre = _get_completed_assessment(db, user_id, "PRE")
    post = _get_completed_assessment(db, user_id, "POST")

    return {
        "instrument_version": INSTRUMENT_VERSION,
        "pre": _assessment_result(pre) if pre else None,
        "post": _assessment_result(post) if post else None,
        "gain": _gain_result(pre, post) if pre and post else None,
    }


def get_questions_for_form(form: str) -> list:
    return list(QUESTION_BANK[INSTRUMENT_VERSION][form])


def get_all_questions() -> list:
    questions = []

    for form in PHASE_FORMS.values():
        questions.extend(get_questions_for_form(form))

    return questions


def _require_active_consent(db: Session, user_id: int):
    if not pilot_service.has_active_pilot_consent(db, user_id):
        raise PilotAssessmentPermissionError(
            "Active pilot consent is required for pilot assessment."
        )


def _get_assessment(db: Session, user_id: int, phase: str):
    return (
        db.query(models.PilotAssessment)
        .filter(
            models.PilotAssessment.user_id == user_id,
            models.PilotAssessment.phase == phase,
            models.PilotAssessment.instrument_version == INSTRUMENT_VERSION,
        )
        .first()
    )


def _get_completed_assessment(db: Session, user_id: int, phase: str):
    return (
        db.query(models.PilotAssessment)
        .filter(
            models.PilotAssessment.user_id == user_id,
            models.PilotAssessment.phase == phase,
            models.PilotAssessment.instrument_version == INSTRUMENT_VERSION,
            models.PilotAssessment.status == "completed",
        )
        .first()
    )


def _completed_assessment_exists(db: Session, user_id: int, phase: str) -> bool:
    return _get_completed_assessment(db, user_id, phase) is not None


def _get_user_assessment_by_id(db: Session, user_id: int, assessment_id: str):
    return (
        db.query(models.PilotAssessment)
        .filter(
            models.PilotAssessment.id == assessment_id,
            models.PilotAssessment.user_id == user_id,
            models.PilotAssessment.instrument_version == INSTRUMENT_VERSION,
        )
        .first()
    )


def _get_question_for_assessment(assessment, question_id: str) -> dict:
    questions = {
        question["question_id"]: question
        for question in get_questions_for_form(assessment.form)
    }

    if question_id not in questions:
        raise PilotAssessmentValidationError(
            "Question does not belong to assessment form."
        )

    return questions[question_id]


def _answer_exists(db: Session, assessment_id: str, question_id: str) -> bool:
    return (
        db.query(models.PilotAssessmentAnswer)
        .filter(
            models.PilotAssessmentAnswer.assessment_id == assessment_id,
            models.PilotAssessmentAnswer.question_id == question_id,
        )
        .first()
        is not None
    )


def _answered_count(db: Session, assessment_id: str) -> int:
    return (
        db.query(models.PilotAssessmentAnswer)
        .filter(models.PilotAssessmentAnswer.assessment_id == assessment_id)
        .count()
    )


def _answered_question_ids(assessment) -> list[str]:
    answered = {
        answer.question_id
        for answer in assessment.answers
    }

    return [
        question["question_id"]
        for question in get_questions_for_form(assessment.form)
        if question["question_id"] in answered
    ]


def _intervention_progress(db: Session, user_id: int, pre) -> dict:
    progress = {
        "distinct_scenarios_completed": 0,
        "required_distinct_scenarios": REQUIRED_DISTINCT_SCENARIOS_FOR_POST,
        "completed_minigame_sessions": 0,
        "required_minigame_sessions": REQUIRED_MINIGAME_SESSIONS_FOR_POST,
    }

    if pre is None or pre.status != "completed" or pre.completed_at is None:
        return progress

    distinct_scenarios = (
        db.query(func.count(func.distinct(models.Decision.scenario_id)))
        .filter(
            models.Decision.user_id == user_id,
            models.Decision.created_at > pre.completed_at,
        )
        .scalar()
    )
    completed_minigames = (
        db.query(func.count(models.MinigameSessionRecord.id))
        .filter(
            models.MinigameSessionRecord.user_id == user_id,
            models.MinigameSessionRecord.status == "completed",
            models.MinigameSessionRecord.completed_at > pre.completed_at,
        )
        .scalar()
    )

    progress["distinct_scenarios_completed"] = int(distinct_scenarios or 0)
    progress["completed_minigame_sessions"] = int(completed_minigames or 0)
    return progress


def _is_post_eligible(pre, intervention_progress: dict) -> bool:
    if pre is None or pre.status != "completed" or pre.completed_at is None:
        return False

    return (
        intervention_progress["distinct_scenarios_completed"]
        >= intervention_progress["required_distinct_scenarios"]
        and intervention_progress["completed_minigame_sessions"]
        >= intervention_progress["required_minigame_sessions"]
    )


def _normalize_selected_option(value: str) -> str:
    return (value or "").strip().upper()


def _validate_uuid(value: str):
    try:
        uuid.UUID(str(value))
    except ValueError as exc:
        raise PilotAssessmentValidationError(
            "assessment_id must be a valid UUID."
        ) from exc


def _calculate_scores(answers: list) -> dict:
    topic_scores = {}

    for topic in TOPICS:
        topic_answers = [
            answer
            for answer in answers
            if answer.topic == topic
        ]
        correct = sum(1 for answer in topic_answers if answer.is_correct)
        topic_scores[topic] = _score(correct, QUESTIONS_PER_TOPIC)

    correct_total = sum(1 for answer in answers if answer.is_correct)

    return {
        "total_score": _score(correct_total, TOTAL_QUESTIONS),
        "topic_scores": topic_scores,
    }


def _score(correct: int, total: int) -> float:
    return round((correct / total) * 100, 2)


def _start_response(assessment) -> dict:
    return {
        "assessment_id": assessment.id,
        "phase": assessment.phase,
        "instrument_version": assessment.instrument_version,
        "status": assessment.status,
        "answered_question_ids": _answered_question_ids(assessment),
        "questions": [
            _public_question(question)
            for question in get_questions_for_form(assessment.form)
        ],
    }


def _public_question(question: dict) -> dict:
    return {
        "question_id": question["question_id"],
        "prompt": question["prompt"],
        "options": list(question["options"]),
    }


def _assessment_result(assessment) -> dict:
    return {
        "assessment_id": assessment.id,
        "phase": assessment.phase,
        "instrument_version": assessment.instrument_version,
        "status": assessment.status,
        "completed_at": assessment.completed_at,
        "total_score": assessment.total_score,
        "topic_scores": {
            "phishing": assessment.phishing_score,
            "passwords": assessment.passwords_score,
            "malware": assessment.malware_score,
            "wifi": assessment.wifi_score,
        },
    }


def _gain_result(pre, post) -> dict:
    return {
        "total": round(post.total_score - pre.total_score, 2),
        "phishing": round(post.phishing_score - pre.phishing_score, 2),
        "passwords": round(post.passwords_score - pre.passwords_score, 2),
        "malware": round(post.malware_score - pre.malware_score, 2),
        "wifi": round(post.wifi_score - pre.wifi_score, 2),
    }


def _status_item(assessment) -> dict | None:
    if assessment is None:
        return None

    return {
        "assessment_id": assessment.id,
        "phase": assessment.phase,
        "status": assessment.status,
        "started_at": assessment.started_at,
        "completed_at": assessment.completed_at,
        "answered_count": len(assessment.answers),
        "answered_question_ids": _answered_question_ids(assessment),
    }


def _next_phase(pre, post, consent_active: bool) -> str | None:
    if not consent_active:
        return None

    if pre is None or pre.status == "started":
        return "PRE"

    if post is None or post.status == "started":
        return "POST"

    return None


def _commit_and_refresh(db: Session, record):
    try:
        db.commit()
    except IntegrityError:
        db.rollback()
        raise PilotAssessmentConflictError(
            "Pilot assessment could not be persisted."
        )

    db.refresh(record)
