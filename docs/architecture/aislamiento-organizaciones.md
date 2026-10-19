# Aislamiento por organización

**Requisito confirmado:** RQ-006. **Estrategia física candidata:** ADR-0010, propuesta. No hay controles ejecutados o verificados todavía.

## Invariantes

- Organización es el límite de propiedad y acceso del conocimiento privado. Una membresía en A no concede acceso a B; compartir usuario no mezcla contextos.
- Cada operación valida identidad, membresía activa y permiso sobre la organización seleccionada, antes de leer o escribir recursos.
- La ruta selecciona contexto, no autoridad. Nunca confiar en `organization_id`, roles o claims aportados en un cuerpo o cabecera arbitraria por el cliente.
- Entidad, relación, propuesta, evidencia, evaluación, ejecución y auditoría privados tienen `organization_id` obligatorio. Los extremos de una relación pertenecen a la misma organización.
- El identificador global de un recurso no concede acceso. Listados, conteos, búsquedas, errores y metadatos tampoco revelan información ajena.
- Fallar por defecto cuando falte contexto. No ofrecer consultas globales como sustituto de una organización no seleccionada.

## Defensa propuesta

| Superficie | Control |
| --- | --- |
| API | Identidad validada y permisos por operación; contexto derivado del servidor y membresía comprobada. |
| Dominio | Comandos reciben contexto autorizado, no organización editable por el solicitante. |
| PostgreSQL | Filtrado de dominio más RLS propuesta; claves/foráneas compuestas incluyen organización; restricciones de duplicados por ámbito. |
| Conexiones | Contexto transaccional local de organización; no estado de sesión que sobreviva al préstamo de una conexión. Contexto ausente deniega. |
| Roles de base | Rol de aplicación sin propiedad de tablas, privilegios administrativos o bypass de RLS; aislamiento de migración y operación. Políticas aplican a lectura y escritura, incluso bajo pooling. |
| Auditoría | Eventos privados de la organización; acceso controlado y sin privilegio de edición desde la API. |
| Caché y búsqueda futuras | Claves/índices y autorización por organización; no basta con filtrar al final. |
| Archivos y embeddings futuros | Namespace, permisos, ciclo de vida y recuperación con el mismo límite. No crear una memoria compartida de datos privados. |
| Agentes futuros | Contexto y herramientas autorizados por tarea; propuesta y salida vinculadas al mismo ámbito, sin acceso directo a credenciales de base. |
| Grafo opcional | Organización en nodos, aristas y consultas; si el producto o edición elegida no permite aislamiento verificable, no usarla para datos privados. |
| Observabilidad | Identificadores mínimos, sin cuerpos sensibles ni prompts completos por defecto. Logs de seguridad separados y accesibles solo por rol autorizado. |

RLS es defensa adicional, no sustituto de la autorización. PostgreSQL permite bypass según roles y propiedad: las pruebas deben usar el rol real de aplicación y comprobar políticas forzadas donde corresponda. No usar una cuenta administrativa para afirmar que el aislamiento funciona.

## Identidad y roles de I1

Provider y mecanismo de desarrollo quedan pendientes P-02. La solución debe validar identidad —incluidos emisor, audiencia, firma y caducidad si usa tokens— y resolver membresía y rol en el servidor. No aceptar una cabecera libre como usuario o administrador. Los dobles de autenticación solo se permiten dentro del entorno de pruebas, sin ruta activable en producción.

Roles propuestos del catálogo: `organization_admin` puede leer, escribir y consultar auditoría; `architect` puede leer y escribir; `reader` solo lee. La administración de membresías no forma parte de I1. Un operador global no hereda estos permisos sobre organizaciones.

## Casos de aceptación obligatorios

Con organizaciones sintéticas A y B y un usuario con membresía en ambas para las pruebas de cambio de contexto:

1. Leer un UUID de A bajo B devuelve el mismo 404 que un UUID inexistente.
2. Listas, filtros, conteos y mensajes de conflicto solo incluyen B cuando B es el contexto.
3. Relación de B con extremo de A se rechaza sin revelar ese extremo; no persiste nada.
4. Cuerpo intenta cambiar organización o campos de sistema: rechazo por contrato.
5. Lector no escribe y usuario sin membresía no accede a la ruta organizacional.
6. Conexiones reutilizadas A → B → sin contexto no conservan acceso de A; escrituras y lecturas sin contexto se deniegan.
7. Consultas directas con rol de aplicación, dentro de su contexto, no acceden ni escriben datos de otra organización.
8. Auditoría y futuras salidas de agentes no cruzan organizaciones. Un agente no puede seleccionar otra organización mediante su salida.

Amenazas de enumeración, suplantación de contexto, inyección en contenido, privilegios de soporte y exposición a proveedores requieren revisión continuada. Claves por organización, residencia, borrado y retención productiva se resolverán antes del piloto con datos reales; no prometerlos como ya implementados.
