# Gestión administrativa de roles

La API administra el catálogo de roles con autorización `usuarios.gestionar`. El token debe enviarse como `Authorization: Bearer <jwt>`.

## Endpoints

| Método | Ruta | Respuesta | Descripción |
| --- | --- | --- | --- |
| `GET` | `/api/roles` | `200 RolDto[]` | Lista los roles disponibles. |
| `POST` | `/api/roles` | `201 RolDto` | Crea un rol personalizado. |
| `PUT` | `/api/roles/{id}` | `200 RolDto` | Actualiza el nombre y la descripción permitidos. |
| `DELETE` | `/api/roles/{id}` | `204` | Elimina un rol personalizado sin usuarios ni permisos asociados. |

El DTO de creación y actualización es `{ "nombre": "string", "descripcion": "string|null" }`. El nombre es obligatorio y admite hasta 50 caracteres; la descripción admite hasta 250. Las respuestas usan `{ "id": 1, "nombre": "...", "descripcion": "..." }`.

Los roles `Solicitante`, `ResponsableEspacio`, `SecretariaAcademica` y `Administrador` están protegidos: no se pueden eliminar ni renombrar. La descripción de un rol institucional puede editarse. Los roles personalizados se crean sin permisos; esta API no expone todavía la asignación de permisos a roles. Un rol asignado a usuarios no puede eliminarse. Los intentos inválidos devuelven `400`, la falta de permiso `403`, un recurso inexistente `404` y una restricción de negocio `409` en formato ProblemDetails.

La Secretaría Académica recibe `usuarios.gestionar` mediante el sembrador incremental. Al actualizar el backend, el sembrador agrega el permiso sin modificar las demás relaciones; los usuarios deben iniciar sesión nuevamente para obtener un JWT con los claims actualizados.
