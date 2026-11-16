# Informe de Revisión Técnica del Backend (.NET 8 + EF Core + PostgreSQL)

**Estado de compilación y pruebas al momento de la revisión:**
- `dotnet build`: OK (0 advertencias, 0 errores).
- `dotnet test`: OK (58 tests pasados, 0 fallados, 0 omitidos en 7 clases de prueba).

---

## 1. Tabla Consolidada de Hallazgos (Ordenada por Severidad)

| # | Severidad | Área | Archivo y Línea | Problema | Arreglo Sugerido |
| :---: | :---: | :--- | :--- | :--- | :--- |
| 1 | **CRÍTICO** | Lógica / Auth | `src/GestionReservas.Application/Features/Auth/Services/AuthService.cs:180-209` | `GetCurrentUserProfileAsync` (`GET /api/auth/me`) ignora `usuario.DepartamentoId` y asigna el departamento buscando por texto `"Sistemas"` o `"Secretaría Académica"` según el rol/email. Un responsable de otro departamento recibe "Sistemas" y los solicitantes reciben `null`. | Agregar `.Include(u => u.Departamento)` en la consulta de la línea 155 y mapear directamente `usuario.Departamento` a `DepartamentoSimpleDto`. |
| 2 | **CRÍTICO** | Tests / Tiempo | `tests/GestionReservas.Tests/EspaciosEndpointsTests.cs:390-410` | `Disponibles_ConCapacidadYEquipamiento_FiltraAdecuadamente` usa `DateTime.UtcNow.AddDays(7)` de 18:00 a 21:00 esperando que `AULA-101` esté disponible. Si el test se corre un martes (hasta el 30/11/2026), colisiona con la cursada oficial `A1011` sembrada en `AULA-101` (Martes 18-21 hs) y falla. | Fijar una fecha determinista que no sea martes (ej. miércoles) o calcular el próximo miércoles explícitamente. |
| 3 | **IMPORTANTE** | Seguridad / CORS | `src/GestionReservas.Api/Program.cs:20-103` | No hay política de CORS configurada (`AddCors` / `UseCors`). Cualquier cliente frontend SPA en otro puerto/origen será bloqueado por el navegador. | Registrar `builder.Services.AddCors(...)` con orígenes permitidos desde configuración y agregar `app.UseCors(...)` antes de `UseAuthentication()`. |
| 4 | **IMPORTANTE** | Concurrencia | `src/GestionReservas.Application/Features/Reservas/Services/ReservasService.cs:80-84`, `555-670` y `EspaciosService.cs:406-459` | `CreateReservaAsync` solo toma `pg_advisory_xact_lock` si `autoAprobar == true`. Una solicitud `PendienteAprobacion` concurrente con `POST /api/bloqueos` (`cancelarReservasAfectadas = true`) o con `DELETE /api/espacios/{id}` puede insertarse sin ser detectada ni cancelada. `UpdateReservaAsync` tampoco usa transacción ni advisory lock. | Adquirir `AcquireAdvisoryXactLockAsync(espacio.Id)` siempre al crear o modificar reservas (con o sin auto-aprobación) y al dar de baja un espacio. |
| 5 | **IMPORTANTE** | Reglas de Negocio / Fechas | `src/GestionReservas.Application/Features/Reservas/Services/ReservasService.cs:472-485` | `CambiarEstadoReservaAsync` permite aprobar (`PATCH` con `accion = "aprobar"`) reservas cuya `FechaHoraInicio` o `FechaHoraFin` ya quedó en el pasado. | Validar en el bloque `if (nuevoEstado == EstadoReserva.Aprobada)` que `reserva.FechaHoraInicio > DateTime.UtcNow` (o arrojar `ConflictException` 409). |
| 6 | **IMPORTANTE** | Tests / Paralelismo | `tests/GestionReservas.Tests/UsuariosAndCatalogosEndpointsTests.cs:374,384,394` y `ConsolidacionAjustesEndpointsTests.cs:175-196` | `Catalogos_DevuelvenDatosDelSeed` valida `Assert.Equal(8, equipamientos.Count)`. Como xUnit ejecuta clases en paralelo sobre la misma BD (`GestionReservasDb_Test`), si coincide con el `POST /api/equipamientos` antes del `finally`, el test falla por condición de carrera. | Cambiar `Assert.Equal(8, ...)` por `Assert.True(equipamientos.Count >= 8)` o deshabilitar paralelismo entre colecciones que mutan catálogos. |
| 7 | **IMPORTANTE** | Permisos / Catálogos | `src/GestionReservas.Api/Controllers/DepartamentosController.cs:13`, `EquipamientosController.cs:13,41`, `TiposEspacioController.cs:13,41`, `RolesController.cs:13` | El permiso `PermisosDefinidos.CatalogosVer` (`catalogos.ver`) está definido y sembrado pero no se exige en ningún controller (usan solo `[Authorize]`). Además, los `POST` de catálogos usan `[Authorize(Roles = "Administrador")]` en lugar de políticas de permisos, y `GET /api/roles` queda expuesto a solicitantes. | Aplicar `[Authorize(Policy = PermisosDefinidos.CatalogosVer)]` en lecturas de catálogos y `[Authorize(Policy = PermisosDefinidos.UsuariosGestionar)]` en `GET /api/roles`. |
| 8 | **IMPORTANTE** | Permisos / Bloqueos | `src/GestionReservas.Application/Features/Bloqueos/Services/BloqueosService.cs:260-268` y `DatabaseSeeder.cs:99` | `docs/ENDPOINTS.md` especifica que `GET /api/bloqueos` requiere rol `Autenticado`, pero el seeder no asigna `bloqueos.ver` a `Solicitante` y `BloqueosService` lanza `ForbiddenException` (403) si no es Admin, Secretaría o Responsable. | **A verificar**: Definir si `Solicitante` debe poder consultar bloqueos (ajustar seeder y service) o actualizar `docs/ENDPOINTS.md` para reflejar la restricción administrativa. |
| 9 | **IMPORTANTE** | Calidad / Paginación | `src/GestionReservas.Application/Features/Reservas/Services/ReservasService.cs:225-263` y `NotificacionesService.cs:23-48` | `GET /api/reservas/pendientes`, `/canceladas`, `/rechazadas` y `GET /api/notificaciones` devuelven listas completas sin paginación ni límite `.Take()`. Además, el `README.md` afirma que `/api/notificaciones` es paginado. | Incorporar paginación (`PagedResultDto`) o aplicar un `.Take(maxItems)` defensivo y alinear la documentación. |
| 10 | **IMPORTANTE** | Calidad / Rendimiento | `src/GestionReservas.Application/Features/Espacios/Services/DisponibilidadService.cs:90-100` y `153-167` | 1) `ObtenerAsignacionSuperpuestaAsync` trae a memoria todas las `AsignacionesEspacio` activas del aula con 4 `Include` en cada validación. 2) `ObtenerEspaciosNoDisponiblesQuery` (líneas 155-166) evalúa un único `DayOfWeek` (`localDesde.DayOfWeek`), por lo que si recibe un rango multi-día no detecta cursadas de los días siguientes. | Filtrar `a.DiaSemana == diaSemana` y rango horario directamente en SQL dentro de `ObtenerAsignacionSuperpuestaAsync`; documentar o iterar días en `ObtenerEspaciosNoDisponiblesQuery`. |
| 11 | **IMPORTANTE** | Documentación / Endpoints | `docs/ENDPOINTS.md:1-71` | El contrato en `docs/ENDPOINTS.md` tiene 11 endpoints implementados sin documentar (`PUT/DELETE /api/espacios/{id}`, `GET /api/reservas`, `GET /api/reservas/rechazadas`, `GET/PUT /api/reservas/{id}`, `DELETE /api/bloqueos/{id}`, `POST /api/equipamientos`, `GET/POST /api/tipos-espacio`, `GET /api/roles`, `GET/PATCH /api/notificaciones`). | Actualizar las tablas de `docs/ENDPOINTS.md` con todos los endpoints reales, sus verbos, rutas, parámetros y políticas requeridas. |
| 12 | **MENOR** | Seguridad / Permisos | `src/GestionReservas.Api/Controllers/ReservasController.cs:68-75` | `GET api/reservas/{id}` y `PATCH api/reservas/{id}` solo tienen `[Authorize]` de clase y carecen de atributos `[ProducesResponseType]` para 400/403/404/409. En `GET api/reservas/{id}`, un usuario autenticado sin `reservas.ver_propias` podría consultar su reserva si fuera dueño. | Agregar `[ProducesResponseType]` en todos los métodos de `ReservasController` y verificar `HasPermission(ReservasVerPropias) || HasPermission(ReservasVerPendientes)` en `GetReservaByIdAsync`. |
| 13 | **MENOR** | Auditoría | `src/GestionReservas.Application/Features/Auth/Services/AuthService.cs:77-103` y `ReservasService.cs:875` | 1) En `LoginFallido` (contraseña incorrecta o inactivo), no se guarda el `rol` en `Auditoria` aunque los roles del usuario ya fueron cargados en memoria. 2) En `ConstruirDetalleReservaAsync` (línea 875), se usa `a.Usuario.NombreCompleto` ignorando el snapshot `a.UsuarioNombre`. | Pasar `rol: string.Join(", ", usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre))` en `LoginFallido` y usar `a.UsuarioNombre ?? a.Usuario.NombreCompleto ?? "Sistema"` en `ReservasService`. |
| 14 | **MENOR** | Validaciones | `src/GestionReservas.Application/Features/Reservas/Validators/ReservaValidators.cs:178-211` y `BloqueoValidators.cs:36-45` | `MisReservasQueryValidator`, `ReservasQueryParametersValidator` y `BloqueosQueryParametersValidator` no validan que `Desde <= Hasta` cuando ambos filtros están presentes (a diferencia de `AuditoriaQueryParametersValidator`). | Agregar `.Must(x => !x.Desde.HasValue || !x.Hasta.HasValue || NormalizeToUtc(x.Desde.Value) <= NormalizeToUtc(x.Hasta.Value))` en los tres validadores. |
| 15 | **MENOR** | Validaciones / Disponibilidad | `src/GestionReservas.Application/Features/Espacios/Validators/DisponibilidadEspacioQueryParametersValidator.cs:6-29` | `GET /api/espacios/disponibles` no valida longitudes máximas de `Edificio`/`Sector`, ni valida si `EquipamientoIds` contiene IDs `<= 0`. | Agregar reglas `.MaximumLength(100)` para `Edificio`/`Sector` y `RuleForEach(x => x.EquipamientoIds).GreaterThan(0)`. |
| 16 | **MENOR** | Dominio / Máquina de Estados | `src/GestionReservas.Application/Features/Reservas/ReservaStateMachine.cs:18-21` | El enum `EstadoReserva.Solicitada` (valor 1) está en el dominio y en `docs/ENUNCIADO.md` (`Solicitada -> PendienteAprobacion`), pero `DeterminarEstadoInicial` asigna directamente `PendienteAprobacion` o `Aprobada`. `Solicitada` nunca se persiste. | **A verificar**: Si `Solicitada` y `PendienteAprobacion` son equivalentes en el flujo síncrono, documentar la decisión en `README.md`. |
| 17 | **MENOR** | Calidad / EF Core | `src/GestionReservas.Application/Features/Reservas/Services/ReservasService.cs:285-288`, `EspaciosService.cs:57-60`, `BloqueosService.cs:254-256`, `UsuariosService.cs:161-163` | Uso de `.Include(...)` antes de proyecciones `.Select(...)` hacia DTOs. EF Core ignora los `Include` cuando hay un `Select` explícito, dejando código redundante. | Eliminar los `.Include()` en las consultas que proyectan directamente con `.Select(...)`. |
| 18 | **MENOR** | Seguridad / Configuración | `src/GestionReservas.Infrastructure/DependencyInjection.cs:40` y `CustomWebApplicationFactory.cs:20,30,55` | La clave JWT de fallback para `Development`/`Testing` está hardcodeada en `DependencyInjection.cs`, contradiciendo parcialmente el `README.md` (aunque bloquea su uso en `Production`). | Mover el fallback exclusivamente a `appsettings.Development.json` / User Secrets y dejar que `DependencyInjection.cs` exija siempre `Jwt:Key` configurada. |
| 19 | **MENOR** | Lógica / Espacios | `src/GestionReservas.Application/Features/Espacios/Services/EspaciosService.cs:63-66` y `164` | Cualquier `Solicitante` puede enviar `GET /api/espacios?incluirBajas=true` y listar espacios dados de baja, mientras que `GET /api/espacios/{id}` devuelve 404 incluso al `Administrador` si el espacio fue dado de baja. | Restringir `IncluirBajas = true` a roles administrativos o permitir que `GetEspacioByIdAsync` sea consultado por administradores aunque tenga `FechaBaja != null`. |
| 20 | **MENOR** | Documentación / README | `README.md:22,99,143,214` | 1) No documenta `GET /api/auditoria` ni los 5 tests de `AuditoriaEndpointsTests` (figura 53 tests en vez de 58). 2) Menciona "14 permisos" cuando `PermisosDefinidos.Todos` tiene 15. 3) Indica parámetro `soloNoLeidas` en `/api/notificaciones` cuando el código usa `leida`. | Actualizar `README.md` incorporando Tarea 7, corrigiendo el conteo a 15 permisos y 58 tests, y ajustando el nombre del query param `leida`. |

---

## 2. Revisión Punto por Punto

### Punto 1 y 2: Criterios de Aceptación, Reglas de Negocio y Situaciones
| Regla / Situación | Estado | Archivo de Implementación | Test que lo cubre |
| :--- | :---: | :--- | :--- |
| Conflicto simultáneo / Reservas paralelas en mismo espacio y horario | ✅ Cubierto | `ReservasService.cs:403,479`, `20261111134818_AddReservaExclusionConstraint.cs:16` | `ReservasEndpointsTests.Aprobaciones_EnParalelo_SoloUnaQuedaAprobada` (`ReservasEndpointsTests.cs:234`) |
| Superposición con reserva aprobada (409) y horario contiguo permitido (201) | ✅ Cubierto | `DisponibilidadService.cs:53-69`, `ReservasService.cs:775` | `ReservasEndpointsTests.Superposicion_ConAprobada_Devuelve409_YHorarioContiguoOk` (`ReservasEndpointsTests.cs:101`) y `EspaciosEndpointsTests.cs:421` |
| Capacidad máxima excedida (`asistentes > capacidadMaxima` $\to$ 400) | ✅ Cubierto | `ReservasService.cs:696-701` | `ReservasEndpointsTests.Crear_CapacidadExcedida_Devuelve400` (`ReservasEndpointsTests.cs:127`) |
| Reservar en espacio bloqueado o `FueraDeServicio` (409) | ✅ Cubierto | `ReservasService.cs:691,760` | `ReservasEndpointsTests.Crear_EspacioBloqueado_Devuelve409` (`ReservasEndpointsTests.cs:136`) y `BloqueosEndpointsTests.cs:197` |
| Colisión con cursada académica oficial `AsignacionEspacio` (409) | ✅ Cubierto | `DisponibilidadService.cs:81-127`, `ReservasService.cs:750`, `BloqueosService.cs:95` | `ConsolidacionAjustesEndpointsTests.Reserva_EnHorarioCursadaOficial_Devuelve409Conflict` (`ConsolidacionAjustesEndpointsTests.cs:49`) |
| Modificar reserva aprobada vuelve a `PendienteAprobacion` | ✅ Cubierto | `ReservaStateMachine.cs:56-65`, `ReservasService.cs:588` | `ReservasEndpointsTests.Modificar_Aprobada_VuelveAPendiente` (`ReservasEndpointsTests.cs:212`) |
| Bloquear espacio con reservas futuras (`cancelarReservasAfectadas = false` $\to$ 409 con lista; `true` $\to$ 201, cancela, audita y notifica) | ✅ Cubierto | `BloqueosService.cs:105-179` | `BloqueosEndpointsTests.Crear_ConAfectadas_FlagFalse_Devuelve409ConLista` (`:100`) y `Crear_ConAfectadas_FlagTrue_Devuelve201YCancelaReservasYNotifica` (`:145`) |
| Baja lógica de espacio con reservas futuras pendientes/aprobadas (409) | ✅ Cubierto | `EspaciosService.cs:420-433` | `EspaciosEndpointsTests.BajaLogica_ConReservasFuturasAprobadasOPendientes_Devuelve409Conflict` (`EspaciosEndpointsTests.cs:331`) |
| Responsable operando en departamento ajeno (403 Forbidden) | ✅ Cubierto | `EspacioAuthorizationService.cs:22-63` | `EspaciosEndpointsTests.cs:174`, `ReservasEndpointsTests.cs:202`, `BloqueosEndpointsTests.cs:250` |
| Operación sin permisos por `Solicitante` (403) y sin token (401) | ✅ Cubierto | Políticas en `DependencyInjection.cs:100-106` | `EspaciosEndpointsTests.cs:271,319`, `ReservasEndpointsTests.cs:191`, `BloqueosEndpointsTests.cs:231`, `AuditoriaEndpointsTests.cs:52` |
| Rechazar reserva sin motivo (400) y con motivo registrado en detalle/historial | ✅ Cubierto | `ReservaValidators.cs:166`, `ReservasService.cs:464` | `ReservasEndpointsTests.Rechazar_SinMotivo_400_YConMotivoQuedaEnDetalle` (`ReservasEndpointsTests.cs:160`) |
| Cancelar reserva `Aprobada` exige motivo obligatorio | ⚠️ Solo código | `ReservaStateMachine.cs:81-84`, `ReservasService.cs:464-469` | **Falta test explícito** de cancelación sin motivo sobre reserva `Aprobada`. |
| Intervención excepcional de `SecretariaAcademica` en otro depto exige motivo | ⚠️ Solo código | `ReservasService.cs:423-439` | **Falta test explícito**. |
| Protección de auto-desactivación de Admin y último Admin activo (409) | ✅ Cubierto | `UsuariosService.cs:284-299, 330-346` | `UsuariosAndCatalogosEndpointsTests.Admin_NoPuedeDesactivarseASiMismo_Devuelve409Conflict` (`:230`) |

### Punto 3: Comparación de Endpoints (`Controllers` vs `docs/ENDPOINTS.md`)
- **Coinciden en ruta y verbo (17 endpoints)**:
  - `POST /api/auth/login`, `GET /api/auth/me`
  - `POST /api/usuarios`, `GET /api/usuarios`, `GET /api/usuarios/{id}`, `PATCH /api/usuarios/{id}`
  - `GET /api/departamentos`, `GET /api/equipamientos`, `GET /api/auditoria`
  - `GET /api/espacios`, `GET /api/espacios/disponibles`, `GET /api/espacios/{id}`, `POST /api/espacios`
  - `POST /api/reservas`, `GET /api/mis-reservas`, `GET /api/reservas/pendientes`, `GET /api/reservas/canceladas`, `PATCH /api/reservas/{id}`
  - `POST /api/bloqueos`, `GET /api/bloqueos`
  - `GET /health`, `GET /swagger`
- **Implementados en Controllers pero ausentes en `docs/ENDPOINTS.md` (11 endpoints)**:
  - `PUT /api/espacios/{id}`, `DELETE /api/espacios/{id}`
  - `GET /api/reservas` (listado administrativo paginado), `GET /api/reservas/rechazadas`, `GET /api/reservas/{id}`, `PUT /api/reservas/{id}`
  - `DELETE /api/bloqueos/{id}`
  - `POST /api/equipamientos`, `GET /api/tipos-espacio`, `POST /api/tipos-espacio`, `GET /api/roles`
  - `GET /api/notificaciones`, `PATCH /api/notificaciones/{id}/leer`
- **Faltan implementar (Tarea 8)**:
  - `GET /api/calendario`

### Punto 4: Seguridad y Permisos
- **IDOR**: Correctamente prevenido en `GetMisReservasAsync` (filtra por `usuarioId`), `GetReservaByIdAsync`, `CambiarEstadoReservaAsync`, `UpdateReservaAsync` y `MarcarComoLeidaAsync` (verifica `notificacion.UsuarioId != usuarioId` $\to$ 403).
- **DTOs y datos sensibles**: Ningún DTO expone `PasswordHash` ni el secreto JWT; las auditorías de usuarios excluyen contraseñas (verificado por test en `UsuariosAndCatalogosEndpointsTests.cs:354`).
- **CORS**: Falta configurar en `Program.cs` (Hallazgo #3).
- **Políticas**: Falta aplicar `PermisosDefinidos.CatalogosVer` en los catálogos y `RolesController` expone `GET /api/roles` a cualquier autenticado (Hallazgo #7).

### Punto 5: Integridad y Concurrencia
- **Restricción física**: Migración `20261111134818_AddReservaExclusionConstraint.cs` implementa `EXCLUDE USING gist ("EspacioId" WITH =, tstzrange("FechaHoraInicio", "FechaHoraFin", '[)') WITH &&) WHERE ("Estado" = 3)`.
- **Concurrencia optimista**: `ReservaConfiguration.cs:23` configura `RowVersion` (`xmin`). `ReservasService` captura `DbUpdateConcurrencyException` y `DbUpdateException` (códigos `23P01` y `40001`) convirtiéndolos en `ConflictException` (409).
- **Hueco de concurrencia**: `CreateReservaAsync` cuando `autoAprobar == false`, `UpdateReservaAsync` y `EliminarEspacioAsync` no adquieren `pg_advisory_xact_lock(espacio.Id)` (Hallazgo #4).

### Punto 6: Fechas y Zonas Horarias
- `DateTimeZoneHelper` normaliza a `DateTimeKind.Utc` convirtiendo `DateTimeKind.Unspecified` desde `America/Argentina/Buenos_Aires`.
- Se bloquea crear o modificar reservas y bloqueos en el pasado (con tolerancia de 2 minutos por latencia de red).
- **Hueco**: Al aprobar una reserva pendiente (`PATCH /api/reservas/{id}`), no se verifica si la fecha de inicio ya quedó en el pasado (Hallazgo #5).

### Punto 7: Auditoría y Notificaciones
- Todas las operaciones de escritura relevantes (`LoginExitoso`, `LoginFallido`, `CrearUsuario`, `ModificarUsuario`, `CrearEspacio`, `ModificarEspacio`, `EliminarEspacio`, `CrearEquipamiento`, `CrearTipoEspacio`, `SolicitarReserva`, `CrearAutoAprobada`, `AprobarReserva`, `RechazarReserva`, `CancelarReserva`, `ModificarReserva`, `CrearBloqueo`, `LevantarBloqueo`) registran en `Auditorias` con snapshot de actor (`UsuarioId`, `UsuarioNombre`, `UsuarioEmail`, `Rol`, `FechaHora`, `Accion`, `Entidad`, `EntidadId`, `Motivo`, `Detalle`).
- Único endpoint sin auditoría: `PATCH /api/notificaciones/{id}/leer` (aceptable por ser lectura de aviso).

### Punto 8: Calidad de Código
- **Controllers limpios**: Ningún controller contiene lógica de negocio; todos delegan en su servicio de `Application`.
- **Serialización de Enums**: Configurada globalmente como string con `JsonStringEnumConverter` en `Program.cs:24`.
- **Consultas en memoria / Paginación**: Ver Hallazgos #9, #10 y #17.

### Punto 9: Suite de Tests
- **Total actual**: 58 tests pasando en 7 archivos (`Auth`: 8, `UsuariosAndCatalogos`: 11, `Espacios`: 10, `Reservas`: 10, `Bloqueos`: 9, `ConsolidacionAjustes`: 5, `Auditoria`: 5).
- **Fragilidad**: Ver Hallazgos #2 y #6.

### Punto 10: Documentación
- `README.md` incluye arquitectura, User Secrets, credenciales de prueba, regla de reservas afectadas por bloqueo, zona horaria y concurrencia. Solo resta sincronizar el módulo de Auditoría (Tarea 7) y ajustar detalles menores (Hallazgo #20).

---

## 3. Qué falta para la Demo Final

1. **Implementar Tarea 8 (`GET /api/calendario`)**:
   - Endpoint con `desde` y `hasta` obligatorios (rango máximo validado, ej. 31 días) y filtros opcionales (`espacioId`, `edificio`, `tipoEspacioId`, `departamentoId`).
   - Proyección unificada combinando: Reservas (`Aprobada` y `PendienteAprobacion`), Bloqueos vigentes y `AsignacionesEspacio` recurrentes proyectadas en los días correspondientes del rango.
2. **Corregir `GET /api/auth/me` (`AuthService.cs`)**:
   - Devolver el `Departamento` real del usuario desde `usuario.DepartamentoId` en lugar del filtro hardcodeado por texto.
3. **Configurar CORS en `Program.cs`**:
   - Habilitar CORS para permitir peticiones desde el frontend o clientes externos durante la demo.
4. **Completar Tests Faltantes (Tarea 9)**:
   - Tests de `GET /api/calendario`.
   - Tests de intervención excepcional de `SecretariaAcademica`, cancelación de aprobada sin motivo (400), e intento IDOR entre solicitantes (403).
   - Corregir la fecha relativa en `EspaciosEndpointsTests.cs:390` para que no falle los martes.
5. **Sincronizar Documentación Final (Tarea 9)**:
   - Actualizar `docs/ENDPOINTS.md` con todos los endpoints implementados (incluyendo Auditoría y Calendario).
   - Actualizar `README.md` con Tareas 7 y 8.

---

## 4. Top 5 a Arreglar Primero

1. **#1 (CRÍTICO) — `AuthService.GetCurrentUserProfileAsync` (`AuthService.cs:180-209`)**: Reemplazar la búsqueda hardcodeada de departamento por `.Include(u => u.Departamento)` usando `usuario.DepartamentoId`.
2. **#2 (CRÍTICO) — `EspaciosEndpointsTests.cs:390`**: Fijar un día que no sea martes en `Disponibles_ConCapacidadYEquipamiento_FiltraAdecuadamente` para evitar que falle los martes por la cursada oficial `A1011` en `AULA-101`.
3. **#3 (IMPORTANTE) — `Program.cs`**: Configurar `AddCors` y `UseCors` para que el backend sea consumible desde el navegador/frontend sin bloqueo de origen cruzado.
4. **#4 y #5 (IMPORTANTE) — `ReservasService.cs`**: Adquirir `AcquireAdvisoryXactLockAsync(espacio.Id)` siempre en `CreateReservaAsync` / `UpdateReservaAsync` y validar en `CambiarEstadoReservaAsync` que no se aprueben reservas cuya fecha ya pasó.
5. **#6 (IMPORTANTE) — `UsuariosAndCatalogosEndpointsTests.cs:384`**: Cambiar `Assert.Equal(8, equipamientos.Count)` a `Assert.True(equipamientos.Count >= 8)` para evitar fallos intermitentes por ejecución en paralelo con `ConsolidacionAjustesEndpointsTests`.
