# ProtectPyME

ProtectPyME es una plataforma de concientizacion en ciberseguridad para PYMES. El cliente principal es Unity Android y el backend esta construido con FastAPI, PostgreSQL y SQLAlchemy.

Release documentado: `v0.2.1-adaptive-learning`.

## Arquitectura

```text
Unity
-> FastAPI en Render
-> Neon PostgreSQL
-> analytics
-> Random Forest risk classification
-> recommendation engine
-> Unity
```

La aplicacion mantiene dos capas adaptativas:

- Adaptacion conductual: decisiones de escenarios, analitica, Random Forest y recomendacion de entrenamiento/escenario.
- Adaptacion pedagogica: microlecciones, minijuegos, intentos, `concept_ids`, dominio Beta-Bernoulli y feedback personalizado.

El modelo clasifica; el motor recomienda. El recomendador no es otro modelo ML.

## Modulos principales

- Backend FastAPI: `backend/`
- Proyecto Unity principal: `../ProtectPYME/`
- Documentacion tecnica: `../docs/APRENDIZAJE_ADAPTATIVO.md`

## Backend

El backend expone endpoints protegidos con JWT interno para usuarios, escenarios, decisiones, encuesta, IA/recomendaciones y minijuegos adaptativos.

Endpoints clave del aprendizaje adaptativo:

- `GET /ai/risk/me`
- `GET /minigames/lesson`
- `POST /minigames/session`
- `POST /minigames/attempts`
- `POST /minigames/session/{session_id}/complete`
- `GET /minigames/session/{session_id}/feedback`
- `GET /minigames/mastery`

## Random Forest

El modelo actual es un `RandomForestClassifier` entrenado sobre datos sinteticos/controlados. Sus features reales son:

- `total_points`
- `correct_decisions`
- `total_decisions`
- `accuracy`
- `risk_score`
- `awareness_score`
- `decisions_last_7_days`
- `failed_category_encoded`

En `v0.2.1`, la inferencia usa `pandas.DataFrame` con `model.feature_names_in_`, eliminando el warning de Scikit-learn por feature names sin reentrenar ni modificar resultados predictivos.

## QA

Baseline backend final:

```text
355 passed
0 failed
```

Categorias cubiertas:

- adaptive selection
- AI hybrid
- concept catalog
- concept mastery
- minigame banks
- lesson coverage
- minigame lessons
- minigame sessions
- personalized feedback
- scenario decisions
- seed scenarios
- survey
- RF feature-name regression

## Documentacion completa

El cierre tecnico y documental del aprendizaje adaptativo esta en:

```text
../docs/APRENDIZAJE_ADAPTATIVO.md
```

Ese documento registra arquitectura, taxonomia, escenarios, cobertura pedagogica, mastery, feedback, produccion, limitaciones y releases `v0.2.0` / `v0.2.1`.
