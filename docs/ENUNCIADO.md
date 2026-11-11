# Sistema de Gestión y Reservas de Espacios de la Facultad

## 1. Introducción y Contexto del Negocio
El sistema permite a la Facultad gestionar eficientemente sus espacios físicos (aulas, laboratorios, auditorios, salas de reunión), administrando tanto la ocupación académica regular recurrente como las solicitudes de reservas eventuales (clases de consulta, exámenes especiales, reuniones, actos institucionales), los bloqueos por mantenimiento o reparaciones, el equipamiento tecnológico disponible y la trazabilidad/auditoría de cada acción.

---

## 2. Módulos del Sistema y Reglas de Negocio

### 2.1 Módulo Académico y Ocupación Oficial
Centraliza la información del calendario académico y los horarios fijos de cursadas.
- **Entidades**: `CicloLectivo`, `Materia`, `Cursada`, `Comision`, `AsignacionEspacio`.
- **Regla**: La secretaría académica carga o clona las asignaciones oficiales recurrentes (ej. martes de 18:00 a 21:00 hs). Estas asignaciones representan ocupación oficial fija del aula.

### 2.2 Módulo de Gestión de Aulas y Mantenimiento (Espacios)
Administra la infraestructura física, sus condiciones y equipamiento.
- **Entidades**: `TipoEspacio`, `Espacio`, `Departamento`, `Equipamiento`, `EspacioEquipamiento`, `Bloqueo`.
- **Reglas**:
  - Cada espacio pertenece a un `Departamento` responsable.
  - El espacio posee código único institucional, ubicación (edificio, piso, sector), capacidad máxima y estado (`Disponible`, `FueraDeServicio`).
  - Baja lógica mediante `FechaBaja`.
  - Capacidad máxima estrictamente mayor a 0 (`capacidad > 0`).
  - Catálogo flexible de equipamiento mediante tabla intermedia `EspacioEquipamiento` con cantidad y valor opcional (NO columnas booleanas rígidas como `TieneProyector`).
  - **Bloqueos**: Permite inhabilitar aulas temporalmente por mantenimiento, pintura o eventos institucionales mayores. Si un espacio tiene un bloqueo en un rango de fechas y horas, se considera no disponible para reservas.
  - Validación de rango temporal: `fin > inicio` o `hasta > desde`.

### 2.3 Módulo de Reservas Eventuales
Gestiona solicitudes puntuales de uso de espacios (clases de consulta, defensas de tesis, exámenes libres, eventos).
- **Entidades**: `Reserva`, `ReservaEquipamiento`.
- **Reglas de Negocio**:
  - Toda reserva requiere: `SolicitanteId`, `EspacioId`, `FechaHoraInicio`, `FechaHoraFin`, `TipoActividad`, `AsistentesEstimados`, `Motivo`, `Estado`.
  - Equipamientos requeridos mediante `ReservaEquipamiento`.
  - `FechaHoraFin > FechaHoraInicio`.
  - `AsistentesEstimados > 0` y no puede superar la capacidad máxima del aula (con validación de negocio).
  - **Resolución de Conflictos**: Al solicitar o aprobar una reserva, el sistema debe verificar que no existan colisiones con:
    1. Otras reservas aprobadas en el mismo espacio y solape horario.
    2. Bloqueos vigentes en el mismo espacio y solape horario.
    3. Asignaciones académicas fijas en el mismo día/horario.
  - Concurrencia optimista implementada mediante `RowVersion` / token de concurrencia.
  - Ciclo de vida de la reserva: `Solicitada` -> `PendienteAprobacion` -> `Aprobada` / `Rechazada` / `Cancelada`.
  - En caso de rechazo o cancelación, se debe registrar el usuario responsable, fecha de resolución y motivo.

### 2.4 Módulo de Evolución de Matrícula
Optimización de espacios físicos según la cantidad de alumnos reales.
- **Entidades**: `HistorialMatricula`.
- **Regla**: Permite registrar a lo largo del cuatrimestre la evolución de alumnos por comisión para detectar aulas subutilizadas y sugerir reasignaciones.

### 2.5 Módulo de Seguridad, Notificaciones y Auditoría
Garantiza trazabilidad, control de accesos e integridad de operaciones.
- **Entidades**: `Usuario`, `Rol`, `Permiso`, `UsuarioRol`, `RolPermiso`, `Notificacion`, `Auditoria`.
- **Roles principales**:
  - `Solicitante`: Puede crear solicitudes de reserva y consultar sus propias reservas.
  - `ResponsableEspacio`: Gestiona y aprueba/rechaza reservas de los espacios a cargo de su departamento.
  - `SecretariaAcademica`: Gestiona la ocupación académica, comisiones, asignaciones y bloqueos generales.
  - `Administrador`: Acceso total a usuarios, roles, permisos, configuración, espacios y bitácora de auditoría.
- **Auditoría**: Bitácora que registra usuario, fecha/hora, acción realizada, entidad afectada, ID de la entidad y motivo o detalle.
- **Notificaciones**: Avisos para usuarios con mensaje, estado de lectura y fecha.

---

## 3. Restricciones de Base de Datos
- **Check Constraints**:
  - `CK_Reserva_FinMayorInicio`: `FechaHoraFin > FechaHoraInicio`.
  - `CK_Bloqueo_HastaMayorDesde`: `FechaHoraHasta > FechaHoraDesde`.
  - `CK_Espacio_CapacidadPositiva`: `CapacidadMaxima > 0`.
  - `CK_Reserva_AsistentesPositivos`: `AsistentesEstimados > 0`.
- **Índices**:
  - Índice único en `Espacio.Codigo`.
  - Índice compuesto `(EspacioId, FechaHoraInicio, FechaHoraFin)` en `Reserva` y `Bloqueo`.
  - Concurrencia optimista (`RowVersion` / `xmin`) en `Reserva`.
