# Contrato de Endpoints de la API

Especificación completa de todos los endpoints implementados en el backend del **Sistema de Gestión y Reservas de Espacios de la Facultad** (.NET 8 + EF Core 8 + PostgreSQL).

## Convenciones Generales
- **Formato de intercambio**: `application/json` (propiedades en `camelCase`).
- **Serialización de enumeraciones**: Texto (`JsonStringEnumConverter`).
- **Fechas y Horas**: ISO 8601 (`YYYY-MM-DDTHH:mm:ssZ`). Persistencia en UTC; conversión horaria oficial de la facultad en `America/Argentina/Buenos_Aires`.
- **Errores estandarizados**: RFC 7807 `ProblemDetails` con códigos HTTP `400`, `401`, `403`, `404`, `409` y `500`.

---

## 1. Módulo de Autenticación y Perfil (`/api/auth`)

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Público (`[AllowAnonymous]`) | Body: `{ email, password }` | `200`, `400`, `401` | Inicia sesión, audita el intento y emite token JWT con claims de roles y permisos. |
| `GET` | `/api/auth/me` | Autenticado (`[Authorize]`) | — | `200`, `401`, `404` | Devuelve perfil del usuario autenticado, roles, permisos y departamento real (`usuario.DepartamentoId`). |

---

## 2. Módulo de Usuarios y Roles (`/api/usuarios`, `/api/roles`)

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/usuarios` | `usuarios.gestionar` | Body: `{ nombre, apellido, email, telefono, password, departamentoId, roles }` (`roles`: lista de nombres de rol) | `201`, `400`, `401`, `403`, `404`, `409` | Crea un nuevo usuario validando fortaleza de contraseña, unicidad de email y existencia de roles. |
| `GET` | `/api/usuarios` | `usuarios.gestionar` | Query: `page`, `pageSize`, `search`, `rol`, `departamentoId`, `activo` | `200`, `400`, `401`, `403` | Listado paginado de usuarios con filtros combinados. |
| `GET` | `/api/usuarios/{id}` | `usuarios.gestionar` | Ruta: `id` (`int`) | `200`, `401`, `403`, `404` | Detalle completo de un usuario por ID. |
| `PATCH` | `/api/usuarios/{id}` | `usuarios.gestionar` | Ruta: `id`; Body: `{ nombre, apellido, telefono, departamentoId, roles, activo, password }` | `200`, `400`, `401`, `403`, `404`, `409` | Actualización parcial de datos, roles o estado (protege auto-desactivación y último Admin activo). |
| `GET` | `/api/roles` | `usuarios.gestionar` | — | `200`, `401`, `403` | Catálogo de roles disponibles (`id`, `nombre`, `descripcion`). |

---

## 3. Módulo de Catálogos (`/api/departamentos`, `/api/equipamientos`, `/api/tipos-espacio`)

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/departamentos` | `catalogos.ver` | — | `200`, `401`, `403` | Lista de departamentos y secretarías académicas con `cantidadEspacios` activos. |
| `GET` | `/api/equipamientos` | `catalogos.ver` | — | `200`, `401`, `403` | Catálogo flexible de equipamientos tecnológicos disponibles. |
| `POST` | `/api/equipamientos` | `usuarios.gestionar` | Body: `{ nombre, descripcion }` | `201`, `400`, `401`, `403`, `409` | Alta administrativa de nuevo equipamiento con validación de nombre único y auditoría. |
| `GET` | `/api/tipos-espacio` | `catalogos.ver` | — | `200`, `401`, `403` | Catálogo de clasificaciones funcionales de espacios físicos. |
| `POST` | `/api/tipos-espacio` | `usuarios.gestionar` | Body: `{ nombre, descripcion }` | `201`, `400`, `401`, `403`, `409` | Alta administrativa de nuevo tipo de espacio físico con validación de unicidad y auditoría. |

---

## 4. Módulo de Espacios Físicos y Disponibilidad (`/api/espacios`)

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/espacios` | `espacios.ver` | Query: `page`, `pageSize`, `search`, `tipoEspacioId`, `edificio`, `piso`, `sector`, `capacidadMinima`, `departamentoId`, `estado`, `equipamientoIds`, `incluirBajas` (solo con permiso de alta/edición/baja) | `200`, `400`, `401`, `403` | Listado paginado de espacios físicos con filtros opcionales. |
| `GET` | `/api/espacios/disponibles` | `espacios.ver` | Query: `fecha` (obligatorio), `horaInicio` (obligatorio), `horaFin` (obligatorio), `capacidadMinima`, `tipoEspacioId`, `edificio`, `sector`, `departamentoId`, `equipamientoIds` | `200`, `400`, `401`, `403` | Busca espacios disponibles en fecha y horario evaluando reservas aprobadas, bloqueos y cursadas (`AsignacionEspacio`). |
| `GET` | `/api/espacios/{id}` | `espacios.ver` | Ruta: `id` (`int`) | `200`, `401`, `403`, `404` | Detalle completo de un espacio físico y su dotación de equipamiento (`404` si está dado de baja). |
| `POST` | `/api/espacios` | `espacios.crear` | Body: `{ codigo, nombre, tipoEspacioId, edificio, piso, sector, capacidadMaxima, departamentoId, estado, equipamientos }` | `201`, `400`, `401`, `403`, `404`, `409` | Crea un nuevo espacio físico (Responsable solo en su departamento; Admin/Secretaría en cualquiera). |
| `PUT` | `/api/espacios/{id}` | `espacios.editar` | Ruta: `id`; Body: `{ codigo, nombre, tipoEspacioId, edificio, piso, sector, capacidadMaxima, departamentoId, estado, equipamientos }` | `200`, `400`, `401`, `403`, `404`, `409` | Modifica integralmente un espacio físico protegiendo reservas futuras activas contra `FueraDeServicio`, reducción de capacidad o quita/reducción de equipamiento requerido. |
| `DELETE` | `/api/espacios/{id}` | `espacios.eliminar` | Ruta: `id` (`int`) | `204`, `401`, `403`, `404`, `409` | Baja lógica (`FechaBaja`). Rechaza con `409 Conflict` si posee reservas futuras pendientes o aprobadas. |

---

## 5. Módulo de Reservas Eventuales (`/api/reservas`, `/api/mis-reservas`)

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/reservas` | `reservas.solicitar` | Body: `{ espacioId, fechaHoraInicio, fechaHoraFin, tipoActividad, asistentesEstimados, motivo, observaciones, equipamientoRequerido }` | `201`, `400`, `401`, `403`, `404`, `409` | Crea una solicitud de reserva (auto-aprobada si el usuario administra el departamento del espacio). |
| `GET` | `/api/mis-reservas` | `reservas.ver_propias` | Query: `page`, `pageSize`, `estado`, `desde`, `hasta` | `200`, `400`, `401`, `403` | Devuelve las reservas paginadas del usuario autenticado. |
| `GET` | `/api/reservas` | `reservas.ver_pendientes` | Query: `page`, `pageSize`, `estado`, `espacioId`, `departamentoId`, `desde`, `hasta` | `200`, `400`, `401`, `403` | Listado administrativo paginado con alcance departamental (Responsable ve su depto; Admin/Secretaría todos). |
| `GET` | `/api/reservas/pendientes` | `reservas.ver_pendientes` | Query: `page`, `pageSize` | `200`, `400`, `401`, `403` | Listado paginado de reservas en estado `PendienteAprobacion` según alcance departamental. |
| `GET` | `/api/reservas/canceladas` | `reservas.ver_pendientes` | Query: `page`, `pageSize` | `200`, `400`, `401`, `403` | Listado paginado de reservas en estado `Cancelada` según alcance departamental. |
| `GET` | `/api/reservas/rechazadas` | `reservas.ver_pendientes` | Query: `page`, `pageSize` | `200`, `400`, `401`, `403` | Listado paginado de reservas en estado `Rechazada` según alcance departamental. |
| `GET` | `/api/reservas/{id}` | Autenticado + (`reservas.ver_propias` o `reservas.ver_pendientes`) + dueño o gestor del depto | Ruta: `id` (`int`) | `200`, `401`, `403`, `404` | Detalle completo de una reserva junto con su `historialCambios` leído de auditoría. |
| `PATCH` | `/api/reservas/{id}` | `reservas.aprobar_rechazar` / `reservas.cancelar` | Ruta: `id`; Body: `{ accion: "aprobar" \| "rechazar" \| "cancelar", motivo }` | `200`, `400`, `401`, `403`, `404`, `409` | Cambia el estado de una reserva aplicando `ReservaStateMachine`, concurrencia y notificaciones. |
| `PUT` | `/api/reservas/{id}` | `reservas.modificar` | Ruta: `id`; Body: `{ fechaHoraInicio, fechaHoraFin, tipoActividad, asistentesEstimados, motivo, observaciones, equipamientoRequerido }` | `200`, `400`, `401`, `403`, `404`, `409` | Modifica una reserva existente; si estaba `Aprobada`, vuelve a `PendienteAprobacion`. |

---

## 6. Módulo de Bloqueos de Espacios (`/api/bloqueos`)

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/bloqueos` | `bloqueos.crear` | Body: `{ espacioId, desde, hasta, motivo, cancelarReservasAfectadas }` | `201`, `400`, `401`, `403`, `404`, `409` | Crea un bloqueo temporal. Si hay reservas afectadas y `cancelarReservasAfectadas = false` devuelve `409` con la lista; si es `true`, las cancela atómicamente, audita y notifica. |
| `GET` | `/api/bloqueos` | `bloqueos.ver` | Query: `page`, `pageSize`, `espacioId`, `desde`, `hasta`, `vigentes` | `200`, `400`, `401`, `403` | Listado paginado de bloqueos respetando alcance departamental. |
| `DELETE` | `/api/bloqueos/{id}` | `bloqueos.crear` | Ruta: `id` (`int`) | `204`, `401`, `403`, `404` | Levanta/elimina el bloqueo y registra en auditoría (no restaura reservas previamente canceladas). |

---

## 7. Módulo de Notificaciones (`/api/notificaciones`)

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/notificaciones` | Autenticado (`[Authorize]`) | Query: `leida` (`bool?`), `page`, `pageSize` | `200`, `400`, `401` | Devuelve las notificaciones paginadas del usuario autenticado ordenadas por fecha descendente. |
| `PATCH` | `/api/notificaciones/{id}/leer` | Autenticado (dueño) | Ruta: `id` (`int`) | `200`, `401`, `403`, `404` | Marca una notificación propia como leída (`403` si pertenece a otro usuario). |

---

## 8. Módulo de Auditoría (`/api/auditoria`)

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/auditoria` | `auditoria.ver` | Query: `page`, `pageSize`, `usuarioId`, `accion`, `entidad`, `desde`, `hasta` | `200`, `400`, `401`, `403` | Bitácora institucional paginada de solo lectura ordenada por fecha descendente e ID descendente. |

---

## 9. Módulo de Calendario Unificado (`/api/calendario`)

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/calendario` | Autenticado (`[Authorize]`) | Query: `desde` (obligatorio), `hasta` (obligatorio, máx. 31 días), `espacioId`, `edificio`, `sector`, `tipoEspacioId`, `departamentoId` | `200`, `400`, `401` | Devuelve eventos unificados (`Reserva`, `Bloqueo`, `Cursada`) ordenados por `inicio` aplicando privacidad según rol. |

---

## 10. Monitoreo y Documentación

| Método | Ruta | Permiso / Política | Parámetros / Body | Respuestas | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/health` | Público | — | `200`, `503` | Estado operativo del servicio y verificación de conectividad con PostgreSQL. |
| `GET` | `/swagger` | Público | — | `200` | Interfaz interactiva OpenAPI / Swagger UI con soporte de autorización Bearer JWT. |
