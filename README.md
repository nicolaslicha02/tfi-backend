# Sistema de Gestión y Reservas de Espacios de la Facultad - Backend

Backend desarrollado en **.NET 8** y **ASP.NET Core Web API** con **Entity Framework Core 8** y **PostgreSQL**, diseñado bajo una arquitectura en capas limpia, desacoplada y orientada al dominio (**Clean Architecture**).

---

## 1. Arquitectura de la Solución

La solución `GestionReservas.slnx` se divide en 4 proyectos principales más el proyecto de pruebas de integración:

```
back-office-tfi/
│
├── docs/
│   ├── ENUNCIADO.md                    # Requisitos, alcance y reglas de negocio
│   ├── ENDPOINTS.md                    # Contrato completo de los 29 endpoints HTTP
│   ├── CREDENCIALES_Y_SETUP.md         # Guía rápida de credenciales, setup y ejemplos JSON
│   ├── DECISIONES_MODELO.md            # Decisiones de diseño e inmutabilidad de auditoría
│   └── REVISION.md                     # Informe de revisión técnica del backend
│
├── src/
│   ├── GestionReservas.Domain/         # Entidades, Enums y constantes de permisos (15 permisos)
│   ├── GestionReservas.Application/    # Casos de uso, DTOs, validadores FluentValidation, ReservaStateMachine
│   ├── GestionReservas.Infrastructure/ # EF Core 8 + Npgsql, configuraciones Fluent API, migraciones, DatabaseSeeder, JWT
│   └── GestionReservas.Api/            # Controllers REST, ExceptionHandlingMiddleware (RFC 7807), CORS, Swagger
│
└── tests/
    └── GestionReservas.Tests/          # Suite de 81 pruebas de integración xUnit contra PostgreSQL (GestionReservasDb_Test)
```

---

## 2. Requisitos e Instalación

### Prerrequisitos
- **.NET 8 SDK** (o superior con `RollForward=Major` configurado).
- **PostgreSQL 16 / 18** ejecutándose en `localhost:5432`.

### Configuración de User Secrets y Cadena de Conexión
Por seguridad, ni la contraseña de PostgreSQL ni la clave criptográfica de firma JWT (`Jwt:Key`) se almacenan en `appsettings.json`. Configúrelas mediante **User Secrets** desde la raíz del repositorio:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=GestionReservasDb;Username=postgres;Password=postgres" --project src/GestionReservas.Api
dotnet user-secrets set "Jwt:Key" "ClaveSuperSecretaExtremadamenteLargaParaJwtGestionDeEspacios2026!#" --project src/GestionReservas.Api
```

### Configuración de CORS en `appsettings.Development.json`
El backend lee los orígenes habilitados desde la sección `Cors:AllowedOrigins` (y en entorno `Development` habilita por defecto `http://localhost:3000`, `http://localhost:5173` y `http://localhost:4200` con `AllowAnyHeader` y `AllowAnyMethod`, ejecutando `UseCors` antes de `UseAuthentication`):

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173",
      "http://localhost:4200"
    ]
  }
}
```

### Migraciones e Inicialización de Base de Datos
```powershell
dotnet ef database update --project src/GestionReservas.Infrastructure --startup-project src/GestionReservas.Api
dotnet run --project src/GestionReservas.Api
```
Al iniciar, `DatabaseSeeder` carga de forma idempotente los 4 roles, 15 permisos, 3 departamentos, 4 tipos de espacio, 8 equipamientos, 6 espacios físicos, 4 usuarios de prueba y la estructura académica 2026 (`CicloLectivo`, `Materia`, `Cursada`, `Comision`, `AsignacionEspacio`).

- **Swagger UI**: `http://localhost:5051/swagger`
- **Health Check**: `http://localhost:5051/health`

---

## 3. Credenciales de Prueba Sembradas (Seed)

| Rol | Nombre Completo | Email | Contraseña | Departamento Asignado | Permisos |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Administrador** | Administrador General | `admin@facultad.edu.ar` | `Admin123!` | `null` (Global) | Todos (15 permisos) |
| **SecretariaAcademica** | Secretaría Académica | `secretaria@facultad.edu.ar` | `Secretaria123!` | `null` (Global) | Espacios, Reservas, Bloqueos, Catálogos, Auditoría |
| **ResponsableEspacio** | Ing. Carlos Gutiérrez | `responsable.sistemas@facultad.edu.ar` | `Responsable123!` | Depto. de Sistemas e Informática (`Id = 1`) | Espacios, Reservas, Bloqueos y Catálogos (alcance Depto. 1) |
| **Solicitante** | Prof. Martín Pérez | `docente.perez@facultad.edu.ar` | `Docente123!` | Depto. de Ciencias Básicas (`Id = 2`) | `espacios.ver`, `catalogos.ver`, `reservas.solicitar`, `reservas.ver_propias`, `reservas.modificar`, `reservas.cancelar` |

---

## 4. Decisiones Técnicas y Reglas de Negocio Clave

### 4.1 Decisión sobre el Estado `Solicitada` vs `PendienteAprobacion`
El enumerado `EstadoReserva` incluye el valor `Solicitada` (1) además de `PendienteAprobacion` (2) para respetar el vocabulario del dominio descrito en el enunciado (`Solicitada -> PendienteAprobacion -> Aprobada / Rechazada / Cancelada`). Dado que la validación de disponibilidad, capacidad y equipamiento al crear la solicitud es **100% síncrona e inmediata** dentro de la transacción de `POST /api/reservas`:
- Si el creador es un `Solicitante` (o un `ResponsableEspacio` pidiendo un aula de otro departamento), `ReservaStateMachine.DeterminarEstadoInicial` persiste directamente la reserva en estado **`PendienteAprobacion`** y notifica en el acto a los responsables del departamento.
- Si el creador administra el departamento del espacio (`ResponsableEspacio` de ese departamento, `SecretariaAcademica` o `Administrador`), se persiste directamente como **`Aprobada`** (`CrearAutoAprobada`).

### 4.2 Regla de Reservas Afectadas por Bloqueo (`POST /api/bloqueos`)
1. **Detección**: Se buscan todas las reservas en estado `Aprobada` o `PendienteAprobacion` en ese espacio que se solapen con el rango (`inicioA < finB AND finA > inicioB`).
2. **Por defecto (`cancelarReservasAfectadas = false`)**: Si existen reservas afectadas, la operación **no crea el bloqueo** y devuelve **HTTP 409 Conflict** incluyendo en la respuesta JSON la propiedad `reservasAfectadas` (`id`, `solicitante`, `inicio`, `fin`, `estado`).
3. **Confirmación (`cancelarReservasAfectadas = true`)**: En **una única transacción** serializada con `pg_advisory_xact_lock(espacioId)`, se crea el bloqueo, se transicionan todas las reservas afectadas a `Cancelada` con motivo `"Cancelada por bloqueo del espacio: {motivo}"`, se registra una entrada en `Auditorias` por cada reserva cancelada y se encola una notificación para cada solicitante afectado.
4. **Levantamiento (`DELETE /api/bloqueos/{id}`)**: Elimina el bloqueo y lo audita (`LevantarBloqueo`). **Las reservas que habían sido canceladas NO se restauran automáticamente**, preservando la trazabilidad histórica y las notificaciones ya emitidas.

### 4.3 Zona Horaria Institucional y Manejo de Fechas
- La facultad opera en la zona horaria **`America/Argentina/Buenos_Aires`** (UTC-3), centralizada en `DateTimeZoneHelper`.
- Todas las columnas `timestamptz` en PostgreSQL se persisten y consultan en **UTC** (`DateTimeKind.Utc`).
- Las asignaciones académicas recurrentes (`AsignacionEspacio`: `DiaSemana`, `HoraInicio`, `HoraFin`) y las búsquedas de disponibilidad (`Fecha`, `HoraInicio`, `HoraFin`) se convierten entre la hora local de la facultad y UTC mediante `DateTimeZoneHelper.ToUtc` y `DateTimeZoneHelper.ToFacultadLocal`, contemplando cruces de medianoche y rangos multi-día.

### 4.4 Control de Concurrencia en 3 Niveles
1. **Bloqueo Consultivo Transaccional (`pg_advisory_xact_lock`)**: Se adquiere por `espacioId` en `CreateReservaAsync` (con o sin auto-aprobación), `UpdateReservaAsync`, `CambiarEstadoReservaAsync` (al aprobar), `CrearBloqueoAsync` y `EliminarEspacioAsync`.
2. **Concurrencia Optimista (`xmin`)**: Configurada como `RowVersion` en `Reserva`; cualquier colisión simultánea de escritura dispara `DbUpdateConcurrencyException` mapeada a **409 Conflict**.
3. **Restricción Física en PostgreSQL (`EXCLUDE USING gist`)**: La migración `AddReservaExclusionConstraint` impide a nivel motor que coexistan dos filas en `Reservas` con `Estado = 3` (`Aprobada`) para el mismo `EspacioId` con `tstzrange(FechaHoraInicio, FechaHoraFin, '[)')` superpuesto.

### 4.5 Calendario Unificado y Vistas Diaria / Semanal / Mensual
`GET /api/calendario` recibe `desde` y `hasta` obligatorios (`hasta >= desde`, máximo 31 días) y devuelve la lista unificada de `Reserva`, `Bloqueo` y `Cursada` ordenada por `inicio`. Las vistas diaria (1 día), semanal (7 días) y mensual (hasta 31 días) se resuelven parametrizando el intervalo `desde`/`hasta` sin requerir endpoints adicionales.

---

## 5. Ejecución de las Pruebas Automatizadas (xUnit)

La suite de pruebas (`tests/GestionReservas.Tests`) cuenta con **81 tests de integración** que se ejecutan contra una base de datos PostgreSQL aislada (`GestionReservasDb_Test`), verificando automáticamente que nunca se apunte a la base de desarrollo:

```powershell
# Ejecutar toda la suite (81 tests)
dotnet test

# Ejecutar únicamente los 11 criterios de aceptación de la defensa (Sección 13 del enunciado)
dotnet test --filter "FullyQualifiedName~CriteriosAceptacionDefensaTests"
```

---

## 6. Guion de Demo Paso a Paso (Recorrido de la Sección 14 del Enunciado)

1. **Inicio de Sesión y Perfil (`/api/auth/login` y `/api/auth/me`)**:
   - Loguearse como `docente.perez@facultad.edu.ar` (`Docente123!`) y como `responsable.sistemas@facultad.edu.ar` (`Responsable123!`).
   - Consultar `GET /api/auth/me` con ambos tokens para mostrar los roles, permisos y el departamento real asociado (`usuario.DepartamentoId`).
2. **Consulta de Catálogos y Disponibilidad (`/api/espacios/disponibles`)**:
   - Consultar `GET /api/departamentos`, `GET /api/equipamientos` y `GET /api/tipos-espacio`.
   - Consultar `GET /api/espacios/disponibles?fecha=2026-11-25&horaInicio=18:00&horaFin=21:00&capacidadMinima=60&equipamientoIds=1` (miércoles) $\to$ devuelve `AULA-101` y `AULA-201`.
   - Repetir para el martes `2026-11-24` de `18:00` a `21:00` $\to$ `AULA-101` no aparece disponible porque está ocupada por la cursada oficial `A1011` (Álgebra y Geometría Analítica).
3. **Solicitud de Reserva y Validaciones de Negocio (`POST /api/reservas`)**:
   - Como `docente.perez`, intentar reservar `LAB-SIS-01` (capacidad 32) con `asistentesEstimados = 50` $\to$ **400 Bad Request**.
   - Solicitar `LAB-SIS-01` con `asistentesEstimados = 25` para un miércoles de `10:00` a `12:00` $\to$ **201 Created** en estado `PendienteAprobacion`.
4. **Control de Alcance Departamental y Aprobación (`PATCH /api/reservas/{id}`)**:
   - Como `responsable.sistemas` (Depto. 1), intentar aprobar una reserva de `AULA-201` (Depto. 2) $\to$ **403 Forbidden**.
   - Aprobar la reserva de `LAB-SIS-01` (Depto. 1) con `{ "accion": "aprobar" }` $\to$ **200 OK** (`Estado = "Aprobada"`).
   - Intentar crear o aprobar otra reserva en `LAB-SIS-01` el mismo día de `11:00` a `13:00` $\to$ **409 Conflict**; probar de `12:00` a `14:00` (horario contiguo) $\to$ **201 Created**.
5. **Modificación de Reserva Aprobada (`PUT /api/reservas/{id}`)**:
   - Como `docente.perez`, modificar el horario de la reserva aprobada $\to$ **200 OK** y su estado vuelve automáticamente a **`PendienteAprobacion`**.
6. **Bloqueo con Reservas Afectadas (`POST /api/bloqueos`)**:
   - Como `responsable.sistemas` o `admin`, enviar `POST /api/bloqueos` sobre `LAB-SIS-01` cubriendo el horario de la reserva con `"cancelarReservasAfectadas": false` $\to$ **409 Conflict** devolviendo la lista `reservasAfectadas`.
   - Reenviar con `"cancelarReservasAfectadas": true` $\to$ **201 Created**, la reserva pasa a `Cancelada`, y el docente recibe el aviso en `GET /api/notificaciones?leida=false`.
7. **Calendario Unificado y Privacidad (`GET /api/calendario`)**:
   - Consultar `GET /api/calendario?desde=2026-11-01T00:00:00Z&hasta=2026-11-30T23:59:59Z` como `docente.perez`: muestra sus propias reservas con detalle completo, las reservas ajenas como `"Ocupado"` sin datos sensibles, los bloqueos como `"Bloqueado"` sin motivo y las cursadas proyectadas en cada martes y jueves del mes.
   - Repetir la consulta como `admin@facultad.edu.ar`: visualiza todos los solicitantes, motivos y observaciones.
8. **Bitácora de Auditoría (`GET /api/auditoria`)**:
   - Consultar `GET /api/auditoria?page=1&pageSize=20` como `admin@facultad.edu.ar` o `secretaria@facultad.edu.ar` para verificar el registro cronológico inverso con usuario, rol histórico, acción, entidad, ID y motivo de todas las operaciones realizadas.
