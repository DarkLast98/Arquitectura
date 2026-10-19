# Especificación de implementación: catálogo arquitectónico

**ID:** SPEC-CAT-001 · **Versión:** 0.1.0 · **Estado:** propuesta para revisión.

**Incremento:** I1, BL-00–BL-05. **No autoriza código ni infraestructura.**

**Origen:** RF-CAT-001–003, RNF-ISO-001, RNF-AUD-001, RNF-CON-001.

## 1. Objetivo y límites

Gestionar manualmente entidades y relaciones tipadas de una organización y demostrar el recorrido completo entre interfaz, API y persistencia. Las decisiones de implementación candidatas son ADR-0002, 0003, 0004, 0006, 0008, 0010 y las fronteras de 0011.

No incluye agentes, regulación, grafo externo, paquetes dinámicos, tipos personalizados ni despliegue. No se interpreta una relación como evidencia de cumplimiento. [I1](../delivery/primer-incremento.md) define la demostración y exclusiones.

## 2. Contexto, identidad y autorización

Todas las rutas privadas parten de `/api/v1/organizations/{organizationId}/catalog`. El servidor valida identidad y membresía activa en esa organización; la ruta no acredita permiso. La identidad de desarrollo y cómo se aprovisionan membresías son decisiones bloqueantes P-02. Se requieren dos organizaciones y los tres roles para las pruebas, sin cabeceras libres de suplantación.

| Acción | organization_admin | architect | reader |
| --- | --- | --- | --- |
| Consultar tipos, entidades y relaciones | Sí | Sí | Sí |
| Crear/editar/archivar entidades | Sí | Sí | No |
| Crear/archivar relaciones | Sí | Sí | No |
| Consultar auditoría del catálogo | Sí | No | No |
| Administrar organizaciones o membresías | Fuera de I1 | No | No |

Un usuario sin autenticar recibe 401. Sin membresía en la organización de la ruta recibe 403 sin consultar recursos de catálogo. Con membresía, pero sin permiso de operación, 403. Dentro de una organización autorizada, un UUID inexistente o de otra organización devuelve el mismo 404. El cliente no puede escribir organización, actor, rol, revisión o fechas.

## 3. Registro fijo de tipos

`core.catalog` v0.1.0 proporciona estas definiciones inmutables de esquema `0.1.0`:

| type_id | Categoría | Origen permitido | Destino permitido |
| --- | --- | --- | --- |
| `core.business_capability` | entity | No aplica | No aplica |
| `core.application` | entity | No aplica | No aplica |
| `core.data_asset` | entity | No aplica | No aplica |
| `core.supports` | relationship | `core.application` | `core.business_capability` |
| `core.uses` | relationship | `core.application` | `core.data_asset` |

Todos están disponibles en ambas organizaciones del ejemplo. La validación de tipos usa este registro, no una cadena arbitraria. `schema_version` es obligatorio en creación y no se resuelve silenciosamente a «latest». No hay atributos libres, herencia dinámica ni editor de metamodelo en I1.

Los contratos JSON Schema futuros expresarán campos, longitudes, enums y rechazo de campos desconocidos; las reglas de autorización, referencias y concurrencia se comprueban además en dominio y base.

## 4. Datos y campos

### Entidad

| Campo | Regla |
| --- | --- |
| `id` | UUID generado por servidor, inmutable. |
| `organization_id` | UUID del contexto autorizado, asignado por servidor e inmutable. |
| `type_id`, `schema_version` | Del registro fijo; inmutables tras creación. |
| `code` | Obligatorio; 1–64 caracteres ASCII de `[A-Za-z0-9._-]`, empieza con alfanumérico; trim y normalización a mayúsculas. Inmutable. |
| `name` | Obligatorio, trim, 1–200 caracteres Unicode; sin texto de solo espacios. |
| `description` | Opcional y nullable, texto plano hasta 4000 caracteres. Nunca ejecutar HTML o instrucciones de su contenido. |
| `classification` | `internal`, `confidential` o `restricted`; por defecto `internal`. Es una etiqueta, no un mecanismo adicional de permisos en I1. |
| `status` | `active` o `archived`, controlado por servidor; creación activa. |
| `revision` | Entero positivo, empieza en 1 y aumenta por cada mutación confirmada. |
| `created_at`, `updated_at` | Instantes UTC asignados por servidor. |
| `created_by`, `updated_by` | Identidad validada del actor, no texto recibido del cuerpo. |

Unicidad de `(organization_id, type_id, code_normalized)`, incluso si está archivada. Dos organizaciones pueden usar el mismo código; dos tipos diferentes también. No reutilizar un código archivado ni cambiar tipo como modo de evitar la restricción.

### Relación

| Campo | Regla |
| --- | --- |
| `id`, `organization_id` | Asignados por servidor, con las mismas garantías de ámbito. |
| `type_id`, `schema_version` | Definición de relación del registro fijo, inmutable. |
| `source_entity_id`, `target_entity_id` | UUID distintos, extremos activos de la misma organización, tipos compatibles; inmutables. |
| `description` | Texto plano opcional/nullable, máximo 4000 caracteres; se establece en creación. |
| `status`, `revision`, fechas y actores | Mismas reglas de sistema que la entidad. |

Unicidad activa de `(organization_id, type_id, source_entity_id, target_entity_id)`. No hay edición de relación en I1: archivar y crear una nueva si cambia su descripción o sus extremos. Una relación archivada conserva historia y permite crear después una relación nueva equivalente con otro ID; nunca hay dos equivalentes activas.

### Persistencia e integridad

Modelo relacional candidato: `architecture_entities`, `architecture_relationships`, registro de tipos y `catalog_audit_events`, más organización/membresía. La organización no es una entidad de catálogo que pueda editarse para cambiar propiedad.

Claves candidatas compuestas `(organization_id, id)` en entidades y foráneas compuestas de ambos extremos de relaciones; índices y restricciones de unicidad con el mismo ámbito. Actividad de extremos y compatibilidad se validan transaccionalmente. La aplicación no usa un rol que pueda eludir RLS; el esquema físico y las políticas se implementarán solo cuando se autorice.

## 5. Reglas de operación y concurrencia

1. Crear entidad valida contrato, tipo, código y unicidad; persiste entidad y auditoría en una transacción.
2. Editar entidad solo permite `name`, `description` y `classification`, con al menos un campo. Campos ausentes no cambian; `null` solo es válido para descripción. Rechazar campos adicionales.
3. Crear relación resuelve ambos extremos en la organización activa y verifica su estado y tipos. Un extremo ajeno se trata como inexistente.
4. Archivar una entidad con relaciones activas incidentes devuelve 409. Archivar primero cada relación; no hay cascada silenciosa.
5. Registro archivado sigue consultable dentro del ámbito, pero no editable ni rearchivable (409). No hay restauración o borrado físico en I1.
6. GET de detalle devuelve ETag fuerte basado en revisión, por ejemplo `"3"`. PATCH y comandos de archivo exigen `If-Match` con una revisión concreta; no aceptar comodín ni listas de revisiones. Ausente: 428; sintaxis inválida o comodín/lista: 400; revisión no vigente: 412; éxito incrementa revisión. Para esta API no se promete otro uso de cabeceras condicionales en I1.
7. Validar y confirmar edición/archivo por comparación atómica de revisión. Dos mutaciones con el mismo ETag: como máximo una se confirma.
8. Creación de relación y archivo de extremos deben coordinarse mediante bloqueos de filas o garantías equivalentes de transacción. Si la relación gana, el archivo falla; si el archivo gana, falla la relación. No puede quedar relación activa con extremo archivado. Mantener orden de bloqueo determinista para reducir deadlocks y tratar reintentos seguros.
9. Si falla auditoría, integridad o autorización, revertir toda la mutación. No devolver éxito parcial.
10. No se ofrece idempotency-key en I1. No reintentar POST automáticamente tras respuesta incierta; consultar por código/extremos dentro de la organización y reconciliar. Duplicado confirmado devuelve 409 sin duplicar datos.

## 6. Contrato HTTP propuesto

Todas las rutas siguientes son relativas al prefijo organizacional. JSON en éxito; errores `application/problem+json`. UUID inválido en ruta: 400. Entrada o campo desconocido: 422, salvo JSON malformado que da 400.

| Método y ruta | Entrada/resultado | Éxito |
| --- | --- | --- |
| GET `/types` | Lista de cinco definiciones con ID, categoría, versión y restricciones de extremos. | 200 |
| POST `/entities` | `type_id`, `schema_version`, `code`, `name`; opcionales descripción y clasificación. | 201, representación, Location y ETag. |
| GET `/entities` | Filtros y paginación descritos abajo. | 200, página. |
| GET `/entities/{id}` | Detalle autorizado, incluido archivado. | 200 y ETag. |
| PATCH `/entities/{id}` | JSON con campos permitidos, If-Match. No se usa JSON Patch. | 200, representación y nuevo ETag. |
| POST `/entities/{id}/archive` | Sin cuerpo, If-Match. | 200, representación y nuevo ETag. |
| POST `/relationships` | Tipo/versión, origen y destino; descripción opcional. | 201, representación, Location y ETag. |
| GET `/relationships` | Filtros y paginación. | 200, página. |
| GET `/relationships/{id}` | Detalle autorizado, incluido archivado. | 200 y ETag. |
| POST `/relationships/{id}/archive` | Sin cuerpo, If-Match. | 200, representación y nuevo ETag. |
| GET `/audit-events` | Solo admin organizacional; filtros por recurso y paginación. | 200, página. |

No existe DELETE, PATCH de relación o endpoint que cambie organización, tipo o versión de esquema.

### Consulta

Paginación `limit` por defecto 20, rango 1–100; `offset` por defecto 0, rango 0–10000. Respuesta `{items, total, limit, offset}`. Total y filtros se calculan dentro del mismo ámbito. No se promete instantánea estable entre varias peticiones mientras otros usuarios escriben.

- Entidades: `type_id`, `status` (`active` por defecto, `archived` o `all`), `code` exacto normalizado y `q` hasta 128 caracteres para subcadena de nombre sin distinguir mayúsculas. No se garantiza equivalencia de acentos. Orden fijo por código normalizado y UUID.
- Relaciones: `type_id`, `status` con las mismas opciones, `source_entity_id`, `target_entity_id` o `entity_id` para ambos extremos incidentes. `entity_id` no se combina con filtros de origen/destino. Orden fijo por `created_at` e ID. Referencia filtrada ajena/inexistente no revela el recurso: página vacía.
- Auditoría: `resource_kind` (`entity` o `relationship`) y `resource_id` opcionales, orden por instante y ID. Nunca consulta global de auditoría.
- Rechazar parámetros desconocidos, combinaciones y valores inválidos con 400. Tipo desconocido o de categoría incorrecta es filtro inválido. No concatenar filtros como SQL libre.

### Cuerpo de creación ilustrativo

```json
{
  "type_id": "core.application",
  "schema_version": "0.1.0",
  "code": "APP-PAGOS-DEMO",
  "name": "Portal de pagos de demostración",
  "description": "Aplicación ficticia para validar el catálogo",
  "classification": "internal"
}
```

El ejemplo es un contrato propuesto y no una llamada ejecutada. No contiene organización ni campos de sistema. Todos los textos se muestran como texto plano, sin interpretar HTML o instrucciones.

### Errores

| HTTP | Causa |
| --- | --- |
| 400 | JSON/ruta/filtro malformado, parámetros de consulta inválidos o sintaxis If-Match no admitida. |
| 401 | Identidad ausente o inválida. |
| 403 | Sin membresía o permiso de operación. |
| 404 | Recurso no visible en la organización autorizada. |
| 409 | Código duplicado, relación activa duplicada, extremo archivado, entidad con relaciones activas o operación sobre archivado. |
| 412 | Revisión If-Match obsoleta. |
| 422 | Campos inválidos/desconocidos, versión/tipo no admitido, extremos del mismo ID o tipos incompatibles. |
| 428 | Falta If-Match en operación que lo exige. |

Problem Details incluye `type`, `title`, `status`, `code`, `correlation_id` y errores de campo si proceden. `type` puede usar `about:blank`; códigos estables de aplicación como `CAT_DUPLICATE_CODE` o `CAT_REVISION_CONFLICT` se documentarán en OpenAPI. No incluir SQL, nombres privados ajenos, tokens o trazas internas. Para un recurso existente, comprobar permiso y visibilidad antes de devolver estado, ETag o conflicto de negocio.

## 7. Auditoría

Cada creación, edición o archivo produce un evento con ID, organización, actor, operación, clase/ID del recurso, revisiones anterior/nueva, instante UTC y correlación. Para creación la revisión anterior es nula. Registrar nombres de campos modificados; no copiar por defecto descripciones o contenido sensible. Los intentos fallidos no generan eventos de mutación confirmada; seguridad puede registrar metadatos mínimos sin exponer contenido.

Los eventos de I1 son append-only para la aplicación y se confirman en la misma transacción del cambio. La consulta de auditoría es solo para `organization_admin` y su organización. Esta historia de cambios no implementa todavía un repositorio de snapshots completos ni un workflow de aprobación. Retención y protección productiva frente a administradores de base requieren una decisión posterior; no se promete inmutabilidad criptográfica.

## 8. Interfaz de usuario

Seleccionar únicamente organizaciones con membresía validada; no basta con ocultar opciones. Cambiar contexto limpia listas, selecciones y cachés previas. Formularios muestran definiciones permitidas; pantalla de relación filtra extremos compatibles, pero el backend repite toda validación.

Mostrar errores de validación, acceso y concurrencia sin perder cambios de formulario innecesariamente. Ante 412, recuperar versión nueva y pedir reconciliación; no sobrescribir automáticamente. Deshabilitar acciones de lector por claridad y demostrar que API también las rechaza. Archivar exige confirmar la intención en UI y explicar bloqueos por relaciones; sin cascada. Auditoría se ofrece solo a administrador autorizado.

No se requiere editor visual de grafos, drag-and-drop ni diseño de marca para demostrar el slice. Una lista de relaciones y navegación de detalle es suficiente.

## 9. Aceptación verificable

| ID | Escenario y resultado exigido |
| --- | --- |
| AC-01 | Arquitecto A crea los tres tipos de entidad y recupera lo persistido desde la interfaz con API y PostgreSQL reales. Tipos/versiones/campos inválidos dan 422 sin mutación. |
| AC-02 | Código normalizado duplicado en igual organización/tipo da 409; el mismo código en B o en otro tipo se permite. |
| AC-03 | Las dos relaciones válidas se crean y navegan; dirección/tipos incompatibles, autorrelación y duplicado activo se rechazan según contrato. |
| AC-04 | A y B no se ven por UUID, listas, búsqueda, filtros, conteos, relaciones o auditoría. Extremos ajenos dan 404 y no dejan datos. |
| AC-05 | Sin identidad 401; sin membresía/permisos 403; reader no muta; solo admin de su organización consulta auditoría. |
| AC-06 | GET entrega ETag; mutación sin If-Match da 428, con versión antigua 412; de dos cambios con igual revisión como máximo uno se confirma. |
| AC-07 | Entidad con relación activa no se archiva; tras archivar la relación puede archivarse. Archivados no se editan, restauran o borran en I1. |
| AC-08 | Crear relación concurrentemente con archivar un extremo nunca deja una relación activa hacia un archivado. |
| AC-09 | Cada mutación y su evento se confirman o revierten juntos; fallo inducido de auditoría no deja cambios parciales. |
| AC-10 | Conexiones A → B → sin contexto y SQL con rol real verifican RLS, escritura protegida y ausencia de contexto residual o bypass. |
| AC-11 | Páginas, orden, filtros, errores y límites coinciden con contrato; sin exposición de SQL, credenciales o contenido de otras organizaciones. |
| AC-12 | UI muestra textos no confiables sin ejecutar HTML; intenta escritura prohibida y conflicto, muestra resultado correcto y limpia contexto al cambiar organización. |
| AC-13 | Reiniciar el proceso de aplicación conserva datos y permite repetir la demostración con base persistente; instrucciones locales no dependen de un proceso heredado. |
| AC-14 | OpenAPI y JSON Schema validan ejemplos y rechazan entradas inválidas; pruebas contrastan respuestas reales y contratos versionados. |

Pruebas de dominio para reglas; integración con PostgreSQL real y rol no privilegiado para restricciones, RLS y carreras; API para autorización/contrato; un recorrido de interfaz para utilidad completa. In-memory o mocks no demuestran aislamiento ni semántica transaccional. Ningún caso está ejecutado todavía.

## 10. Orden de implementación y aprobación

Tras aprobación: fijar identidad/versiones y contratos; crear límites modulares y persistencia de catálogo con contexto; construir entidad y auditoría; relaciones y concurrencia; interfaz; cerrar evidencia de todos los casos. Incorporar pruebas junto con cada comportamiento, no posponer aislamiento a la entrega final.

La autorización debe identificar I1/SPEC-CAT-001 v0.1.0 o sus cambios y las decisiones aplicables. P-02 y P-03 deben resolverse antes del código que dependa de ellas. Lo restante del producto sigue diferido. Esta especificación preparada es el resultado de la tarea actual, no un inicio de implementación.
