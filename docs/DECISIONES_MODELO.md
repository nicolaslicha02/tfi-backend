# Decisiones del modelo implementado

Este documento registra diferencias entre el DER conceptual entregado y el modelo relacional del backend para mantener trazable su evolución. No reemplaza la actualización del DER de entrega.

## Responsabilidad de los espacios

El diagrama dibuja una entidad `ResponsableDirecto` vinculada con `Espacio`. El backend vincula cada espacio con `Departamento`; la autorización se aplica a nivel departamental, y un usuario responsable obtiene su alcance mediante `Usuario.DepartamentoId`. Esta decisión coincide con la regla del ENUNCIADO de asignar un departamento responsable, pero no representa un responsable nominal por espacio.

## Vigencia de asignaciones académicas

El diagrama incluye fechas de vigencia y estado en `AsignacionEspacio`. La implementación conserva día y franja recurrente en esa entidad y obtiene el período de vigencia a través de `AsignacionEspacio -> Comision -> Cursada -> CicloLectivo`. No hay un estado independiente para una asignación. Si se requieren excepciones de vigencia a nivel de asignación, deben modelarse explícitamente antes de incorporarlas al calendario.

## Roles de usuario

El diagrama representa `rol_id` en Usuario. La implementación usa `UsuarioRol` y `RolPermiso` para permitir varias asignaciones de roles y permisos dinámicos. Es una normalización deliberada que requiere mantener el DER actualizado.

## Auditoría

El ENUNCIADO requiere actor, fecha, acción, entidad afectada, ID y motivo/detalle. La implementación extiende los atributos mínimos del diagrama con entidad, ID y motivo/detalle, y conserva nombre, email y rol del actor en cada evento para que la consulta muestre el contexto histórico aunque el usuario cambie o se elimine. Los eventos previos a estas columnas no tienen esos valores históricos recuperables con certeza.
