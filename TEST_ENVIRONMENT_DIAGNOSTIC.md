# Test Environment Diagnostic - Fase 4

Fecha: 2026-10-11

Rama verificada: `fix/pilot-progression-reliability`

Commits base confirmados:

| Commit | Mensaje |
| --- | --- |
| `07f3d0f` | `fix: enforce pilot pre-intervention flow` |
| `8c557b0` | `fix: decouple scenario completion from answer correctness` |
| `d7c84bb` | `fix: prevent minigame unlock state overwrite` |

## Archivos de dependencias

Archivos encontrados en `protectpyme-app/backend`:

| Archivo | Estado |
| --- | --- |
| `requirements.txt` | Existe. Define dependencias runtime. |
| `requirements-dev.txt` | Existe. Incluye `pytest` y `httpx2`. |
| `pyproject.toml` | No existe. |
| `poetry.lock` | No existe. |
| `Pipfile` | No existe. |
| `Pipfile.lock` | No existe. |
| `tests/conftest.py` | No existe. |

`httpx2` no es un typo en esta instalacion: `requirements-dev.txt` lo declara y `starlette.testclient` intenta importar `httpx2` antes de `httpx`.

## Entornos inspeccionados

| Entorno | Python | pip | pytest | FastAPI | Starlette | SQLAlchemy | httpx | httpx2 | Pydantic | Resultado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Python global | 3.12.0 | 23.2.1 | 9.1.1 | 0.139.0 | 1.3.1 | 2.0.51 | MISSING | MISSING | 2.13.4 | No puede importar `fastapi.testclient` por falta de `httpx2`. |
| `protectpyme-app/backend/venv` | 3.14.2 | 25.3 | 9.1.1 | 0.136.3 | 1.3.1 | 2.0.50 | MISSING | 2.9.1 | 2.13.4 | `TestClient` se queda bloqueado dentro del sandbox. Ademas emite warning de Python 3.14: no encuentra ubicacion real de `pythoncore-3.14-64`. |
| `.codex_build/phase4-py312` | 3.12.0 | 23.2.1 | 9.1.1 | 0.143.0 | 1.7.0 | 2.1.4 | MISSING | 2.13.1 | 2.14.1 | Entorno temporal local usado para ejecutar la suite. |

El entorno temporal se creo dentro del repositorio en `.codex_build/phase4-py312` y no se versiono. Se instalo con `requirements-dev.txt`, por lo que no hubo instalacion global.

## Variables y secretos

Durante las pruebas se removieron explicitamente:

- `DATABASE_URL`
- `TEST_DATABASE_URL`

Se definieron solo valores de prueba:

- `SECRET_KEY=test-secret`
- `GOOGLE_CLIENT_IDS=test-client.apps.googleusercontent.com`

El archivo `.env` existe y contiene variables sensibles, pero no se imprimieron ni se usaron para las pruebas. No se expuso `DATABASE_URL`, JWT secret, tokens ni credenciales.

## Base de datos usada en tests

Las pruebas del piloto usan SQLite in-memory con `StaticPool` e instalan un modulo falso `app.database` antes de importar modelos, rutas y servicios. Ejemplo verificado en:

- `tests/test_pilot_assessment.py`
- `tests/test_pilot_readiness.py`
- `tests/test_pilot_export.py`

El patron usado es:

- `create_engine("sqlite://", connect_args={"check_same_thread": False}, poolclass=StaticPool)`
- `sys.modules["app.database"] = fake_database`
- `fake_database.get_db = get_test_db`

La suite relacionada no apunto a Neon ni a una base PostgreSQL externa.

## Diagnostico de ejecucion

Hallazgos:

1. Python global fallaba antes de ejecutar tests con `RuntimeError` de `starlette.testclient` porque faltaba `httpx2`.
2. El venv existente tenia `httpx2`, pero `TestClient` se bloqueaba al ejecutar una peticion.
3. El mismo bloqueo ocurria en un venv temporal Python 3.12 dentro del sandbox.
4. El bloqueo desaparecia al ejecutar fuera del sandbox, confirmando que el cuello de botella era el socket local del event loop de `TestClient`, no la logica del backend ni la base de datos.
5. La suite completa inicialmente mostro un problema real de fixture en `tests/test_survey.py`: el modulo podia reutilizar `app.models` cacheado por otros tests, dejando su SQLite sin la tabla `users`.
6. Se corrigio `tests/test_survey.py` para instalar su `app.database` falso con modulos frescos y restaurar `sys.modules` inmediatamente, evitando contaminar otros tests.

## Fase 3 y backend

`git show --name-only --oneline 07f3d0f` confirma que Fase 3 no modifico backend. Los archivos modificados por ese commit fueron scripts Unity:

- `ProtectPYME/Assets/Scripts/API/Models/AIRecommendationController.cs`
- `ProtectPYME/Assets/Scripts/CambioEscena.cs`
- `ProtectPYME/Assets/Scripts/MinigameLessonController.cs`
- `ProtectPYME/Assets/Scripts/MinijuegosMenuUI.cs`
- `ProtectPYME/Assets/Scripts/Pilot/PilotController.cs`
- `ProtectPYME/Assets/Scripts/Pilot/PilotInterventionGate.cs`
- `ProtectPYME/Assets/Scripts/Pilot/PilotInterventionGate.cs.meta`
- `ProtectPYME/Assets/Scripts/ProgresoController.cs`

No se modificaron:

- `app/services/pilot_assessment_service.py`
- `app/routes/pilot.py`
- modelos backend del assessment piloto

## Conclusion

El entorno backend quedo identificado y la suite puede ejecutarse sin produccion usando SQLite in-memory y permisos de socket local para `TestClient`. El unico cambio de test necesario fue aislar `tests/test_survey.py` para que no dependiera del orden de importacion de otros modulos.
