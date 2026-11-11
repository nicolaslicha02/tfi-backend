# Sistema de Gestión y Reservas de Espacios de la Facultad - Backend

Backend desarrollado en **.NET 8** y **ASP.NET Core Web API** con **Entity Framework Core 8** y **PostgreSQL**, diseñado bajo una arquitectura en capas limpia, desacoplada y orientada al dominio (Clean Architecture).

---

## 1. Arquitectura de la Solución

La solución `GestionReservas.slnx` se divide en capas siguiendo los principios de Clean Architecture:

```
back-office-tfi/
│
├── docs/
│   ├── ENUNCIADO.md                    # Requisitos, alcance y reglas de negocio
│   ├── ENDPOINTS.md                    # Contrato detallado de endpoints HTTP
│   ├── Modelado_de_datos_-_DER.pdf     # Documento DER del sistema
│   └── Peticiones_HTTP.pdf             # Matriz original de endpoints
│
├── src/
│   ├── GestionReservas.Domain/         # Entidades, Enums, Reglas de dominio puro (sin dependencias externas)
│   │   ├── Constants/                  # PermisosDefinidos (catálogo central de 14 permisos)
│   │   ├── Entities/                   # Usuario, Rol, Permiso, Departamento, TipoEspacio, Espacio,
│   │   │                               # Equipamiento, EspacioEquipamiento, Reserva, ReservaEquipamiento,
│   │   │                               # Bloqueo, Notificacion, Auditoria, CicloLectivo, Materia,
│   │   │                               # Cursada, Comision, AsignacionEspacio, HistorialMatricula
│   │   └── Enums/                      # EstadoReserva, EstadoEspacio, TipoActividad
│   │
│   ├── GestionReservas.Application/    # Casos de uso, interfaces, DTOs, validaciones, excepciones
│   │   ├── Common/
│   │   │   ├── Exceptions/             # NotFoundException, ConflictException, ForbiddenException,
│   │   │   │                           # UnauthorizedException, ValidationException (mapeo RFC 7807)
│   │   │   ├── Helpers/                # DateTimeZoneHelper (normalización UTC y hora de la facultad)
│   │   │   └── Interfaces/             # IApplicationDbContext, IPasswordHasher, IJwtTokenService, ICurrentUserService
│   │   ├── Features/
│   │   │   ├── Auth/                   # DTOs, validadores y AuthService con auditoría
│   │   │   ├── Bloqueos/               # DTOs, validadores, IBloqueosService y BloqueosService
│   │   │   ├── Catalogos/              # DTOs, validadores, ICatalogosService y CatalogosService
│   │   │   ├── Espacios/               # DTOs, validadores, IEspaciosService, IDisponibilidadService, IEspacioAuthorizationService
│   │   │   ├── Notificaciones/         # DTOs, INotificacionesService y NotificacionesService
│   │   │   ├── Reservas/               # DTOs, validadores, ReservaStateMachine, IReservasService y ReservasService
│   │   │   └── Usuarios/               # DTOs, validadores, IUsuariosService y UsuariosService
│   │   └── DependencyInjection.cs      # Registro de servicios y FluentValidation
│   │
│   ├── GestionReservas.Infrastructure/ # Persistencia EF Core, Npgsql, JwtTokenService, CurrentUserService, Seeder
│   │   ├── Persistence/
│   │   │   ├── Configurations/         # Fluent API IEntityTypeConfiguration para cada entidad
│   │   │   ├── Migrations/             # Migraciones EF Core (InitialCreate, AddReservaExclusionConstraint)
│   │   │   ├── Seed/                   # DatabaseSeeder con datos iniciales (usuarios, espacios, cursadas)
│   │   │   └── ApplicationDbContext.cs # DbContext con soporte Npgsql / PostgreSQL y advisory locks
│   │   ├── Security/                   # JwtOptions, JwtTokenService, CurrentUserService
│   │   └── DependencyInjection.cs      # Configuración de DbContext, JWT Bearer y políticas dinámicas
│   │
│   └── GestionReservas.Api/            # ASP.NET Core Web API, Controllers, Middlewares, Swagger
│       ├── Controllers/
│       │   ├── AuthController.cs       # POST /api/auth/login, GET /api/auth/me
│       │   ├── BloqueosController.cs   # POST /api/bloqueos, GET /api/bloqueos, DELETE /api/bloqueos/{id}
│       │   ├── DepartamentosController.cs # GET /api/departamentos
│       │   ├── EquipamientosController.cs # GET /api/equipamientos, POST /api/equipamientos
│       │   ├── EspaciosController.cs   # GET, POST, PUT, DELETE /api/espacios, GET /api/espacios/disponibles
│       │   ├── HealthController.cs     # GET /health
│       │   ├── NotificacionesController.cs # GET /api/notificaciones, PATCH /api/notificaciones/{id}/leer
│       │   ├── ReservasController.cs   # POST, GET /api/reservas, mis-reservas, pendientes, canceladas, rechazadas, PATCH, PUT
│       │   ├── RolesController.cs      # GET /api/roles
│       │   ├── TiposEspacioController.cs # GET /api/tipos-espacio, POST /api/tipos-espacio
│       │   └── UsuariosController.cs   # GET, POST, PATCH /api/usuarios
│       ├── Middlewares/                # ExceptionHandlingMiddleware (ProblemDetails RFC 7807)
│       ├── Program.cs                  # Pipeline HTTP, Swagger UI con Bearer, inicialización
│       ├── appsettings.json            # Conexión PostgreSQL y sección Jwt
│       └── Properties/launchSettings.json # Configuración de puertos: http://localhost:5051
│
├── tests/
│   └── GestionReservas.Tests/          # Proyecto de pruebas de integración con xUnit (53 tests)
│       ├── Infrastructure/             # CustomWebApplicationFactory (PostgreSQL GestionReservasDb_Test)
│       ├── AuthEndpointsTests.cs       # 8 tests de autenticación, JWT y perfil
│       ├── UsuariosAndCatalogosEndpointsTests.cs # 11 tests de usuarios, passwords, permisos y catálogos
│       ├── EspaciosEndpointsTests.cs   # 10 tests de CRUD de espacios, disponibilidad y autorización
│       ├── ReservasEndpointsTests.cs   # 10 tests de reservas, máquina de estados, validaciones y cancelaciones
│       ├── BloqueosEndpointsTests.cs   # 9 tests de bloqueos, reservas afectadas y levantamiento
│       └── ConsolidacionAjustesEndpointsTests.cs # 5 tests de ocupación académica, roles, altas y reservas admin
│
├── dotnet-tools.json                   # Manifiesto local de herramientas .NET (dotnet-ef 8.0.11)
├── GestionReservas.slnx                # Archivo de solución .NET
└── README.md                           # Documentación técnica del proyecto
```

---

## 2. Tecnologías y Stack

- **Framework**: .NET 8 (C# 12) con soporte de `RollForward=Major` para compatibilidad en entornos con SDK .NET 10.
- **API**: ASP.NET Core Web API con Controllers.
- **ORM**: Entity Framework Core 8 (`Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.11).
- **Base de Datos**: PostgreSQL 18.
- **Seguridad**: Autenticación JWT Bearer con claims de rol y permisos, `ClockSkew = TimeSpan.Zero`.
- **Autorización**: Políticas dinámicas registradas automáticamente para cada permiso del sistema (`[Authorize(Policy = "...")]`).
- **Control de Concurrencia**:
  - **Optimista**: Token de concurrencia mapeado al campo de sistema `xmin` de PostgreSQL en `Reserva`.
  - **Pesimista / Bloqueo Consultivo**: `pg_advisory_xact_lock` con hash de `EspacioId` para operaciones atómicas de reserva y bloqueo con cancelación de afectadas.
  - **Física en BD**: Restricción de exclusión mediante extensión `btree_gist` (`tsrange`) para evitar colisiones horarias físicas en reservas aprobadas.
- **Validación**: FluentValidation 11.9.2 con inyección automática en el pipeline.
- **Manejo de Errores**: Middleware global RFC 7807 ProblemDetails (400, 401, 403, 404, 409, 500).
- **Documentación de API**: Swagger / Swashbuckle 6.6.2 con comentarios XML en español y botón "Authorize" (Bearer).
- **Pruebas Automatizadas**: xUnit 2.5 + `Microsoft.AspNetCore.Mvc.Testing` contra PostgreSQL (`GestionReservasDb_Test`).

---

## 3. Configuración de Seguridad y User Secrets

La clave criptográfica JWT **no se encuentra hardcodeada ni commiteada** en `appsettings.json`. En su lugar, se administra mediante **User Secrets** en desarrollo:

```bash
dotnet user-secrets set "Jwt:Key" "ClaveSuperSecretaExtremadamenteLargaParaJwtGestionDeEspacios2026!#" --project src/GestionReservas.Api
```

Si no se configura User Secrets, el sistema cuenta con un fallback seguro para entornos de desarrollo local.

---

## 4. Usuarios de Prueba Sembrados (Seed)

| Rol | Nombre Completo | Correo Electrónico | Contraseña (Plano) | Permisos |
| :--- | :--- | :--- | :--- | :--- |
| **Administrador** | Administrador General | `admin@facultad.edu.ar` | `Admin123!` | Todos (14 permisos) |
| **SecretariaAcademica** | Secretaría Académica | `secretaria@facultad.edu.ar` | `Secretaria123!` | Espacios, Bloqueos, Reservas, Catálogos |
| **ResponsableEspacio** | Ing. Carlos Gutiérrez (Responsable Sistemas) | `responsable.sistemas@facultad.edu.ar` | `Responsable123!` | Espacios, Reservas, Bloqueos, Catálogos |
| **Solicitante** | Prof. Martín Pérez (Docente Solicitante) | `docente.perez@facultad.edu.ar` | `Docente123!` | Solicitar reservas, ver propias, catálogos |

### Datos Académicos Sembrados (Ciclo Lectivo 2026)
- **Ciclo Lectivo**: Año 2026 (`2026-03-01` a `2026-11-30`, `Activo = true`).
- **Materias**: `SO-01` (Sistemas Operativos) y `AGA-01` (Álgebra y Geometría Analítica).
- **Comisiones**: `K2051` (Cursada SO-01) y `A1011` (Cursada AGA-01).
- **Asignaciones de Espacio Recurrentes**:
  - `AULA-101`: Martes de 18:00 a 21:00 hs (Comisión A1011).
  - `LAB-SIS-01`: Jueves de 14:00 a 17:00 hs (Comisión K2051).

---

## 5. Endpoints Implementados (Tareas 1 a 6 + Ajustes)

### 5.1 Autenticación y Perfil (`/api/auth`)
- `POST /api/auth/login`: Autenticación con email y contraseña, retorno de JWT con claims de roles y permisos.
- `GET /api/auth/me`: Consulta de datos, roles, permisos y departamento del usuario autenticado.

### 5.2 Catálogos del Sistema, Roles y Altas (`/api/departamentos`, `/api/equipamientos`, `/api/tipos-espacio`, `/api/roles`)
- `GET /api/departamentos`: Lista de departamentos y secretarías institucionales (`[Authorize]`).
- `GET /api/equipamientos`: Catálogo flexible de equipamientos tecnológicos (`[Authorize]`).
- `POST /api/equipamientos`: Alta administrativa de nuevo equipamiento con validación de nombre único y auditoría (`[Authorize(Roles = "Administrador")]`).
- `GET /api/tipos-espacio`: Clasificación funcional de espacios físicos (`[Authorize]`).
- `POST /api/tipos-espacio`: Alta administrativa de tipología de espacio físico con validación de unicidad y auditoría (`[Authorize(Roles = "Administrador")]`).
- `GET /api/roles`: Listado de roles disponibles con Id, Nombre y Descripción para selectores de frontend (`[Authorize]`).

### 5.3 Gestión de Usuarios (`/api/usuarios`)
- `POST /api/usuarios`: Alta de usuario con validación de política de contraseñas, email único y asignación de roles y departamento.
- `GET /api/usuarios`: Listado paginado con filtros por búsqueda de texto, rol, departamento y estado activo.
- `GET /api/usuarios/{id}`: Detalle completo de usuario.
- `PATCH /api/usuarios/{id}`: Modificación parcial con reglas de negocio (protección contra auto-desactivación de administradores y garantía de al menos un administrador activo).

### 5.4 Gestión de Espacios Físicos y Disponibilidad (`/api/espacios`)
- `GET /api/espacios`: Listado paginado de espacios físicos con filtros opcionales (búsqueda de texto, tipo, edificio, piso, sector, capacidad mínima, departamento responsable, estado y equipamientos obligatorios). Por defecto excluye bajas lógicas.
- `GET /api/espacios/{id}`: Detalle completo del espacio con dotación de equipamiento, estado y ubicación.
- `POST /api/espacios`: Alta de nuevo espacio físico con código alfanumérico único, capacidad, departamento y equipamiento.
- `PUT /api/espacios/{id}`: Modificación completa del espacio y reemplazo de su dotación de equipamiento.
- `DELETE /api/espacios/{id}`: Baja lógica (`FechaBaja`). Impide la baja si existen reservas futuras pendientes o aprobadas (HTTP 409).
- `GET /api/espacios/disponibles`: Búsqueda de espacios disponibles en una fecha y franja horaria determinadas con filtros de capacidad mínima y equipamientos requeridos.

### 5.5 Gestión de Reservas y Máquina de Estados (`/api/reservas`, `/api/mis-reservas`)
- `POST /api/reservas`: Solicitud de reserva con control de disponibilidad (reservas aprobadas, bloqueos y ocupación académica oficial `AsignacionEspacio`), capacidad y equipamiento. Auto-aprobada si el solicitante administra el departamento del espacio, Secretaría o Admin.
- `GET /api/reservas`: Listado administrativo general de reservas con paginación y filtros (`estado`, `espacioId`, `departamentoId`, `desde`, `hasta`) respetando alcance departamental (Admin y Secretaría ven todas; Responsable solo de su departamento).
- `GET /api/mis-reservas`: Reservas del usuario autenticado con filtros por estado y rango de fechas.
- `GET /api/reservas/pendientes`: Solicitudes pendientes de revisión (Responsable ve su departamento; Secretaría y Admin ven todas).
- `GET /api/reservas/canceladas` y `GET /api/reservas/rechazadas`: Reservas canceladas o rechazadas.
- `GET /api/reservas/{id}`: Detalle completo con historial de auditoría de transiciones de estado.
- `PATCH /api/reservas/{id}`: Aprobación, rechazo (con motivo obligatorio) o cancelación con máquina de estados (`ReservaStateMachine`).
- `PUT /api/reservas/{id}`: Modificación integral de reserva; si estaba aprobada vuelve a pendiente y se revalida.

### 5.6 Notificaciones Institucionales (`/api/notificaciones`)
- `GET /api/notificaciones`: Consulta paginada de notificaciones del usuario autenticado con filtro opcional de `soloNoLeidas`.
- `PATCH /api/notificaciones/{id}/leer`: Marcado individual de notificación como leída.

### 5.7 Gestión de Bloqueos de Espacios (`/api/bloqueos`)
- `POST /api/bloqueos`: Registro de inhabilitación temporal de un espacio físico con detección o cancelación atómica de reservas afectadas.
- `GET /api/bloqueos`: Listado paginado de bloqueos con filtros (`espacioId`, `desde`, `hasta`, `soloVigentes`) y alcance departamental.
- `DELETE /api/bloqueos/{id}`: Levantamiento del bloqueo con registro en auditoría.

---

## 6. Reglas de Negocio, Concurrencia y Persistencia Temporal

### Máquina de Estados de Reservas (`ReservaStateMachine`)
- Ciclo de vida controlado: `Solicitada` $\to$ `PendienteAprobacion` $\to$ `Aprobada` / `Rechazada` / `Cancelada`.
- **Transiciones válidas**:
  - `Solicitada` / `PendienteAprobacion` $\to$ `Aprobada` (requiere rol Admin, Secretaría o Responsable del espacio).
  - `Solicitada` / `PendienteAprobacion` $\to$ `Rechazada` (requiere motivo obligatorio de rechazo).
  - Cualquier estado previo $\to$ `Cancelada` (el solicitante solo cancela las suyas; si estaba aprobada, el motivo es obligatorio).
- Transiciones inválidas o fuera de regla arrojan **HTTP 400 Bad Request**.

### Control de Concurrencia y Anticolisión
1. **Detección Optimista**: El campo de sistema `xmin` de PostgreSQL en `Reserva` detecta modificaciones simultáneas.
2. **Bloqueo Pesimista Atómico**: En transacciones críticas (`POST /api/reservas` y `POST /api/bloqueos` con cancelación), se adquiere un candado consultivo `pg_advisory_xact_lock(hashtext('espacio_' || espacioId))` que serializa las solicitudes concurrentes sobre el mismo aula sin bloquear otros espacios.
3. **Restricción Física Exclude**: Mediante la extensión `btree_gist`, la base de datos rechaza inserciones directas que colisionen en `(EspacioId, tsrange(FechaHoraInicio, FechaHoraFin))` para reservas aprobadas.

### Regla de Reservas Afectadas por Bloqueo
Al intentar registrar un bloqueo sobre un espacio físico (`POST /api/bloqueos`):
1. **Detección de Afectaciones**: El sistema evalúa todas las reservas en estado `Aprobada` o `PendienteAprobacion` que se superpongan temporalmente con el intervalo solicitado ($inicio_A < fin_B \land fin_A > inicio_B$).
2. **Rechazo por defecto (`cancelarReservasAfectadas = false`)**: Si existen reservas afectadas y el flag es falso (comportamiento por defecto), la operación se aborta arrojando **HTTP 409 Conflict** con el listado detallado de reservas afectadas (`id`, `solicitante`, `fechas`, `estado`), impidiendo la creación del bloqueo hasta que se defina la acción.
3. **Cancelación Atómica (`cancelarReservasAfectadas = true`)**: Si el usuario confirma la cancelación, la creación del bloqueo y la cancelación de las reservas afectadas se ejecutan en **una única transacción de base de datos** serializada mediante `pg_advisory_xact_lock`. Cada reserva afectada pasa al estado `Cancelada` con el motivo `"Cancelada por bloqueo del espacio: {motivo}"`, se registra la auditoría individual de cada cancelación, se encola una notificación a cada solicitante afectado y se retorna **HTTP 201 Created** con el bloqueo y las reservas canceladas.
4. **Levantamiento de Bloqueo**: Al eliminar un bloqueo (`DELETE /api/bloqueos/{id}`), el espacio vuelve a estar disponible para el futuro. **Las reservas previamente canceladas NO se restauran automáticamente**, ya que su cancelación fue notificada y consolidada en la bitácora institucional.

### Ocupación Académica Oficial (`AsignacionEspacio`)
- Las asignaciones de cursadas regulares (sembradas para el Ciclo Lectivo 2026) representan ocupación oficial fija.
- `DisponibilidadService` valida día de la semana (`DiaSemana`) y franja horaria (`HoraInicio` a `HoraFin`) contra la fecha de la solicitud convertida a hora local de la facultad.
- Solicitudes de reservas o bloqueos en conflicto con una cursada oficial son rechazadas con **HTTP 409 Conflict**:
  `"El espacio se encuentra ocupado por la cursada oficial: {Materia} - Comisión {Codigo}."`.

### Política de Zonas Horarias y Persistencia Temporal
- **Zona Horaria de la Facultad**: `America/Argentina/Buenos_Aires` (UTC-3 / `Argentina Standard Time`).
- **Conversión Unificada**: Todo el cómputo y normalización de fechas se encuentra centralizado en `DateTimeZoneHelper`:
  - `DateTimeZoneHelper.ToUtc(fecha, hora)`: Transforma la fecha y hora de la facultad a `DateTime` UTC con `DateTimeKind.Utc`.
  - `DateTimeZoneHelper.ToFacultadLocal(utcDateTime)`: Convierte un instante UTC a la hora oficial de la facultad.
- **Persistencia en PostgreSQL**: Npgsql exige que todas las columnas de tipo `timestamptz` reciban instancias `DateTime` con `DateTimeKind.Utc`. Todas las comparaciones de superposición y filtros en base de datos se ejecutan en UTC.

### Autorización Departamental
- **Administrador** y **Secretaría Académica**: Tienen facultades para crear, editar, bloquear y consultar reservas de cualquier departamento institucional.
- **Responsable de Espacio**: Solo tiene facultades para administrar espacios y revisar solicitudes que pertenezcan a **su propio departamento** (`usuario.DepartamentoId`). Si intenta operar sobre otro departamento, la capa de aplicación arroja `ForbiddenException` (HTTP 403).
- **Solicitante**: Posee acceso para solicitar reservas, consultar disponibilidad y ver/cancelar exclusivamente sus propias reservas.

---

## 7. Instrucciones para Ejecutar y Probar

### 1. Iniciar el Backend
El puerto configurado en `launchSettings.json` es el **5051** (HTTP):

```powershell
dotnet run --project src/GestionReservas.Api
```

Una vez iniciado, la API estará escuchando en:
- **Swagger UI**: [http://localhost:5051/swagger](http://localhost:5051/swagger)
- **Health Check**: [http://localhost:5051/health](http://localhost:5051/health)

### 2. Probar en Swagger UI
1. Navegar a [http://localhost:5051/swagger](http://localhost:5051/swagger).
2. Desplegar `POST /api/auth/login` y presionar **Try it out**.
3. Ingresar las credenciales de prueba:
   ```json
   {
     "email": "admin@facultad.edu.ar",
     "password": "Admin123!"
   }
   ```
4. Ejecutar la petición y copiar el `token` de la respuesta.
5. Hacer clic en el botón verde **Authorize** (arriba a la derecha), escribir `Bearer <tu_token>` y presionar **Authorize**.
6. Probar los endpoints protegidos correspondientes a cada rol.

### 3. Ejecutar la Suite de Pruebas Automatizadas (xUnit)
La suite de pruebas en `tests/GestionReservas.Tests` valida exhaustivamente los **53 escenarios de integración** sobre una base de datos PostgreSQL de test aislada (`GestionReservasDb_Test`):

```powershell
dotnet test tests/GestionReservas.Tests/GestionReservas.Tests.csproj
```

**Módulos y suites evaluados**:
1. **Autenticación (`AuthEndpointsTests` - 8 tests)**:
   - Login válido (200 + JWT completo con roles y permisos), contraseña errónea (401), usuario inexistente (401), email inválido (400).
   - GET `/api/auth/me` con token válido (200), token vencido (401), sin token (401).
   - Auditoría de intentos de inicio de sesión exitosos y fallidos.

2. **Usuarios y Catálogos (`UsuariosAndCatalogosEndpointsTests` - 11 tests)**:
   - Alta de usuario por Admin con asignación de roles (201 Created con Location).
   - Unicidad de email insensible a mayúsculas/minúsculas (409 Conflict).
   - Rechazo de contraseñas débiles por FluentValidation (400 Bad Request).
   - Bloqueo de creación/edición por usuarios sin permiso (403 Forbidden) y anónimos (401 Unauthorized).
   - Listado paginado con filtros combinados.
   - Protección contra auto-desactivación de cuenta admin y garantía de último admin activo (409 Conflict).
   - Denegación de login para usuarios dados de baja (401 Unauthorized).
   - Auditoría de operaciones de usuario y lectura íntegra de catálogos sembrados.

3. **Espacios y Disponibilidad (`EspaciosEndpointsTests` - 10 tests)**:
   - CRUD completo de espacios por Admin (201 Created, 200 OK, 204 NoContent, 404 tras baja).
   - Control de códigos alfanuméricos duplicados (409 Conflict).
   - Autorización departamental (Responsable gestiona su departamento, 403 en otros).
   - Restricción de escritura a Solicitante (403 Forbidden).
   - Protección de baja lógica con reservas futuras pendientes o aprobadas (409 Conflict).
   - Búsqueda de disponibilidad con aforo y equipamiento requerido (semántica `ALL`).
   - Superposición horaria: franjas solapadas no disponibles, franjas contiguas disponibles.
   - Exclusión de espacios con bloqueos activos o en estado `FueraDeServicio`.

4. **Reservas y Concurrencia (`ReservasEndpointsTests` - 10 tests)**:
   - Solicitud de reserva válida con equipamiento (201 Created).
   - Auto-aprobación si el solicitante es Responsable del espacio o Admin.
   - Rechazo por aforo excedido (400 Bad Request).
   - Rechazo por equipamiento faltante en el espacio (400 Bad Request).
   - Rechazo por solapamiento con reservas aprobadas o bloqueos (409 Conflict).
   - Máquina de estados: aprobación, rechazo con motivo obligatorio y cancelación con motivo obligatorio para reservas aprobadas.
   - Listado de `GET /api/mis-reservas`, `GET /api/reservas/pendientes` y canceladas.
   - Consulta de detalle con trazabilidad de transiciones en auditoría.

5. **Bloqueos de Espacios (`BloqueosEndpointsTests` - 9 tests)**:
   - Creación de bloqueo sin reservas afectadas (201 Created).
   - Conflicto con reservas afectadas cuando `cancelarReservasAfectadas = false` (409 Conflict con listado detallado).
   - Cancelación atómica de reservas afectadas cuando `cancelarReservasAfectadas = true` (201 Created, reservas en estado `Cancelada`, notificaciones encoladas y auditoría generada).
   - Detección de solape entre bloqueos sobre el mismo espacio (409 Conflict).
   - Autorización departamental en bloqueos (Responsable limitado a su departamento, 403 en otros; Solicitante 403).
   - Listado paginado de bloqueos con filtro de vigencia.
   - Levantamiento de bloqueo (`DELETE /api/bloqueos/{id}`) y no reactivación automática de reservas previamente canceladas.

6. **Consolidación y Cursadas Oficiales (`ConsolidacionAjustesEndpointsTests` - 5 tests)**:
   - Colisión de reserva con cursada académica oficial recurrente `AsignacionEspacio` (409 Conflict con detalle de comisión).
   - Creación exitosa en horarios contiguos o días sin cursada oficial (201 Created).
   - Endpoint `GET /api/roles` retornando catálogo completo de roles del sistema.
   - Altas de catálogo por Admin (`POST /api/equipamientos`) y rechazo a docentes (403 Forbidden).
   - Listado administrativo `GET /api/reservas` con filtros y restricción 403 para rol Solicitante.
