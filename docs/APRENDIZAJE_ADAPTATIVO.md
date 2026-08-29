# Aprendizaje adaptativo en ProtectPyME

Release documentado: `v0.2.1-adaptive-learning`

## 1. Objetivo del modulo

El modulo de aprendizaje adaptativo conecta decisiones de escenarios, analitica conductual, clasificacion de riesgo, recomendaciones, microlecciones, minijuegos adaptativos, dominio por concepto y retroalimentacion personalizada.

Su objetivo es que cada usuario practique contenidos de ciberseguridad acordes con su area vulnerable, nivel de riesgo y evidencia pedagogica reciente. El sistema no usa IA generativa; el contenido y la retroalimentacion son deterministas y estan basados en bancos curados.

## 2. Arquitectura real

El sistema tiene dos capas complementarias.

### Capa A: adaptacion conductual

```text
Unity scenarios
-> decision
-> FastAPI
-> PostgreSQL
-> analytics
-> most_failed_category
-> Random Forest risk classification
-> recommendation engine
-> recommended_training
-> recommended_scenario
-> Unity
```

Esta capa observa el desempeno del usuario en escenarios de ciberseguridad. FastAPI persiste las decisiones en PostgreSQL, calcula analitica de comportamiento y entrega features al Random Forest para clasificar riesgo.

Importante: el modelo clasifica; el motor recomienda.

El `RandomForestClassifier` produce `risk_level` y probabilidad. El motor de recomendaciones usa el resultado, la categoria vulnerable y el historial para elegir `recommended_training` y `recommended_scenario`. El recomendador no es otro modelo ML.

### Capa B: adaptacion pedagogica

```text
training topic
-> microlearning content
-> adaptive minigame
-> concept_ids
-> attempts
-> Beta-Bernoulli mastery
-> strengths / reinforcement areas
-> personalized feedback
```

Esta capa empieza cuando el usuario practica un tema. El backend crea una sesion de minijuego con leccion e items alineados a `topic`, `risk` y `minigame`. Cada intento se registra con `concept_ids` derivados por backend. Al cerrar la sesion, se actualiza dominio por concepto y se genera retroalimentacion personalizada.

## 3. Backend y endpoints

Todos los endpoints adaptativos usan el JWT interno de la aplicacion.

| Endpoint | Metodo | Uso |
| --- | --- | --- |
| `/ai/risk/me` | GET | Clasifica riesgo conductual y devuelve recomendacion. |
| `/minigames/lesson` | GET | Obtiene microleccion por `topic`, `risk` y `minigame`. |
| `/minigames/session` | POST | Crea sesion adaptativa con leccion e items seleccionados. |
| `/minigames/attempts` | POST | Registra intento de un item de sesion. |
| `/minigames/session/{session_id}/complete` | POST | Cierra la sesion, resume resultados y actualiza mastery. |
| `/minigames/session/{session_id}/feedback` | GET | Devuelve retroalimentacion personalizada de una sesion completada. |
| `/minigames/mastery` | GET | Consulta dominio pedagogico por concepto. |
| `/minigames/quiz` | GET | Endpoint legacy de quiz. |
| `/minigames/wordsearch` | GET | Endpoint legacy de sopa de letras. |
| `/minigames/crossword` | GET | Endpoint legacy de crucigrama. |

Los endpoints legacy conservan su contrato anterior. Permiten abrir minijuegos de forma directa, pero no participan en el flujo completo de sesiones adaptativas, intentos, mastery y feedback.

## 4. Random Forest

El modelo historico es un `RandomForestClassifier` entrenado con datos sinteticos/controlados. Si se menciona una metrica historica de accuracy, debe entenderse como resultado sobre ese dataset sintetico/controlado, no como validacion de eficacia educativa real con poblacion real.

Features reales del modelo:

```text
total_points
correct_decisions
total_decisions
accuracy
risk_score
awareness_score
decisions_last_7_days
failed_category_encoded
```

El entrenamiento selecciono esas columnas como `DataFrame`. En `v0.2.1-adaptive-learning`, la inferencia tambien usa `pandas.DataFrame` con `model.feature_names_in_`. Esto elimina el warning:

```text
X does not have valid feature names, but RandomForestClassifier was fitted with feature names
```

La correccion no reentrena el modelo, no modifica `model.pkl`, no modifica `encoder.pkl` y no altera los resultados predictivos observados en pruebas de regresion.

## 5. Deuda conocida del RF para malware

El encoder/modelo historico reconoce estas categorias:

```text
phishing
password
wifi
social_engineering
```

La aplicacion actual usa esta taxonomia canonica:

```text
phishing
passwords
malware
wifi
```

Adapter actual hacia el RF:

| Topic canonico | Categoria RF |
| --- | --- |
| `phishing` | `phishing` |
| `passwords` | `password` |
| `wifi` | `wifi` |
| `malware` | fallback controlado a `phishing` |

Para `malware`, el fallback queda documentado con la razon:

```text
malware_without_rf_category
```

Esta limitacion afecta solo a la feature categorica del Random Forest historico. Malware si tiene contenido educativo, minijuegos, concept mastery, feedback personalizado y puede recomendar el escenario backend 3. La solucion futura correcta es reentrenar y versionar el modelo con un espacio categorico revisado.

## 6. Taxonomia

Categorias canonicas:

```text
phishing
passwords
malware
wifi
```

Aliases historicos normalizados:

| Alias | Canonico |
| --- | --- |
| `password` | `passwords` |
| `network` | `wifi` |
| `social_engineering` | `phishing` |
| `malicious_software` | `malware` |

Valores `general`, `unknown` y `none` no representan una categoria concreta. La analitica normaliza aliases historicos en lectura sin reescribir registros existentes.

## 7. Escenarios y mapping Unity

Los IDs del backend no se renumeran. Unity usa un mapping explicito para abrir escenas.

| Backend ID | Escena Unity |
| ---: | --- |
| 1 | `Escenario` |
| 2 | `Escenario2_Acceso` |
| 3 | `Escenario 3 (USB sospechoso)` |
| 5 | `Escenario 4` |
| 6 | `Escenario 5` |
| 7 | `Escenario 6` |

El backend ID 4 es historico/no recomendado actualmente y no debe abrir una escena jugable desde el flujo adaptativo.

Candidatos por categoria:

| Categoria | Candidatos |
| --- | --- |
| `phishing` | `[1, 5]` |
| `passwords` | `[2, 6]` |
| `malware` | `[3]` |
| `wifi` | `[7]` |

El selector evita repeticion inmediata cuando existe alternativa, prioriza el escenario menos practicado y usa desempate determinista por ID.

## 8. Cobertura pedagogica

Conceptos nuevos documentados para escenarios intermedios:

| Concept ID | Topic | Riesgo | Uso pedagogico |
| --- | --- | --- | --- |
| `passwords.credential_request` | `passwords` | `medio` | Solicitudes sospechosas de credenciales. |
| `passwords.identity_verification` | `passwords` | `medio` | Verificacion de identidad por canal oficial. |
| `wifi.suspicious_traffic` | `wifi` | `medio` | Trafico saliente anomalo y conexiones no reconocidas. |
| `wifi.data_exfiltration` | `wifi` | `medio` | Exfiltracion o fuga de datos. |

Cobertura por escenario nuevo:

| Backend ID | Situacion | Topic | Conceptos principales |
| ---: | --- | --- | --- |
| 5 | Portal falso de proveedores | `phishing` | dominio, URL, proveedor falso. |
| 6 | Llamada fraudulenta solicitando contrasena | `passwords` | `credential_request`, `identity_verification`. |
| 7 | Trafico sospechoso / exfiltracion | `wifi` | `suspicious_traffic`, `data_exfiltration`. |

Las microlecciones y bancos de Quiz, Wordsearch y Crossword cubren estos conceptos. En Wordsearch, la respuesta visible `FUGADATOS` representa pedagogicamente `wifi.data_exfiltration` por restriccion de longitud de la cuadricula. El crucigrama puede conservar respuestas mas largas cuando su contrato lo permite.

## 9. Bancos pedagogicos y minijuegos

El backend mantiene bancos curados para:

```text
Quiz
Wordsearch
Crossword
```

Cobertura:

```text
4 topics * 3 riesgos * 3 minijuegos * 5 items = 180 items
```

Los bancos contienen `item_id`, respuesta esperada, dificultad y `concept_ids`. Unity no decide los conceptos evaluados; los recibe en la sesion y registra intentos contra items del backend.

## 10. Contrato de sesion

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

## 11. Registro de intentos

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

## 12. Mastery Beta-Bernoulli

Un error en un escenario no actualiza directamente mastery.

Flujo correcto:

```text
scenario decision
-> behavioral analytics
-> recommended training
-> minigame session
-> item attempt
-> concept_ids
-> alpha/beta
-> mastery_score
```

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

| Nivel | Criterio |
| --- | --- |
| `sin_datos` | Sin intentos. |
| `necesita_refuerzo` | Menor a 50. |
| `en_desarrollo` | 50 a menor de 75. |
| `dominado` | 75 o mas. |

Esta capa distingue evidencia conductual de evidencia de dominio conceptual. Las decisiones de escenarios orientan la recomendacion; los intentos de minijuegos actualizan mastery.

## 13. Retroalimentacion personalizada

La retroalimentacion posterior usa evidencia de:

- attempts;
- concept_ids;
- accuracy;
- mastery.

Con esa evidencia identifica fortalezas, avances y areas de refuerzo. No usa LLM ni IA generativa y no expone respuestas correctas, opciones ni soluciones completas.

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

## 14. Integracion Unity

Modelos Unity principales:

- `MinigameSessionModels.cs`
- `MinigameAttemptModels.cs`
- `MinigameSessionSummaryModels.cs`
- `MinigameFeedbackModels.cs`
- `MinigameLessonState.cs`

Controladores principales:

- `AIRecommendationController.cs`
- `ProgresoController.cs`
- `QuizController.cs`
- `PreguntasController.cs`
- `CrosswordController.cs`
- `MinigameLessonController.cs`
- `MinigameFeedbackPresenter.cs`

Unity conserva:

- `MinigameLessonState.Session`: sesion activa.
- `MinigameLessonState.LastSummary`: resumen tras `/complete`.
- `MinigameLessonState.LastFeedback`: retroalimentacion tras `/feedback`.

El flujo validado de produccion es:

```text
Unity
-> Render
-> Neon PostgreSQL
-> analytics
-> IA / recommendation
-> Unity
```

## 15. Produccion

Backend: Render.

Base de datos: Neon PostgreSQL.

Cliente: Unity Android.

La validacion end-to-end confirmo el ciclo:

```text
decision de escenario
-> persistencia en Neon
-> recalculo de analitica
-> recomendacion adaptativa
-> apertura de practica Unity
-> minijuego / aprendizaje
```

## 16. QA y testing

Baseline final de backend para `v0.2.1-adaptive-learning`:

```text
355 passed
0 failed
```

Categorias principales de tests:

- adaptive selection;
- AI hybrid;
- concept catalog;
- concept mastery;
- expanded minigame banks;
- lesson coverage;
- minigame lessons;
- minigame sessions;
- personalized feedback;
- scenario decisions;
- seed scenarios;
- survey;
- RF feature-name regression.

Tambien se valido manualmente el flujo Unity -> Render -> Neon -> analytics -> IA/recommendation -> Unity para escenarios nuevos y recomendacion multi-escenario. No se afirma cobertura total ni 100% porque no existe una metrica formal de cobertura end-to-end visual.

## 17. Releases

`v0.2.0-adaptive-scenarios`:

- escenarios intermedios;
- taxonomia canonica;
- flujo de recomendacion multi-escenario;
- mapping explicito backend -> Unity.

`v0.2.1-adaptive-learning`:

- cobertura pedagogica especifica para escenarios intermedios;
- concept mastery ampliado;
- feedback personalizado;
- inferencia RF segura con feature names;
- QA final backend con 355 tests passing.

## 18. Limitaciones conocidas

Limitaciones actuales:

1. El Random Forest historico no tiene categoria propia para `malware`; usa fallback controlado `malware_without_rf_category` solo para esa feature categorica.
2. El dataset de entrenamiento actual es sintetico/controlado.
3. No existe validacion educativa longitudinal con poblacion real.
4. Persisten warnings de Pydantic v2 por `class Config`.
5. Persisten warnings por `datetime.utcnow()`.
6. Persiste un warning de pytest/SQLAlchemy por coleccion de una clase `Base`.
7. Backend ID 4 es historico/no recomendado actualmente.
8. No existe IA generativa.

Trabajo futuro:

- Reentrenar y versionar el RF con espacio categorico revisado, incluyendo `malware`.
- Incorporar tiempo de respuesta como evidencia secundaria de dominio.
- Permitir ajuste pedagogico gradual de dificultad cuando exista historial suficiente.
- Agregar pantalla historica de dominio por concepto en Unity.
- Migrar Pydantic `class Config` a `ConfigDict`.
- Reemplazar `datetime.utcnow()` por fechas aware en UTC.
- Ampliar pruebas automatizadas de contrato Unity con mocks de API.
