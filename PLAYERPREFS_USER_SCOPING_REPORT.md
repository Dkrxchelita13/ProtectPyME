# PlayerPrefs User Scoping Report - Fase 6

## Alcance

Fase 6 aisla la cache local de progresion por usuario autenticado. El backend
sigue siendo la fuente de verdad; PlayerPrefs queda solo como cache local
scoped por `user_id`.

## Claves globales encontradas

- `NivelAlcanzado`: usada como progreso narrativo del mundo inicial.
- `ProgresoIntermedio`: usada como progreso narrativo del mundo intermedio.
- `MinigamesUnlocked`: usada como bandera local de desbloqueo de minijuegos.
- `UsuarioActual`: usada por sistemas legacy de perfil/avatar/vidas/seguridad.
- `ObtenerClaveUsuario(...)`: helper legacy basado en `UsuarioActual`, no apto
  como identidad estable de progresion.
- `user_id`: presente en modelos de API; ahora se usa como identidad estable de
  progresion local.
- `currentUser`: no se encontro como identificador de progresion en
  `Assets/Scripts`.

## Archivos que usaban claves globales de progresion

- `ProtectPYME/Assets/Scripts/API/APIManager.cs`: escribia
  `NivelAlcanzado`, `ProgresoIntermedio` y `MinigamesUnlocked` al sincronizar
  `/progress/me`.
- `ProtectPYME/Assets/Scripts/BotonesProgresoController.cs`: leia
  `NivelAlcanzado` y `ProgresoIntermedio`; el menu de contexto borraba ambas.
- `ProtectPYME/Assets/Scripts/Escenario 1 Manager.cs`: escribia una clave
  configurable, por defecto `NivelAlcanzado`.
- `ProtectPYME/Assets/Scripts/Escenario2Manager.cs`: escribia
  `NivelAlcanzado`.
- `ProtectPYME/Assets/Scripts/Escenario3Manager.cs`: escribia
  `NivelAlcanzado`.
- `ProtectPYME/Assets/Scripts/Escenario5Manager.cs`: escribia
  `ProgresoIntermedio`.
- `ProtectPYME/Assets/Scripts/Escenario6Manager.cs`: escribia
  `ProgresoIntermedio`.
- `ProtectPYME/Assets/Scripts/RegisterManager.cs`: creaba/resetaba progreso
  global en registro.

## Cambios aplicados

- Se agrego `UserProgressCache` como helper unico para claves de progresion.
- `APIManager` sincroniza el usuario activo desde el JWT y desde
  `/progress/me`, y aplica el progreso remoto con autoridad total.
- `BotonesProgresoController` lee solo desde `UserProgressCache`.
- Los managers de escenarios dejaron de escribir progreso en PlayerPrefs; la
  actualizacion local ocurre despues del refresh de `/progress/me`.
- `RegisterManager` ya no crea progreso global; registro no autentica, asi que
  la cache scoped se crea hasta el primer login exitoso.

## Formato final de claves

- `user_<user_id>_NivelAlcanzado`
- `user_<user_id>_ProgresoIntermedio`
- `user_<user_id>_MinigamesUnlocked`
- Ejemplo: `user_8_NivelAlcanzado`.

## Identificador estable

La identidad de progresion es el `user_id` numerico del backend. El cliente lo
obtiene de:

- `sub` del JWT al llamar `SetToken(...)`.
- `user_id` incluido en la respuesta de `/progress/me`.

No se usa email, nombre, `UsuarioActual` ni `ObtenerClaveUsuario(...)` como
identidad de progresion.

## Login

El flujo queda:

1. Login obtiene token.
2. `APIManager.SetToken(...)` extrae `sub` del JWT y activa el usuario local.
3. `LoginManager.ContinuarDespuesDeAutenticacion()` llama
   `RefreshProgressFromBackend()`.
4. `/progress/me` aplica la cache scoped del usuario activo.
5. La UI de progreso se actualiza desde las claves scoped.

Si el backend devuelve un progreso menor que una cache local corrupta, gana el
backend. No se usa `max(local, remote)`.

## Logout

Se agrego `APIManager.ClearAuthenticationState()` para borrar token e identidad
activa de progresion. `UserProgressCache.ClearActiveUserState()` borra solo la
identidad activa (`ProgressCacheActiveUserId`) y conserva las caches scoped de
cada usuario para uso posterior del mismo usuario.

No se borra `user_<id>_*` en logout normal.

## Cambio A -> B -> A

- Usuario A sincroniza y escribe `user_A_*`.
- Usuario B sincroniza y escribe `user_B_*`.
- Al volver a A se reactiva A y se leen solo `user_A_*`.
- B nunca hereda el progreso de A, y A conserva su propia cache.

## Backend offline/failure

Cuando `/progress/me` falla, el cliente no lee claves globales legacy. Si hay
usuario activo, la UI puede leer solo la cache scoped de ese mismo usuario. Si
no existe cache para ese usuario, los defaults son seguros: etapa inicial `1`,
etapa intermedia `1` y minijuegos bloqueados salvo que el estado scoped diga lo
contrario.

## Tratamiento de claves legacy

Las claves globales `NivelAlcanzado`, `ProgresoIntermedio` y
`MinigamesUnlocked` se ignoran para progresion. No se migran al backend y no se
copian a claves scoped. Tampoco se borran de forma masiva, para evitar efectos
secundarios fuera de Fase 6.

## Validacion

- Unity EditMode: 27 tests passed, 0 failed.
- Nuevos tests de Fase 6: 10 passed en `UserProgressCacheTests`.
- Pytest backend completo: 459 passed.
- Unity batchmode compile: return code 0.

## Tests nuevos

- `UserBCacheDoesNotReadUserAProgress`
- `SwitchingAtoBtoAShowsEachUsersBackendProgressInUi`
- `BackendSuccessCanLowerCorruptLocalCache`
- `BackendFailureUsesOnlySameUserCache`
- `BackendFailureForUserWithoutCacheDoesNotInheritOtherUser`
- `NewUserBackendDefaultsCreateOnlyThatUsersCache`
- `DecisionRefreshUpdatesOnlyAuthenticatedUsersCache`
- `LogoutClearsActiveIdentityButKeepsUserCache`
- `LegacyGlobalProgressKeysAreIgnoredForNewUser`
- `JwtSubSelectsStableUserId`

## Advertencias existentes

La compilacion batchmode no reporto errores `CS`. El log contiene advertencias
informativas de ILPP/PostProcessingAssemblyLoadContext. Git tambien advierte que
algunos archivos pasaran de LF a CRLF cuando Git los toque, consistente con la
configuracion local.

## Riesgos restantes

- Existe `UsuarioActual` y `ObtenerClaveUsuario(...)` para sistemas legacy no
  relacionados con progresion, como seguridad, vidas, avatar y puntuacion. No se
  refactorizaron en esta fase.
- Si una decision se envia correctamente pero el refresh posterior de
  `/progress/me` falla, la cache local no se adelanta optimistamente. Esto evita
  sobreescribir al backend y conserva la autoridad remota.
- `ClearAuthenticationState()` queda disponible para logout; cualquier boton o
  flujo futuro de cierre de sesion debe llamarlo para limpiar token e identidad
  activa.

## Fuera de alcance

No se avanzo a Fase 7. Esta fase se limita a aislar PlayerPrefs de progresion
por usuario y a validar el comportamiento local/backend.
