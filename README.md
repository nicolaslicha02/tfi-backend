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

## 5. Endpoints Disponibles en Tarea 2

### Autenticación y Perfil (`/api/auth`)
- **`POST /api/auth/login`**:
  - Entrada: `{ "email": "admin@facultad.edu.ar", "password": "Admin123!" }`
  - Salida: Token JWT, fecha de expiración y datos del usuario (`id`, `nombreCompleto`, `email`, `roles`).
  - Validación con FluentValidation (formato de correo y longitud).
  - Mensaje genérico ante error (401: *"Credenciales inválidas."*).
  - Rechaza usuarios inactivos o dados de baja.
  - Registra eventos en la tabla `Auditorias` (`LoginExitoso` y `LoginFallido`).
- **`GET /api/auth/me`**:
  - Requiere cabecera `Authorization: Bearer <token>`.
  - Devuelve `id`, `nombreCompleto`, `email`, `roles`, `permisos` y `departamento`.
- **`GET /health`**:
  - Estado de salud y conectividad a la base de datos PostgreSQL.
- **`GET /swagger`**:
  - Interfaz interactiva de Swagger UI con botón **Authorize** para ingresar el Bearer token y probar los endpoints protegidos.

---

## 6. Instrucciones para Ejecutar y Probar

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
La suite de pruebas en `tests/GestionReservas.Tests` valida todos los escenarios solicitados:

```powershell
dotnet test tests/GestionReservas.Tests/GestionReservas.Tests.csproj
```

**Casos cubiertos**:
1. Login correcto (`admin@facultad.edu.ar` / `Admin123!`) devuelve HTTP 200 con JWT válido.
2. Login con contraseña incorrecta devuelve HTTP 401 con mensaje genérico.
3. Login con usuario inexistente devuelve HTTP 401 con mensaje genérico.
4. Login con email inválido devuelve HTTP 400 Bad Request (FluentValidation).
5. GET `/api/auth/me` sin token devuelve HTTP 401 Unauthorized.
6. GET `/api/auth/me` con token inválido/manipulado devuelve HTTP 401 Unauthorized.
7. GET `/api/auth/me` con token vencido devuelve HTTP 401 Unauthorized (`ClockSkew = TimeSpan.Zero`).
8. GET `/api/auth/me` con token válido devuelve HTTP 200 con perfil completo, roles y permisos.
