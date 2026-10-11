# Pytest Hang Root Cause - Fase 4

## Causa confirmada

El bloqueo no era causado por Neon, PostgreSQL, fixtures del piloto, FastAPI startup/lifespan ni sesiones de base de datos.

La causa confirmada fue la restriccion de sockets locales dentro del sandbox. `FastAPI TestClient` usa `Starlette TestClient`, que usa AnyIO y crea un event loop en un hilo. En Windows, la inicializacion del loop usa un socket local interno (`socketpair`). Dentro del sandbox ese socket quedaba bloqueado en `accept()`.

## Evidencia

Caso minimo que se bloqueaba dentro del sandbox:

```powershell
python -X faulthandler -c "from fastapi import FastAPI; from fastapi.testclient import TestClient; app=FastAPI(); app.add_api_route('/ping', lambda: {'ok': True}); client=TestClient(app); client.get('/ping')"
```

Traza capturada con `faulthandler.dump_traceback_later(5)`:

```text
Thread run_blocking_portal:
  socket.py line 295 in accept
  socket.py line 646 in socketpair
  asyncio/proactor_events.py line 785 in _make_self_pipe
  asyncio/windows_events.py line 315 in __init__
  anyio/from_thread.py line 538 in run_blocking_portal

Main thread:
  concurrent/futures/_base.py line 451 in result
  anyio/from_thread.py line 554 in start_blocking_portal
  starlette/testclient.py line 435 in _portal_factory
  starlette/testclient.py line 355 in handle_request
  httpx2/_client.py line 1076 in _send_single_request
```

El mismo caso minimo ejecutado fuera del sandbox respondio correctamente:

```text
before client
before request
after request 200 {'ok': True}
```

Esto confirma que el bloqueo estaba en la creacion/uso del socket local requerido por `TestClient`, no en el codigo del piloto.

## Dependencia `httpx2`

`httpx2` es una dependencia real en este proyecto de test:

- `requirements-dev.txt` incluye `httpx2`.
- `starlette.testclient` intenta `import httpx2 as httpx`.
- Python global no tenia `httpx2`, por eso fallaba antes de ejecutar pruebas.
- El venv existente si tenia `httpx2`, por eso llegaba a iniciar tests, pero se quedaba bloqueado por el socket local del sandbox.

No se instalo nada globalmente. Se creo un venv temporal local en `.codex_build/phase4-py312` y se instalaron dependencias con `requirements-dev.txt`.

## Solucion aplicada

1. Crear un entorno temporal Python 3.12 local:

```powershell
python -m venv C:\Users\luzgr\Documents\ProtectPyME\.codex_build\phase4-py312
```

2. Instalar dependencias de desarrollo dentro de ese entorno:

```powershell
C:\Users\luzgr\Documents\ProtectPyME\.codex_build\phase4-py312\Scripts\python.exe -m pip install -r requirements-dev.txt
```

3. Ejecutar las pruebas que usan `TestClient` con permiso ampliado para sockets locales.

4. Mantener `DATABASE_URL` y `TEST_DATABASE_URL` removidas durante la ejecucion.

5. Corregir el aislamiento de `tests/test_survey.py`, que fallaba en suite completa por cache de modulos, sin tocar backend productivo.

## Evidencia de que dejo de colgarse

Resultados finales:

```text
tests/test_pilot_assessment.py: 36 passed in 9.05s
tests/test_minigame_session.py: 81 passed in 6.01s
tests/test_scenario_decisions.py: 14 passed in 1.60s
tests/test_seed_scenarios.py: 1 passed in 0.52s
pytest completo: 442 passed in 27.46s
```

No se dejaron prints, logging temporal ni instrumentacion de debug en codigo productivo.
