# Contrato de Endpoints de la API

Basado en la especificación del sistema de reservas de espacios de la Facultad.

## Convenciones Generales
- Formato de intercambio: `JSON` (application/json)
- Fechas y Horas: ISO 8601 (`YYYY-MM-DDTHH:mm:ssZ`)
- Respuestas de error estandarizadas con código HTTP correspondiente (400, 401, 403, 404, 409, 500).

---

## 1. Módulo de Espacios
| Método | Ruta | Descripción | Rol Requerido |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/espacios` | Devuelve lista de todos los espacios | Autenticado |
| `GET` | `/api/espacios/disponibles` | Devuelve espacios disponibles para un rango de fecha/hora y capacidad | Autenticado |
| `GET` | `/api/espacios/{id}` | Devuelve un espacio específico con su equipamiento | Autenticado |
| `POST` | `/api/espacios` | Agrega un nuevo espacio físico | Administrador / Responsable |

---

## 2. Módulo de Reservas
| Método | Ruta | Descripción | Rol Requerido |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/reservas` | Crea una solicitud de reserva eventual | Solicitante / Cualquiera con permiso |
| `GET` | `/api/mis-reservas` | Devuelve las reservas creadas por el usuario autenticado | Solicitante / Autenticado |
| `GET` | `/api/reservas/pendientes` | Devuelve solicitudes aún no aceptadas pendientes de revisión | Responsable / Secretaria / Admin |
| `GET` | `/api/reservas/canceladas` | Devuelve solicitudes rechazadas / canceladas | Responsable / Secretaria / Admin |
| `PATCH` | `/api/reservas/{id}` | Cambia el estado de una solicitud (Aprobar, Rechazar, Cancelar) | Responsable / Admin / Solicitante |

---

## 3. Módulo de Bloqueos
| Método | Ruta | Descripción | Rol Requerido |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/bloqueos` | Crea un bloqueo sobre un espacio (mantenimiento, reparaciones) | Secretaria / Admin / Responsable |
| `GET` | `/api/bloqueos` | Lista todos los bloqueos activos o por filtro | Autenticado |

---

## 4. Módulo de Catálogos y Auditoría
| Método | Ruta | Descripción | Rol Requerido |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/departamentos` | Lista desplegable de departamentos/secretarías | Autenticado |
| `GET` | `/api/equipamientos` | Catálogo flexible de equipamientos disponibles | Autenticado |
| `GET` | `/api/auditoria` | Devuelve la bitácora de auditoría de acciones importantes | Administrador |

---

## 5. Módulo de Usuarios y Autenticación
| Método | Ruta | Descripción | Rol Requerido |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Iniciar sesión y obtener token JWT | Público |
| `GET` | `/api/auth/me` | Consultar datos del usuario autenticado | Autenticado |
| `POST` | `/api/usuarios` | Crea un nuevo usuario | Administrador |
| `GET` | `/api/usuarios` | Listar usuarios del sistema | Administrador |
| `GET` | `/api/usuarios/{id}` | Consultar un usuario específico | Administrador |
| `PATCH` | `/api/usuarios/{id}` | Modificar datos o roles de un usuario | Administrador |

---

## 6. Monitoreo y Diagnóstico
| Método | Ruta | Descripción | Rol Requerido |
| :--- | :--- | :--- | :--- |
| `GET` | `/health` | Chequeo de salud del servicio y conectividad | Público |
| `GET` | `/swagger` | Documentación interactiva OpenAPI/Swagger UI | Público |
