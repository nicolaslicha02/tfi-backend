# Sistema de Gestión y Reservas de Espacios de la Facultad - Backend

Backend desarrollado en **.NET 8** y **ASP.NET Core Web API** con **Entity Framework Core 8** y **PostgreSQL**, diseñado bajo una arquitectura en capas limpia, desacoplada y orientada al dominio.

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
│   │   │                               # Bloqueo, Notificacion, Auditoria, CicloLectivo, Materia, etc.
│   │   └── Enums/                      # EstadoReserva, EstadoEspacio, TipoActividad
│   │
│   ├── GestionReservas.Application/    # Casos de uso, interfaces, DTOs, validaciones, excepciones
│   │   ├── Common/
│   │   │   ├── Exceptions/             # NotFoundException, ConflictException, ForbiddenException,
│   │   │   │                           # UnauthorizedException, ValidationException (mapeo RFC 7807)
│   │   │   └── Interfaces/             # IApplicationDbContext, IPasswordHasher, IJwtTokenService, ICurrentUserService
│   │   ├── Features/Auth/              # DTOs de login y perfil, LoginRequestValidator (FluentValidation),
│   │   │                               # IAuthService y AuthService con auditoría
│   │   ├── Services/                   # PasswordHasher (PBKDF2 HMAC-SHA256 RFC 2898)
│   │   └── DependencyInjection.cs      # Registro de dependencias de Application
│   │
│   ├── GestionReservas.Infrastructure/ # Persistencia EF Core, Npgsql, JwtTokenService, CurrentUserService, Seeder
│   │   ├── Persistence/
│   │   │   ├── Configurations/         # Fluent API IEntityTypeConfiguration para cada entidad
│   │   │   ├── Migrations/             # Migraciones EF Core (InitialCreate)
│   │   │   ├── Seed/                   # DatabaseSeeder con datos iniciales
│   │   │   └── ApplicationDbContext.cs # DbContext con soporte Npgsql / PostgreSQL
│   │   ├── Security/                   # JwtOptions, JwtTokenService, CurrentUserService
│   │   └── DependencyInjection.cs      # Configuración de DbContext, JWT Bearer y políticas dinámicas
│   │
│   └── GestionReservas.Api/            # ASP.NET Core Web API, Controllers, Middlewares, Swagger
│       ├── Controllers/
│       │   ├── AuthController.cs       # POST /api/auth/login, GET /api/auth/me
│       │   └── HealthController.cs     # GET /health
│       ├── Middlewares/                # ExceptionHandlingMiddleware (ProblemDetails RFC 7807)
│       ├── Program.cs                  # Pipeline HTTP, Swagger UI con Bearer, inicialización
│       ├── appsettings.json            # Conexión PostgreSQL y sección Jwt (placeholder)
│       └── Properties/launchSettings.json # Configuración de puertos: http://localhost:5051
│
├── tests/
│   └── GestionReservas.Tests/          # Proyecto de pruebas de integración con xUnit
│       ├── Infrastructure/             # CustomWebApplicationFactory (PostgreSQL)
│       └── AuthEndpointsTests.cs       # Tests de login, credenciales erróneas, /me y tokens
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
- **Validación**: FluentValidation 11.9.2 con inyección automática en el pipeline.
- **Manejo de Errores**: Middleware global RFC 7807 ProblemDetails (400, 401, 403, 404, 409, 500).
- **Documentación de API**: Swagger / Swashbuckle 6.6.2 con comentarios XML en español y botón "Authorize" (Bearer).
- **Pruebas Automatizadas**: xUnit 2.5 + `Microsoft.AspNetCore.Mvc.Testing` contra PostgreSQL.

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

---

## 5. Endpoints Implementados (Tareas 2, 3 y 4)

### Autenticación y Perfil (`/api/auth`)
- `POST /api/auth/login`: Autenticación con email y contraseña, retorno de JWT con claims de roles y permisos.
- `GET /api/auth/me`: Consulta de datos, roles, permisos y departamento del usuario autenticado.

### Catálogos del Sistema (`/api/departamentos`, `/api/equipamientos`, `/api/tipos-espacio`)
- `GET /api/departamentos`: Lista de departamentos y secretarías institucionales (`[Authorize]`).
- `GET /api/equipamientos`: Catálogo flexible de equipamientos tecnológicos (`[Authorize]`).
- `GET /api/tipos-espacio`: Clasificación funcional de espacios físicos (`[Authorize]`).

### Gestión de Usuarios (`/api/usuarios`)
- `POST /api/usuarios`: Alta de usuario con validación de política de contraseñas, email único y asignación de roles y departamento.
- `GET /api/usuarios`: Listado paginado con filtros por búsqueda de texto, rol, departamento y estado activo.
- `GET /api/usuarios/{id}`: Detalle completo de usuario.
- `PATCH /api/usuarios/{id}`: Modificación parcial con reglas de negocio (protección contra auto-desactivación de administradores y garantía de al menos un administrador activo).

### Gestión de Espacios Físicos (`/api/espacios`)
- `GET /api/espacios`: Listado paginado de espacios físicos con filtros opcionales (búsqueda de texto, tipo, edificio, piso, sector, capacidad mínima, departamento responsable, estado y equipamientos obligatorios). Por defecto excluye espacios dados de baja.
- `GET /api/espacios/{id}`: Detalle completo del espacio con dotación de equipamiento, estado y ubicación.
- `POST /api/espacios`: Alta de nuevo espacio físico con código alfanumérico único, capacidad, departamento y equipamiento.
- `PUT /api/espacios/{id}`: Modificación completa del espacio y reemplazo de su dotación de equipamiento.
- `DELETE /api/espacios/{id}`: Baja lógica (`FechaBaja`). Impide la baja si existen reservas futuras pendientes o aprobadas (HTTP 409).
- `GET /api/espacios/disponibles`: Búsqueda de espacios disponibles en una fecha y franja horaria determinadas con filtros de capacidad mínima y equipamientos requeridos.

### Gestión de Reservas y Notificaciones (`/api/reservas`, `/api/mis-reservas`, `/api/notificaciones`)
- `POST /api/reservas`: Solicitud de reserva con control de disponibilidad, capacidad, equipamiento y bloqueos. Auto-aprobada si el solicitante es Responsable del departamento, Secretaría o Administrador.
- `GET /api/mis-reservas`: Reservas del usuario autenticado con filtros por estado y rango de fechas.
- `GET /api/reservas/pendientes`: Solicitudes pendientes de revisión (Responsable ve su departamento; Secretaría y Admin ven todas).
- `GET /api/reservas/canceladas` y `GET /api/reservas/rechazadas`: Reservas canceladas o rechazadas.
- `GET /api/reservas/{id}`: Detalle completo con historial de auditoría de transiciones de estado.
- `PATCH /api/reservas/{id}`: Aprobación, rechazo (con motivo obligatorio) o cancelación con máquina de estados (`ReservaStateMachine`).
- `PUT /api/reservas/{id}`: Modificación de reserva; si estaba aprobada vuelve a pendiente y se revalida.
- `GET /api/notificaciones` y `PATCH /api/notificaciones/{id}/leer`: Consulta y marcado de notificaciones del usuario.

### Gestión de Bloqueos de Espacios (`/api/bloqueos`)
- `POST /api/bloqueos`: Registro de inhabilitación temporal de un espacio físico por obras, reparaciones o actos institucionales.
- `GET /api/bloqueos`: Listado paginado de bloqueos con filtros (espacioId, desde, hasta, vigentes) y alcance departamental.
- `DELETE /api/bloqueos/{id}`: Levantamiento del bloqueo con registro en auditoría.

---

## 6. Reglas de Negocio y Manejo de Fechas

### Regla de Reservas Afectadas por Bloqueo
Al intentar registrar un bloqueo sobre un espacio físico (`POST /api/bloqueos`):
1. **Detección de Afectaciones**: El sistema evalúa todas las reservas en estado `Aprobada` o `PendienteAprobacion` que se superpongan temporalmente con el intervalo solicitado ($inicio_A < fin_B \land fin_A > inicio_B$).
2. **Rechazo por defecto (`cancelarReservasAfectadas = false`)**: Si existen reservas afectadas y el flag es falso (comportamiento por defecto), la operación se aborta arrojando **HTTP 409 Conflict** con el listado detallado de reservas afectadas (`id`, `solicitante`, `fechas`, `estado`), impidiendo la creación del bloqueo hasta que se defina la acción.
3. **Cancelación Atómica (`cancelarReservasAfectadas = true`)**: Si el usuario confirma la cancelación, la creación del bloqueo y la cancelación de las reservas afectadas se ejecutan en **una única transacción de base de datos** serializada mediante un bloqueo consultivo (`pg_advisory_xact_lock`) por espacio. Cada reserva afectada pasa al estado `Cancelada` con el motivo `"Cancelada por bloqueo del espacio: {motivo}"`, se registra la auditoría individual de cada cancelación, se encola una notificación a cada solicitante afectado y se retorna **HTTP 201 Created** con el bloqueo y las reservas canceladas.
4. **Levantamiento de Bloqueo**: Al eliminar un bloqueo (`DELETE /api/bloqueos/{id}`), el espacio vuelve a estar disponible para el futuro. **Las reservas previamente canceladas NO se restauran automáticamente**, ya que su cancelación fue notificada y consolidada en la bitácora institucional.

### Política de Zonas Horarias y Persistencia Temporal
- **Zona Horaria de la Facultad**: `America/Argentina/Buenos_Aires` (UTC-3 / `Argentina Standard Time`).
- **Conversión Unificada**: Todo el cómputo y normalización de fechas se encuentra centralizado en `DateTimeZoneHelper`:
  - `DateTimeZoneHelper.ToUtc(fecha, hora)`: Transforma la fecha y hora de la facultad a `DateTime` UTC con `DateTimeKind.Utc`.
  - `DateTimeZoneHelper.ToFacultadLocal(utcDateTime)`: Convierte un instante UTC a la hora oficial de la facultad.
- **Persistencia en PostgreSQL**: Npgsql exige que todas las columnas de tipo `timestamptz` reciban instancias `DateTime` con `DateTimeKind.Utc`. Todas las comparaciones de superposición y filtros en base de datos se ejecutan en UTC.

### Autorización Departamental
- **Administrador** y **Secretaría Académica**: Tienen facultades para crear, editar y dar de baja espacios de cualquier departamento institucional.
- **Responsable de Espacio**: Solo tiene facultades para administrar espacios que pertenezcan a **su propio departamento** (`usuario.DepartamentoId`). Si intenta operar sobre otro departamento, la capa de aplicación arroja `ForbiddenException` (HTTP 403).
- **Solicitante**: Posee acceso de solo lectura para listar y consultar espacios y disponibilidad.

### Algoritmo de Disponibilidad y Superposición
- El servicio `IDisponibilidadService` resuelve el cálculo íntegramente a nivel de base de datos vía consultas SQL / EF Core sin traer registros a memoria.
- **Criterio de Superposición**: Dos intervalos $[A_{inicio}, A_{fin})$ y $[B_{inicio}, B_{fin})$ se superponen si y solo si:
  $$A_{inicio} < B_{fin} \quad \land \quad A_{fin} > B_{inicio}$$
- **Intervalos Contiguos**: Reservas o bloqueos pegados (por ejemplo, 10:00 a 12:00 y 12:00 a 14:00) **no se consideran superpuestos**, permitiendo el uso inmediato posterior del espacio.
- Un espacio se considera **no disponible** si:
  1. Fue dado de baja lógicamente (`FechaBaja != null`).
  2. Su estado operativo es `EstadoEspacio.FueraDeServicio`.
  3. Posee un `Bloqueo` que se superpone con el intervalo consultado.
  4. Posee una `Reserva` en estado `Aprobada` que se superpone con el intervalo consultado.

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
2. Desplegar `POST /api/Auth/login` y presionar **Try it out**.
3. Ingresar las credenciales de prueba:
   ```json
   {
     "email": "admin@facultad.edu.ar",
     "password": "Admin123!"
   }
   ```
4. Ejecutar la petición y copiar el `token` de la respuesta.
5. Hacer clic en el botón verde **Authorize** (arriba a la derecha), escribir `Bearer <tu_token>` y presionar **Authorize**.
6. Probar el endpoint protegido `GET /api/Auth/me` para verificar los datos, roles y permisos del usuario autenticado.

### 3. Ejecutar la Suite de Pruebas Automatizadas (xUnit)
La suite de pruebas en `tests/GestionReservas.Tests` valida exhaustivamente los 29 escenarios de integración sobre una base de datos PostgreSQL de test aislada (`GestionReservasDb_Test`):

```powershell
dotnet test tests/GestionReservas.Tests/GestionReservas.Tests.csproj
```

**Módulos evaluados**:
1. **Autenticación (AuthEndpointsTests - 8 tests)**:
   - Login con credenciales válidas (200 + token JWT con claims completos).
   - Login con contraseña incorrecta (401 con mensaje genérico).
   - Login con usuario inexistente (401 con mensaje genérico).
   - Login con email inválido (400 Bad Request por FluentValidation).
   - GET `/api/auth/me` con token válido (200 con perfil, roles, permisos y departamento).
   - GET `/api/auth/me` con token vencido (401 Unauthorized sin margen de tolerancia).
   - GET `/api/auth/me` sin token (401 Unauthorized).
   - Registro en bitácora de auditoría de intentos exitosos y fallidos.

2. **Usuarios y Catálogos (UsuariosAndCatalogosEndpointsTests - 11 tests)**:
   - Alta de usuario por Administrador con asignación de roles (201 Created con Location).
   - Control de unicidad de email insensible a mayúsculas/minúsculas (409 Conflict).
   - Rechazo de contraseñas débiles que no cumplen la política de seguridad (400 Bad Request).
   - Bloqueo de creación y modificación por usuarios sin permiso como Solicitante (403 Forbidden).
   - Denegación de acceso anónimo sin token (401 Unauthorized).
   - Listado paginado de usuarios con filtros por texto, rol y departamento.
   - Restricción de auto-desactivación de la propia cuenta de administrador (409 Conflict).
   - Imposibilidad de iniciar sesión para usuarios dados de baja o desactivados (401 Unauthorized).
   - Registro en tabla `Auditorias` de operaciones de usuario sin comprometer contraseñas.
   - Retorno íntegro de catálogos institucionales sembrados (departamentos, equipamientos y tipos de espacio).

3. **Espacios y Disponibilidad (EspaciosEndpointsTests - 10 tests)**:
   - Flujo integral de CRUD de espacios por Administrador (201 Created, 200 OK, 204 NoContent, 404 NotFound tras baja).
   - Rechazo de códigos alfanuméricos duplicados incluso contra espacios dados de baja (409 Conflict).
   - Autorización departamental: Responsable gestiona su departamento asignado (200/201) y recibe 403 Forbidden al intentar gestionar otros departamentos.
   - Restricción de operaciones de escritura para Solicitante (403 Forbidden).
   - Protección de integridad referencial en baja lógica: rechazo de baja si existen reservas futuras aprobadas o pendientes (409 Conflict).
   - Búsqueda de disponibilidad con aforo mínimo y equipamiento requerido (ej. 60 personas + proyector).
   - Comprobación de superposición temporal de reservas aprobadas: reserva 10:00-12:00 no disponible para consulta 11:00-13:00 pero sí disponible para consulta contigua 12:00-14:00.
   - Exclusión de espacios con bloqueos institucionales activos o en estado `FueraDeServicio`.
   - Paginación y filtros combinados por búsqueda de texto y tipología.
