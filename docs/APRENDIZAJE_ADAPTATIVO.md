# Aprendizaje adaptativo en ProtectPyME

## 1. Objetivo del modulo

El modulo de aprendizaje adaptativo conecta la recomendacion de riesgo del backend con microlecciones, minijuegos, registro de intentos, dominio por concepto y retroalimentacion personalizada. Su objetivo es que cada usuario practique contenidos de ciberseguridad acordes con su area vulnerable, nivel de riesgo y desempeno reciente.

## 2. Arquitectura general

Flujo principal:

```text
IA recomienda topic/risk
-> sesion adaptativa
-> microleccion
-> minijuego
-> intentos
-> cierre
-> dominio
-> feedback
-> siguiente sesion adaptada
```

Backend FastAPI:

- Define endpoints protegidos por JWT interno.
- Selecciona contenido desde bancos curados.
- Persiste sesiones, intentos y dominio por concepto.
- Genera retroalimentacion determinista sin LLM.

Unity:

- Solicita una sesion pedagogica.
- Guarda el estado en `MinigameLessonState`.
- Muestra la microleccion.
- Ejecuta Quiz, Sopa de letras o Crucigrama con los items de sesion.
- Registra un intento por item.
- Cierra la sesion y solicita feedback sin bloquear la pantalla final.

## 3. Endpoints del modulo

| Endpoint | Metodo | Uso |
| --- | --- | --- |
| `/minigames/lesson` | GET | Obtiene una microleccion por `topic`, `risk` y `minigame`. |
| `/minigames/session` | POST | Crea una sesion adaptativa con items y leccion alineados. |
| `/minigames/attempts` | POST | Registra el intento de un item de sesion. |
| `/minigames/session/{session_id}/complete` | POST | Cierra la sesion, actualiza dominio y devuelve resumen. |
| `/minigames/session/{session_id}/feedback` | GET | Devuelve retroalimentacion personalizada de una sesion completada. |
| `/minigames/mastery` | GET | Consulta dominio pedagogico por concepto. |
| `/minigames/quiz` | GET | Endpoint legacy de quiz. |
| `/minigames/wordsearch` | GET | Endpoint legacy de sopa de letras. |
| `/minigames/crossword` | GET | Endpoint legacy de crucigrama. |

Todos los endpoints del modulo requieren usuario autenticado. Los endpoints legacy conservan su contrato anterior y no participan en el flujo completo de sesiones adaptativas.

## 4. Tablas nuevas

`minigame_session_records`:

- `id`: UUID de sesion.
- `user_id`: propietario autenticado.
- `topic`, `risk`, `minigame`: contexto exacto de seleccion.
- `item_ids`: ids seleccionados por backend.
- `concept_ids`: conceptos evaluados en la sesion.
- `status`: `started` o `completed`.
- `started_at`, `completed_at`: marcas temporales.

`minigame_attempts`:

- `session_id`, `user_id`, `item_id`.
- `concept_ids`: derivados por backend desde el banco.
- `difficulty`: derivada por backend desde el item.
- `correct`, `response_time_ms`, `attempt_number`, `points_delta`.
- Restriccion unica por `session_id`, `item_id` y `attempt_number`.

`user_concept_mastery`:

- `user_id`, `concept_id`, `topic`.
- `alpha`, `beta`, `mastery_score`.
- `attempt_count`, `correct_count`, `incorrect_count`, `evidence_weight`.
- `last_practiced_at`, `created_at`, `updated_at`.
- Restriccion unica por usuario y concepto.

## 5. Contrato de sesion

Solicitud:

```json
{
  "topic": "passwords",
  "risk": "bajo",
  "minigame": "crossword"
}
```

Respuesta:

```json
{
  "session_id": "uuid",
  "topic": "passwords",
  "risk": "bajo",
  "minigame": "crossword",
  "lesson": {},
  "items": []
}
```

El cliente no envia `user_id`, `concept_ids`, `difficulty` ni `mastery`. Esos datos se derivan en backend para evitar manipulacion del aprendizaje.

## 6. Registro de intentos

Unity registra un intento por item usando:

```json
{
  "session_id": "uuid",
  "item_id": "item-id",
  "correct": true,
  "response_time_ms": 1234,
  "attempt_number": 1,
  "points_delta": 10
}
```

El backend valida que la sesion pertenezca al usuario autenticado, que este en estado `started`, que el item pertenezca a la sesion y que coincidan `topic`, `risk`, `minigame`, dificultad y conceptos con el banco. El cliente no puede registrar conceptos arbitrarios.

## 7. Cierre automatico

Al terminar un minijuego con sesion, Unity espera intentos pendientes y luego llama:

```text
POST /minigames/session/{session_id}/complete
```

El backend marca la sesion como `completed`, calcula resumen y actualiza dominio si hay intentos. Un segundo cierre de la misma sesion responde conflicto para evitar actualizar dominio dos veces.

## 8. Formula de dominio Beta-Bernoulli

Cada concepto inicia con:

```text
alpha = 2.0
beta = 2.0
mastery_score = alpha / (alpha + beta) * 100
```

Pesos por dificultad:

| Dificultad | Peso |
| --- | ---: |
| `bajo` | 1.0 |
| `medio` | 1.25 |
| `alto` | 1.5 |

Si el intento es correcto, se suma el peso a `alpha`. Si es incorrecto, se suma a `beta`. El resultado se redondea a dos decimales.

Niveles:

- `sin_datos`: sin intentos.
- `necesita_refuerzo`: menor a 50.
- `en_desarrollo`: 50 a menor de 75.
- `dominado`: 75 o mas.

## 9. Seleccion adaptativa

La seleccion usa solo `topic`, `risk` y `minigame` exactos. Cada sesion selecciona 3 items. La puntuacion de cada candidato combina:

- debilidad del concepto;
- bono por refuerzo de conceptos practicados y debiles;
- bono de exploracion para conceptos no practicados;
- penalizacion por repeticion reciente;
- desempate estable basado en `session_id`, `user_id` e `item_id`.

La seleccion mantiene el `risk` solicitado; no escala ni reduce dificultad automaticamente.

## 10. Antirrepeticion

El backend revisa las ultimas sesiones completadas del mismo usuario, topic, risk y minigame. Los items usados recientemente reciben penalizaciones decrecientes para favorecer variedad sin impedir que un item vuelva a aparecer cuando el banco disponible es pequeno.

## 11. Bancos pedagogicos

El backend tiene 180 items curados:

- 4 topics: `phishing`, `passwords`, `malware`, `wifi`.
- 3 riesgos: `alto`, `medio`, `bajo`.
- 3 minijuegos: `quiz`, `wordsearch`, `crossword`.
- 5 items por combinacion.

Total:

```text
4 topics * 3 riesgos * 3 minijuegos * 5 items = 180 items
```

El catalogo pedagogico actual contiene 41 conceptos.

## 12. Retroalimentacion personalizada

Despues del cierre exitoso, Unity consulta:

```text
GET /minigames/session/{session_id}/feedback
```

El feedback se genera solo para sesiones completadas y propiedad del usuario autenticado. No expone respuestas correctas, opciones ni soluciones completas. Incluye:

- resumen de precision;
- nivel de desempeno;
- fortalezas;
- conceptos a reforzar;
- siguiente paso;
- minijuego recomendado por rotacion.

Niveles de desempeno:

- `sin_evidencia`
- `excelente`
- `buen_progreso`
- `en_desarrollo`
- `necesita_refuerzo`

Estados de concepto:

- `fortaleza`
- `avance`
- `refuerzo`
- `dificultad_puntual`

## 13. Integracion Unity

Modelos Unity:

- `MinigameSessionModels.cs`
- `MinigameAttemptModels.cs`
- `MinigameSessionSummaryModels.cs`
- `MinigameFeedbackModels.cs`
- `MinigameLessonState.cs`

Controladores:

- `QuizController.cs`
- `PreguntasController.cs`
- `CrosswordController.cs`
- `MinigameLessonController.cs`
- `MinigameFeedbackPresenter.cs`

Unity conserva:

- `MinigameLessonState.Session`: sesion activa.
- `MinigameLessonState.LastSummary`: resumen tras `/complete`.
- `MinigameLessonState.LastFeedback`: retroalimentacion tras `/feedback`.

La interfaz de feedback es modal, bloquea raycasts mientras se lee, tiene boton X y boton CERRAR, y destruye solo el modal al cerrarse. Los botones originales de la pantalla final conservan sus listeners.

## 14. Seguridad y privacidad

- Los endpoints adaptativos usan JWT interno.
- El backend obtiene `user_id` del usuario autenticado.
- Las sesiones de otros usuarios responden 404.
- El cliente no envia dominio, conceptos ni usuario.
- No se imprimen JWT ni secretos.
- El feedback no expone respuestas correctas.
- Los logs de contenido detallado del crucigrama y entradas de usuario quedan limitados a `UNITY_EDITOR`.

## 15. Pruebas

Backend:

```text
venv/Scripts/python.exe -m pytest -q
292 passed
```

Cobertura de comportamiento validada por tests:

- creacion de sesiones;
- registro unico de intentos;
- cierre de sesiones;
- dominio por concepto;
- seleccion adaptativa;
- bancos ampliados;
- retroalimentacion personalizada;
- encuesta y AI hibrida.

Pruebas visuales Unity pendientes para entrega:

- Quiz: 3 items, aciertos/errores, cierre, feedback, X, CERRAR, botones finales.
- Sopa de letras: 3 terminos, timeout, feedback y botones posteriores.
- Crucigrama: 3 palabras, terminos largos como ARGON2ID, victoria, derrota, scroll y botones.
- Legacy: abrir Kahoot, SopaLetras y Crucigrama directamente sin intentos ni feedback personalizado.
- Android real: revisar legibilidad, bloqueo modal, cierre durante espera y orientacion horizontal.

## 16. Limitaciones

- `mastery` no usa todavia tiempo de respuesta.
- La seleccion adaptativa mantiene `risk` exacto y no cambia dificultad automaticamente.
- El feedback es determinista y no usa LLM.
- El historial previo a 4B.2 no se recalculo automaticamente.
- Los endpoints legacy no usan el aprendizaje adaptativo completo.
- Quedan warnings de Pydantic v2 por `class Config`.
- Quedan warnings por `datetime.utcnow`.
- Crucigrama legacy puede depender de `APIManager` al abrirse directamente.
- Las pruebas visuales finales deben repetirse en Android real.

## 17. Trabajo futuro

- Incorporar tiempo de respuesta como evidencia secundaria de dominio.
- Permitir ajuste pedagogico gradual de dificultad cuando exista historial suficiente.
- Agregar pantalla historica de dominio por concepto en Unity.
- Mostrar recomendaciones acumuladas por topic en Perfil.
- Migrar Pydantic `class Config` a `ConfigDict`.
- Reemplazar `datetime.utcnow` por fechas aware en UTC.
- Ampliar pruebas automatizadas de contrato Unity con mocks de API.
