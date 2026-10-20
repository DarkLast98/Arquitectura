# Trabajo B — Plan de preparación e implementación de I1

**ID:** PLAN-I1-B · **Versión:** 1.0 · **Estado:** aprobado por el usuario («aprobado, implementa el PR») sobre `469f2dc`; implementación en curso, sin fusión automática.

**Fecha:** 2026-10-19 (America/Bogota).

**Rama:** `feature/i1-catalog-foundation`, creada desde `main` en `25c4fccdec6304f34b3e068a93740bfbb830f20b`.

Esta entrega contiene planificación y propuestas de decisión. No habilita implementación, aceptación de ADR, despliegue o fusión automática. La autorización del usuario se registrará antes de escribir código.

## 1. Inspección y línea base

Se revisaron AGENTS, PLANS, el estado, el primer incremento, SPEC-CAT-001, ontología, aislamiento y ADR-0002/0003/0004/0006/0008/0010/0011. `main` contiene la fundación documental, sin aplicaciones versionadas.

Existe una implementación anterior en el commit local `52c653d`, conservado en `work`. Usa identidad local con contraseñas y no cumple la nueva dirección OIDC de Trabajo B. No se importa ese commit, sus estados de aprobación, pruebas ni resultados en esta rama. Tras aprobar este plan se podrá revisar y reutilizar código compatible de forma selectiva; cada parte necesitará validación con OIDC y el nuevo contexto. No se descarta ni reescribe ese trabajo.

La aprobación previa de la implementación local no se atribuye a este nuevo plan. La instrucción actual autoriza inspección, propuesta y PR de revisión; exige aprobación del plan antes de implementar.

## 2. Alcance y compatibilidad

Fuente estable: [SPEC-CAT-001 v0.1.0](../specs/catalogo-arquitectonico.md), sin cambios en este PR. Se mantienen API `v1`, esquema `0.1.0`, roles, campos, operaciones y AC-01–AC-14. No confundir esos números con el metamodelo v0.2 paralelo.

| Tipo estable | Semántica en I1 |
| --- | --- |
| `core.business_capability` | Capacidad de negocio. |
| `core.application` | Aplicación. |
| `core.data_asset` | Metadatos de un activo de datos. |
| `core.supports` | application → business_capability. |
| `core.uses` | application → data_asset. |

Incluido: mantenimiento manual, consulta, relaciones, archivo lógico, concurrencia optimista, autorización, aislamiento, auditoría y recorrido Angular/.NET/PostgreSQL. OIDC agrega el acceso previo al catálogo, sin cambiar el metamodelo.

Fuera: Neo4j, agentes, paquetes instalables, entidades futuras, editor de esquemas, restauración/borrado físico, infraestructura desplegada, evaluación normativa y administración comercial de organizaciones. Los fixtures serán sintéticos. La coordinación con v0.2 se documenta en [compatibilidad](compatibilidad-metamodelo-v02.md).

## 3. Decisiones propuestas y puerta de autorización

| Decisión | Propuesta | Referencia | Estado |
| --- | --- | --- | --- |
| Topología | Un backend desplegable como monolito modular. | ADR-0002 | Propuesto. |
| Interfaz | Angular 22.2.3, TypeScript 6.0.3 y Node 24.19.0. | ADR-0003, ADR-0014 | Propuesto. |
| Backend | .NET 10 LTS, SDK 10.0.402, runtime 10.0.13. | ADR-0004, ADR-0014 | Propuesto. |
| Persistencia | PostgreSQL 17.11 como única autoridad transaccional. | ADR-0006, ADR-0014 | Propuesto. |
| Contratos | OpenAPI 3.1 y JSON Schema 2020-12, sujetos a soporte de herramientas. | ADR-0008 | Propuesto. |
| Aislamiento | Membresía de servidor, contexto transaccional y RLS con rol no propietario/sin bypass. | ADR-0010 | Propuesto. |
| Autenticación | Cliente OIDC confidencial con Code + PKCE, BFF y cookie protegida; proveedor por configuración. | ADR-0013 | Propuesto. |
| Metamodelo | Registro fijo de I1, sin depender del motor de paquetes ni de v0.2. | ADR-0011, SPEC-CAT-001 | Propuesto para implementación; definiciones conservadas. |

Las versiones y su soporte se justifican en [ADR-0014](../decisiones/0014-versiones-soportadas-i1.md). No se generan manifiestos ni lockfiles en la fase de plan.

**Antes de código:** registrar aprobación explícita de PLAN-I1-B v1.0, su alcance, ADR aplicables y condiciones. Identificar proveedor OIDC de desarrollo y perfiles de configuración; la recomendación es Keycloak o un proveedor existente que cumpla los requisitos de ADR-0013. Si se aprueba el plan con esa recomendación, concretar versión soportada, endpoints y registro de cliente antes de implementar la integración. Las credenciales se suministran fuera de Git y del chat. Ningún ADR cambia de estado con este PR.

## 4. Estructura prevista de proyectos

La siguiente estructura es una propuesta: no se crean estos proyectos ahora.

```text
src/
  Aip.Api/                       # Un único host .NET; composición y middleware
    Modules/
      IdentityAccess/            # OIDC, sesión, sujetos y membresía
        Public/                  # Contrato de contexto y autorización
        Application/
        Infrastructure/
      Catalog/
        Public/                  # Contratos y endpoints de catálogo
        Domain/                  # Agregados, tipos e invariantes
        Application/             # Comandos y consultas
        Infrastructure/
      Audit/
        Public/                  # Registro y consulta autorizada
        Application/
        Infrastructure/
    Infrastructure/Postgres/     # Conexión, unidad transaccional y migraciones
web/
  src/app/
    core/auth/                   # Sesión BFF; sin tokens persistidos en navegador
    core/organization-context/
    features/catalog/
    features/relationships/
    features/audit/
contracts/
  openapi/
  schemas/catalog/0.1.0/
tests/
  Aip.Domain.Tests/
  Aip.Api.IntegrationTests/
  Aip.Database.Tests/
  Aip.Contracts.Tests/
web/e2e/
```

Un proyecto productivo inicial, con módulos lógicos y contratos internos explícitos; no se exige una biblioteca o proceso por rol. Las pruebas verifican límites de dependencias: dominio no depende de HTTP, Npgsql ni OIDC; ningún módulo escribe tablas ajenas. La infraestructura implementa puertos y comparte transacción mediante una unidad de trabajo. Una extracción futura de assemblies requiere necesidad demostrada, no carpetas vacías anticipadas.

## 5. Módulos y agregados

| Módulo / agregado | Responsabilidad e invariantes |
| --- | --- |
| IdentityAccess / Subject | Identidad interna UUID vinculada a `(issuer, sub)` verificados, únicos. Email/nombre no identifican ni conceden acceso. |
| IdentityAccess / OrganizationMembership | Organización, sujeto, rol y estado activo/revocado. No se obtiene autoridad de campos del cliente o roles del IdP sin mapeo acordado. |
| Catalog / ArchitectureEntity | Organización, UUID, tipo, versión y código inmutables; nombre/descripción/clasificación editables; revisión positiva y estado active/archived. Código normalizado único por organización/tipo, incluso archivado. |
| Catalog / ArchitectureRelationship | Identidad propia, extremos/tipo/esquema inmutables; extremos activos, distintos, compatibles y de la misma organización. Unicidad de relación activa. No es una colección duplicada dentro de cada entidad. |
| Catalog / TypeDefinition | Registro inmutable de tres tipos de entidad y dos relaciones, versión 0.1.0. No hay extensión ni actualización dinámica. |
| Audit / CatalogAuditEvent | Evento append-only con organización, actor, recurso, revisiones, operación, fecha y correlación; sin copiar descripciones por defecto. |

Crear relación y archivar extremos son operaciones coordinadas entre agregados dentro de una transacción PostgreSQL. Catalog invoca el contrato público de Audit; una falla del evento revierte también la mutación. No escribir directamente tablas de otro módulo para evitar sus reglas.

## 6. Autenticación y autorización

Diseño detallado y alternativas en [ADR-0013](../decisiones/0013-oidc-desarrollo-configurable.md). Angular se sirve bajo el mismo origen del BFF. .NET realiza el intercambio OIDC Code + PKCE; firma/emisor/audiencia/tiempo/nonce/state se validan mediante el handler oficial. Cookies HttpOnly y Secure, claves privadas fuera de Git y antiforgery en mutaciones. No enviar ID tokens como credencial del catálogo ni guardar tokens en localStorage.

1. Autenticar el sujeto mediante OIDC; resolver su UUID interno por el par verificado issuer/sub.
2. Seleccionar una organización autorizada desde `/api/session`.
3. En cada operación, comprobar membresía activa y rol antes de acceder al catálogo.
4. Iniciar transacción y establecer contexto local de sujeto/organización derivado del servidor.
5. Aplicar permisos del caso de uso y políticas RLS con verificación de membresía. Contexto ausente deniega.
6. Confirmar datos y auditoría juntos, limpiar contexto al devolver la conexión al pool.

| Acción | organization_admin | architect | reader |
| --- | --- | --- | --- |
| Consultar tipos, entidades y relaciones | Sí | Sí | Sí |
| Crear/editar/archivar entidades | Sí | Sí | No |
| Crear/archivar relaciones | Sí | Sí | No |
| Consultar auditoría | Sí | No | No |
| Administrar usuarios, organizaciones o membresías | Fuera de I1 | Fuera de I1 | Fuera de I1 |

Alta de membresías sintéticas mediante preparación local privilegiada después de aprobar; no hay autoservicio ni rol global implícito. Un login válido sin membresía no obtiene acceso. Cambiar de proveedor no fusiona usuarios por email. Las APIs privadas devuelven 401/403, nunca redirigen a HTML de login; solo el endpoint de acceso inicia el challenge.

## 7. Contratos HTTP

Prefijo de catálogo conservado: `/api/v1/organizations/{organizationId}/catalog`.

| Método / ruta relativa | Resultado y condiciones |
| --- | --- |
| GET `/types` | 200 con las cinco definiciones fijas. |
| POST `/entities` | 201, representación, Location y ETag. Tipo/esquema/código/nombre obligatorios; descripción/clasificación opcionales. |
| GET `/entities` | 200 con `{items,total,limit,offset}` y filtros de SPEC-CAT-001. |
| GET `/entities/{id}` | 200 y ETag; 404 para inexistente o ajeno al contexto autorizado. |
| PATCH `/entities/{id}` | Solo nombre/descripción/clasificación; If-Match obligatorio; 200 y ETag nuevo. |
| POST `/entities/{id}/archive` | Sin cuerpo; If-Match; 409 si conserva relaciones activas. |
| POST `/relationships` | 201 con Location/ETag; validar tipo/esquema y ambos extremos. |
| GET `/relationships` | 200 con filtros y paginación de la especificación. |
| GET `/relationships/{id}` | 200 y ETag o 404 contextual. |
| POST `/relationships/{id}/archive` | Sin cuerpo; If-Match; no borrado físico. |
| GET `/audit-events` | 200 paginado, solo admin de la organización. |

Se conservan filtros, límites (limit 1–100 y offset 0–10000), orden, status por defecto y reglas de campos de SPEC-CAT-001. No existe PATCH de relación, DELETE o cambio de organización/tipo/esquema. Los esquemas rechazan campos de sistema y adicionales.

Problem Details con código estable/correlación, sin SQL ni información ajena: 400 entrada malformada/filtro inválido, 401 identidad inválida, 403 falta de membresía/permiso, 404 recurso no visible, 409 conflicto de dominio, 412 revisión obsoleta, 422 validación y 428 falta de If-Match. Tras verificar permiso y visibilidad, comparar revisión de forma atómica. El antiforgery inválido tendrá un código de autenticación con HTTP 400, documentado en el contrato adicional de acceso.

Endpoints adicionales propuestos, fuera del namespace de catálogo: GET `/auth/login` inicia challenge; callback `/signin-oidc` del middleware; POST `/auth/logout` protegido por CSRF; callback `/signout-callback-oidc`; GET `/api/session` devuelve usuario mostrado, organizaciones/roles permitidos y token antiforgery. Solo retorno local, nunca URLs arbitrarias. No hay endpoint de contraseñas ni emisión de tokens propia de AIP.

## 8. Modelo relacional propuesto

| Tabla / propietario | Claves, datos y restricciones |
| --- | --- |
| `identity.subjects` | UUID interno, issuer y sub únicos, estado y fechas; sin contraseñas locales. |
| `identity.organizations` | UUID, nombre y estado de organizaciones sintéticas. |
| `identity.memberships` | PK `(organization_id,subject_id)`; FK a organización/sujeto, rol enumerado y estado activo/revocado. |
| `catalog.type_definitions` | PK `(type_id,schema_version)`; categoría, esquema y restricciones de extremos. Registro global fijo, solo lectura para la aplicación. |
| `catalog.entities` | PK `(organization_id,id)`; FK a organización y definición; campos de SPEC; UNIQUE `(organization_id,type_id,code)` normalizado; índices de filtro/orden. |
| `catalog.relationships` | PK `(organization_id,id)`; FKs compuestas de ambos extremos; UNIQUE parcial de organización/tipo/origen/destino WHERE active; índices de ambos extremos. |
| `audit.catalog_events` | PK `(organization_id,id)`; actor interno, recurso, revisiones y metadatos de cambio; SELECT solo admin del ámbito, INSERT controlado, sin UPDATE/DELETE para rol de aplicación. |
| `schema_migrations` | Versión/checksum y ejecución; accesible al migrador, no modificable por la aplicación. |

JSON Schema valida el contrato, pero integridad referencial, estado, autorización y compatibilidad también se comprueban en dominio/base. RLS forzada en tablas privadas cuando corresponda; rol de aplicación no propietario, sin superusuario ni BYPASSRLS. Lectura de membresías/organizaciones limitada al sujeto autorizado. Registro global de tipos no contiene datos organizacionales.

Código se normaliza al crear y no se reutiliza al archivar. Revisión empieza en 1 y se incrementa por mutación confirmada. Entidades archivadas siguen consultables y no editables. Relaciones activas se archivan explícitamente antes de la entidad. Sin cascadas silenciosas ni migración de esquemas en I1.

## 9. Transacciones y concurrencia

Usar transacciones cortas y contexto mediante `SET LOCAL`/equivalente parametrizado, no estado de sesión persistente. Lecturas y escrituras siempre conservan el ámbito organizacional.

PATCH y archivo comparan ETag/revisión y actualizan atómicamente. Crear relación bloquea filas de sus dos extremos en un orden canónico compartido y comprueba de nuevo actividad y tipos dentro de la transacción. Archivar entidad obtiene el mismo bloqueo antes de consultar relaciones activas. Si gana crear relación, archivar falla; si gana archivar, crear falla. Una relación activa jamás puede apuntar a un extremo archivado.

Datos y evento de Audit comparten conexión/transacción. No introducir outbox, escrituras dobles, broker o consistencia eventual para I1. Reintentar únicamente fallos transitorios seguros, sin duplicar POST ni ocultar conflictos; I1 no ofrece idempotency-key.

## 10. Migraciones previstas

| Orden | Contenido a implementar después de aprobación |
| --- | --- |
| M001 | Roles de migración/aplicación, schemas, sujetos OIDC, organizaciones y membresías; restricciones y permisos mínimos. |
| M002 | Registro de tipos 0.1.0 y seed exacto de las cinco definiciones; solo lectura de aplicación. |
| M003 | Entidades, códigos/estados/revisiones, FK de definición, índices y RLS. |
| M004 | Relaciones, FKs compuestas, unicidad parcial e índices; reglas coordinadas de extremos. |
| M005 | Auditoría, políticas de inserción/lectura, protección contra modificación y permisos finales. |

Migrador separado del arranque de aplicación y del rol restringido. Historial con checksums y bloqueo de ejecución; una migración aplicada no se edita. Los fixtures de dos organizaciones y membresías OIDC se aplican solo a la base sintética de desarrollo/pruebas con sujetos verificados; no hay contraseñas ni clientes secretos en seeds. Probar instalación limpia, repetición sin duplicar seed y conservación de datos. No prometer rollback destructivo: reconstruir base desechable en pruebas y aplicar corrección progresiva fuera de ella.

## 11. Orden incremental posterior a aprobación

| Paso | Entrega comprobable | Condición de salida |
| --- | --- | --- |
| B0 | Registrar autorización; concretar proveedor/cliente OIDC, HTTPS, versiones y contratos. | Configuración segura disponible y decisiones aplicables registradas; sin inferir aprobación de ADR restantes. |
| B1 | Primer recorrido completo: login OIDC → organización → crear capacidad y aplicación → relación supports → consultar desde Angular y PostgreSQL. Incluye RLS, rol, auditoría, ETags y archivo seguro desde el inicio. | Pruebas positivas y negativas de aislamiento/concurrencia del recorrido; no basta un backend sin interfaz o una interfaz simulada. |
| B2 | Completar data_asset/uses y todas las operaciones, filtros/paginación, permisos y UI de SPEC-CAT-001. | Tres tipos/dos relaciones y AC-01–AC-12 implementados sin ampliar metamodelo. |
| B3 | Reinicio/persistencia, contratos, compatibilidad y OIDC completos; instrucciones reproducibles. | AC-01–AC-14 y OIDC-01–07 demostrados; evidencia con comandos, commits y resultados. |

Revisión por commits pequeños en la misma rama o PRs posteriores acordados. No fusionar automáticamente ni incluir v0.2 como prerrequisito. Este PR solo prepara B0 documental; no ejecuta B1–B3.

## 12. Estrategia de pruebas y aceptación

| Nivel | Pruebas previstas |
| --- | --- |
| Dominio | Tipos/versiones, normalización, campos inmutables, relación dirigida, estado archivado y conflictos. |
| PostgreSQL real | Migraciones, FKs de organización, unicidad, RLS con rol real, contexto ausente/reutilizado, permisos y auditoría append-only. |
| API | Matriz de roles/organizaciones, 401/403/404 sin filtraciones, filtros/páginas/errores, CSRF y ETag. |
| Concurrencia | Dos modificaciones con igual revisión: solo una confirma. Crear relación contra archivo de extremo: resultado válido para ambos órdenes, repetido y sin ventanas de carrera ocultas por mocks. |
| Transacción | Fallo inducido y acotado de auditoría: ni dato ni evento confirmado; no se prueba solo un código HTTP. |
| Contratos | OpenAPI y JSON Schema con validador independiente, ejemplos negativos y respuestas reales; comprobar que nombres, límites y estados coinciden con SPEC. |
| Navegador | Angular + API + PostgreSQL + proveedor OIDC real de desarrollo; crear/navegar/archivar, rol lector, cambio de organización, HTML no ejecutado y reconciliación sin perder borrador. |
| Operación local | Reiniciar API, conservar registros, reaplicar preparación sin destruir datos y ejecutar instrucciones en directorios documentados. |

Mantener AC-01–AC-14 de SPEC-CAT-001 como condiciones obligatorias; cada resultado se asociará a test/comando, commit, dato sintético y resultado real. No trasladar a esta rama resultados de `52c653d` como si validaran OIDC.

Aceptación adicional OIDC propuesta:

- **OIDC-01:** Code + PKCE con un proveedor de desarrollo, callback/nonce/state válidos, sesión protegida y logout.
- **OIDC-02:** firma, issuer, audience o tiempo inválidos y state/nonce alterados no autentican; redirección externa rechazada.
- **OIDC-03:** cambiar Authority/ClientId mediante perfil seguro permite otro issuer de pruebas sin recompilar y sin fusionar usuarios por email/sub aislado.
- **OIDC-04:** IdP inaccesible/configuración inválida falla cerrado; no se activa identidad local o header alternativo.
- **OIDC-05:** login válido sin membresía no concede catálogo; revocación de membresía se comprueba en servidor sin confiar en rol cacheado del IdP.
- **OIDC-06:** cookie protegida, antiforgery, 401/403 JSON en API y ausencia de tokens/secretos en navegador, logs o Git.
- **OIDC-07:** renovación de JWKS y PAR, si anunciado por el proveedor, conservan verificación y errores seguros; no desactivar validaciones para hacer pasar pruebas.

Los dobles OIDC se usan solo para casos negativos deterministas en pruebas, sin bypass accesible en la aplicación. La prueba positiva de navegador requiere integración con el proveedor de desarrollo elegido.

## 13. Riesgos y asuntos pendientes

| ID | Riesgo / decisión | Tratamiento y bloqueo |
| --- | --- | --- |
| B-P01 | Aprobar plan, alcance y ADR aplicables. | Bloquea todo código nuevo de Trabajo B. Registrar evidencia, versión/commit y exclusiones. |
| B-P02 | Proveedor OIDC, versión soportada, Authority, cliente, callbacks HTTPS y sujetos del fixture. | Recomendación Keycloak o proveedor existente. Configuración/secretos por canal seguro, no chat. Bloquea integración B1. |
| B-P03 | Toolchain y adaptador PostgreSQL concretos. | Matriz de ADR-0014; repetir comprobación de soporte antes de fijar manifests/lockfiles. |
| B-P04 | Propuesta v0.2 todavía no publicada en remoto. | Sin dependencia; comparar cuando exista y elevar incompatibilidades al siguiente incremento. |
| B-R01 | Cookie/CSRF/redirect y proveedor con variaciones de protocolo. | Handler oficial, pruebas OIDC negativas/positivas y origen HTTPS fijo; ningún bypass local. |
| B-R02 | RLS eludida por rol propietario o contaminación del pool. | Separación de roles, contexto transaccional, pruebas SQL reales y ausencia de contexto. |
| B-R03 | Carrera entre extremos, cambios y auditoría. | Bloqueos ordenados, comparación de revisión y transacción única probadas. |
| B-R04 | Reutilización indiscriminada del prototipo local. | Revisión selectiva posterior a aprobación; no cherry-pick completo ni importar identidad/passwords/estados. |
| B-R05 | Soporte o parche cambia entre plan y código. | Fuentes oficiales fechadas, pin explícito y nueva revisión de compatibilidad; nunca usar previews como solución automática. |

Residencia, SLAs, recuperación, identidad productiva, modelos/IA, licencias de paquetes y regulación permanecen fuera de I1. El plan no supone un presupuesto, autoridad jurídica o despliegue aprobado.

## 14. Entrega de planificación y siguiente autorización

PR documental hacia `main`: plan, propuestas OIDC/toolchain y regla de convivencia con v0.2, con enlaces desde PLANS y estado. No cambia las definiciones estables ni los estados de ADR anteriores. Revisión de enlaces, diff exclusivamente Markdown y comparación exacta de SPEC/ontología frente a la base.

La respuesta de aprobación debe identificar PLAN-I1-B v1.0 o el commit del PR, decisiones aceptadas y condiciones pendientes. Publicar la rama, abrir/revisar el PR o aprobar esta documentación como texto no se interpreta automáticamente como permiso de desplegar o fusionar. Después de registrar una autorización de implementación ya recibida, continuar B0–B3 sin pedirla de nuevo para las mismas acciones.
