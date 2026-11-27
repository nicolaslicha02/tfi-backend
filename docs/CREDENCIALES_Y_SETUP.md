# Credenciales de Prueba, Setup del Entorno y Ejemplos JSON

Guía rápida para configurar, levantar y probar el backend del **Sistema de Gestión y Reservas de Espacios de la Facultad** durante el desarrollo o la defensa.

---

## 1. Los 4 Usuarios de Prueba Sembrados

| Rol | Email | Contraseña | Departamento | Uso Principal en la Demo |
| :--- | :--- | :--- | :--- | :--- |
| **Administrador** | `admin@facultad.edu.ar` | `Admin123!` | Global (`null`) | Acceso total: usuarios, roles, catálogos, espacios, reservas, bloqueos y auditoría. |
| **SecretariaAcademica** | `secretaria@facultad.edu.ar` | `Secretaria123!` | Global (`null`) | Gestión global de espacios, bloqueos, auditoría e intervención excepcional en reservas. |
| **ResponsableEspacio** | `responsable.sistemas@facultad.edu.ar` | `Responsable123!` | Depto. de Sistemas e Informática (`Id = 1`) | Administra espacios (`LAB-SIS-01`, `LAB-SIS-02`), reservas y bloqueos de Sistemas; recibe `403` en otros departamentos. |
| **Solicitante** | `docente.perez@facultad.edu.ar` | `Docente123!` | Depto. de Ciencias Básicas (`Id = 2`) | Solicita reservas, consulta disponibilidad, ve/modifica/cancela sus propias reservas y ve ajenas como `"Ocupado"` en el calendario. |

---

## 2. Cómo Levantar la API Paso a Paso

### Paso 1: Configurar User Secrets (Connection String con contraseña y Clave JWT)
Ejecutar desde la raíz del repositorio (`back-office-tfi`):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=GestionReservasDb;Username=postgres;Password=postgres" --project src/GestionReservas.Api
dotnet user-secrets set "Jwt:Key" "ClaveSuperSecretaExtremadamenteLargaParaJwtGestionDeEspacios2026!#" --project src/GestionReservas.Api
```

### Paso 2: Verificar `appsettings.Development.json` (CORS)
El archivo `src/GestionReservas.Api/appsettings.Development.json` define los orígenes permitidos para el frontend local mediante `Cors:AllowedOrigins` (incluyendo `http://localhost:5173`):

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173",
      "http://localhost:4200"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### Paso 3: Aplicar Migraciones y Levantar el Servidor
```powershell
dotnet ef database update --project src/GestionReservas.Infrastructure --startup-project src/GestionReservas.Api
dotnet run --project src/GestionReservas.Api
```
- **URL Base**: `http://localhost:5051`
- **Swagger UI**: `http://localhost:5051/swagger`
- **Health Check**: `http://localhost:5051/health`

---

## 3. Ejemplos de Respuesta JSON de los Endpoints Principales

### 3.1 `POST /api/auth/login` (`200 OK`)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-11-16T01:30:00Z",
  "usuario": {
    "id": 3,
    "nombreCompleto": "Ing. Carlos Gutiérrez (Responsable Sistemas)",
    "email": "responsable.sistemas@facultad.edu.ar",
    "roles": [
      "ResponsableEspacio"
    ]
  }
}
```

### 3.2 `GET /api/auth/me` (`200 OK`)
```json
{
  "id": 3,
  "nombreCompleto": "Ing. Carlos Gutiérrez (Responsable Sistemas)",
  "email": "responsable.sistemas@facultad.edu.ar",
  "telefono": "+54 11 4000-0003",
  "activo": true,
  "fechaCreacion": "2026-11-09T15:00:00Z",
  "roles": [
    "ResponsableEspacio"
  ],
  "permisos": [
    "espacios.ver",
    "espacios.crear",
    "espacios.editar",
    "espacios.eliminar",
    "reservas.solicitar",
    "reservas.ver_propias",
    "reservas.ver_pendientes",
    "reservas.aprobar_rechazar",
    "reservas.cancelar",
    "reservas.modificar",
    "bloqueos.ver",
    "bloqueos.crear",
    "catalogos.ver"
  ],
  "departamento": {
    "id": 1,
    "nombre": "Departamento de Sistemas e Informática",
    "descripcion": "Gestión de laboratorios de computación, redes e infraestructura informática."
  }
}
```

### 3.3 `GET /api/espacios/disponibles?fecha=2026-11-25&horaInicio=18:00&horaFin=21:00&capacidadMinima=60&equipamientoIds=1` (`200 OK`)
```json
[
  {
    "id": 1,
    "codigo": "AULA-101",
    "nombre": "Aula Magna 101",
    "tipoEspacioId": 1,
    "tipoEspacioNombre": "Auditorio",
    "edificio": "Edificio Central",
    "piso": 1,
    "sector": "Ala Norte",
    "capacidadMaxima": 120,
    "departamentoId": 3,
    "departamentoNombre": "Secretaría Académica",
    "estado": "Disponible",
    "esBaja": false,
    "equipamientos": [
      {
        "equipamientoId": 1,
        "nombre": "Proyector Láser Full HD",
        "cantidad": 1,
        "especificacion": "4K Láser"
      }
    ]
  }
]
```

### 3.4 `GET /api/reservas/{id}` (`200 OK`)
```json
{
  "id": 15,
  "espacioId": 4,
  "espacioCodigo": "LAB-SIS-01",
  "espacioNombre": "Laboratorio de Redes y Software",
  "departamentoId": 1,
  "departamentoNombre": "Departamento de Sistemas e Informática",
  "solicitanteId": 4,
  "solicitanteNombre": "Prof. Martín Pérez (Docente Solicitante)",
  "solicitanteEmail": "docente.perez@facultad.edu.ar",
  "fechaHoraInicio": "2026-11-25T13:00:00Z",
  "fechaHoraFin": "2026-11-25T15:00:00Z",
  "tipoActividad": "Clase",
  "asistentesEstimados": 25,
  "motivo": "Clase práctica de laboratorio",
  "observaciones": "Se requiere entorno Linux",
  "estado": "Aprobada",
  "motivoRechazo": null,
  "fechaSolicitud": "2026-11-15T18:00:00Z",
  "fechaResolucion": "2026-11-15T18:05:00Z",
  "resueltoPorUsuarioId": 3,
  "resueltoPorNombre": "Ing. Carlos Gutiérrez (Responsable Sistemas)",
  "equipamientoRequerido": [
    {
      "equipamientoId": 1,
      "nombre": "Proyector Láser Full HD",
      "cantidad": 1,
      "observaciones": null
    }
  ],
  "historialCambios": [
    {
      "id": 101,
      "fechaHora": "2026-11-15T18:00:00Z",
      "accion": "SolicitarReserva",
      "usuarioId": 4,
      "usuarioNombre": "Prof. Martín Pérez (Docente Solicitante)",
      "motivo": "Clase práctica de laboratorio",
      "detalle": "Solicitud de reserva creada en espacio 'LAB-SIS-01' (2026-11-25 13:00 a 2026-11-25 15:00 UTC). Estado inicial: PendienteAprobacion."
    },
    {
      "id": 102,
      "fechaHora": "2026-11-15T18:05:00Z",
      "accion": "AprobarReserva",
      "usuarioId": 3,
      "usuarioNombre": "Ing. Carlos Gutiérrez (Responsable Sistemas)",
      "motivo": null,
      "detalle": "Transición de estado de 'PendienteAprobacion' a 'Aprobada' en espacio 'LAB-SIS-01'."
    }
  ]
}
```

### 3.5 `POST /api/bloqueos` con `cancelarReservasAfectadas = true` (`201 Created`)
El cliente primero recibe un `409` con `reservasAfectadas`. Debe mostrar esa lista y reenviar únicamente las reservas que el usuario confirmó, junto con cada versión. Si hay una reserva nueva, una reserva sale del rango o una reserva cambia desde que se mostró, el servidor devuelve `409` con la lista actualizada y no modifica datos.

```json
{
  "espacioId": 4,
  "desde": "2026-11-25T12:00:00Z",
  "hasta": "2026-11-25T18:00:00Z",
  "motivo": "Reparación eléctrica de urgencia",
  "cancelarReservasAfectadas": true,
  "reservasConfirmadas": [15],
  "versionesReservasConfirmadas": [{ "id": 15, "version": 8241 }]
}
```

`reservasConfirmadas` y `versionesReservasConfirmadas` se copian de `reservasAfectadas` en el `409` previo. IDs repetidos se interpretan como conjunto; todos deben ser positivos. `true` sin lista solo crea el bloqueo cuando no hay reservas afectadas.

```json
{
  "id": 8,
  "espacioId": 4,
  "espacioCodigo": "LAB-SIS-01",
  "espacioNombre": "Laboratorio de Redes y Software",
  "fechaHoraDesde": "2026-11-25T12:00:00Z",
  "fechaHoraHasta": "2026-11-25T18:00:00Z",
  "motivo": "Reparación eléctrica de urgencia",
  "creadoPorUsuarioId": 1,
  "creadoPorUsuarioNombre": "Administrador General",
  "fechaCreacion": "2026-11-15T19:00:00Z",
  "reservasCanceladas": [
    {
      "id": 15,
      "solicitanteId": 4,
      "solicitante": "Prof. Martín Pérez (Docente Solicitante)",
      "solicitanteEmail": "docente.perez@facultad.edu.ar",
      "fechaHoraInicio": "2026-11-25T13:00:00Z",
      "fechaHoraFin": "2026-11-25T15:00:00Z",
      "estado": "Cancelada",
      "version": 8242
    }
  ]
}
```

Un conflicto por lista obsoleta devuelve `reservasAfectadas` actualizado, incluso cuando está vacío:

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Conflicto con el estado actual del recurso",
  "status": 409,
  "detail": "La lista de reservas cambió o alguna reserva fue actualizada. Revisá la lista y confirmá nuevamente.",
  "instance": "/api/bloqueos",
  "reservasAfectadas": []
}
```

### 3.6 `GET /api/calendario` (`200 OK` visto por un `Solicitante`)
```json
[
  {
    "tipo": "Reserva",
    "referenciaId": 12,
    "espacioId": 1,
    "espacioCodigo": "AULA-101",
    "espacioNombre": "Aula Magna 101",
    "edificio": "Edificio Central",
    "sector": "Ala Norte",
    "departamentoId": 3,
    "departamento": "Secretaría Académica",
    "inicio": "2026-05-04T12:00:00Z",
    "fin": "2026-05-04T14:00:00Z",
    "estado": "Aprobada",
    "titulo": "Ocupado",
    "solicitanteId": null,
    "solicitante": null,
    "tipoActividad": null,
    "motivo": null,
    "observaciones": null
  },
  {
    "tipo": "Cursada",
    "referenciaId": 1,
    "espacioId": 1,
    "espacioCodigo": "AULA-101",
    "espacioNombre": "Aula Magna 101",
    "edificio": "Edificio Central",
    "sector": "Ala Norte",
    "departamentoId": 3,
    "departamento": "Secretaría Académica",
    "inicio": "2026-05-05T21:00:00Z",
    "fin": "2026-05-06T00:00:00Z",
    "estado": null,
    "titulo": "Álgebra y Geometría Analítica - Comisión A1011",
    "solicitanteId": null,
    "solicitante": null,
    "tipoActividad": null,
    "motivo": null,
    "observaciones": null
  }
]
```

### 3.7 `GET /api/auditoria?page=1&pageSize=20` (`200 OK`)
```json
{
  "items": [
    {
      "id": 105,
      "usuarioId": 1,
      "usuarioEmail": "admin@facultad.edu.ar",
      "usuarioNombre": "Administrador General",
      "rol": "Administrador",
      "fechaHora": "2026-11-15T19:00:00Z",
      "accion": "CrearBloqueo",
      "entidad": "Bloqueo",
      "entidadId": "8",
      "motivo": "Reparación eléctrica de urgencia",
      "detalle": "Bloqueo creado sobre espacio 'LAB-SIS-01'. Reservas afectadas canceladas: 1."
    }
  ],
  "total": 105,
  "page": 1,
  "pageSize": 20
}
```
