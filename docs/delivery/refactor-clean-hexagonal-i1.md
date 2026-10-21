# Refactorización de I1 a Clean Architecture / hexagonal

**Fecha:** 2026-10-21 America/Bogota. **Estado:** implementado y validado técnicamente; listo para revisión humana del PR #1. **Autorización:** petición explícita del usuario recogida en ADR-0015. Rama de PR #1: feature/i1-catalog-foundation. Sin fusión, producción o I2.

## Plan y criterios

1. Separar Domain, Application, Infrastructure y Api; conservar un solo host y los módulos catálogo, identidad/acceso y auditoría.
2. Sustituir el puerto SQL por operaciones tipadas de repositorio, auditoría, identidad y transacción. Retirar HttpContext/JSON/SQL de los casos de uso.
3. Mantener contratos, invariantes, orden de locks, RLS, ámbito transaccional, OIDC y seis migraciones sin cambios semánticos.
4. Extraer transporte del componente Angular, componer CatalogWorkspace con un puerto HTTP y comprobar el núcleo sin Angular/DOM.
5. Incluir solución, comprobación de dependencias, pruebas de casos de uso y verificador reproducible actualizado.
6. Compilar y ejecutar la suite completa en exportación limpia; registrar evidencia sanitizada y actualizar el dictamen antes de considerar el PR listo.

| Criterio | Evidencia prevista |
| --- | --- |
| Dependencias hacia el núcleo | Proyectos sin paquetes externos en Domain/Application, referencias e importaciones verificadas. |
| Casos de uso independientes | Pruebas .NET sin servidor/base y TypeScript sin Angular/DOM. |
| Contratos y migraciones intactos | Comparación de archivos/checksums, generación de contratos y pruebas HTTP reales. |
| Aislamiento y autorización | Casos negativos API/SQL con PostgreSQL real y sesiones OIDC. |
| Consistencia | Revisiones, carreras archivo/relación y rollback de auditoría. |
| Entorno reproducible | scripts/verify-clean-i1.py desde commit exportado y datos/cachés nuevos. |

## Riesgos y decisiones de la refactorización

El principal riesgo es alterar serialización, orden de errores o límites transaccionales al extraer adaptadores. Se conserva la suite existente y se añade validación específica de puertos. No se usa una base en memoria para acreditar aislamiento/rollback. Las migraciones se mueven físicamente, por lo que preparación y verificador deben usar la nueva ruta. El dictamen técnico anterior queda como evidencia histórica de su commit. La ejecución nueva de abajo acredita el código refactorizado.

Las decisiones de operación productiva, versión futura del metamodelo y dependencias pendientes del IdP siguen con el alcance ya documentado. No se amplía funcionalidad.

## Resultado y dictamen final

**LISTO TÉCNICAMENTE PARA INTEGRAR I1 mediante decisión humana.** Código verificado `1d5eeb7acb7458629ee6f8be1bfca133259767b0`, terminado `2026-10-21T23:47:27Z` / `2026-10-21T18:47:27-05:00`. No quedan defectos bloqueantes identificados en esta refactorización. No se fusiona ni despliega y no se inicia I2.

`python3 scripts/verify-clean-i1.py` exportó ese commit sin datos/artefactos previos, reinstaló npm/Python y restauró NuGet con cachés vacías, PostgreSQL y Keycloak nuevos. Catálogo, relaciones y auditoría empezaron con cero registros; seis migraciones aplicadas. Resultado **PASS: 67 pruebas**: 6 dominio, 21 aplicación .NET, 5 TypeScript, 16 API/PostgreSQL y 19 navegador. Sin omisiones o casos inestables. Build .NET sin errores/advertencias y build Angular correctos; contratos, reinicio, migraciones repetibles y rechazo de checksum alterado correctos.

La [evidencia JSON sanitizada](evidence/i1-clean-hexagonal-2026-10-21.json) vincula commit, hash de exportación/árbol de implementación, casos y hashes de TRX/XML/JSON originales. Artefactos privados temporales: `/tmp/aip-i1-review-uwj_wtqw/clean-review.json` y `repo/.local/i1/test-results/` bajo ese directorio. Los commits posteriores solo actualizan documentación/evidencia; se comprueba que el árbol de código/herramientas/contratos no cambia. Esta es una ejecución local, sin atribuirle un check de GitHub CI.

## Matriz de arquitectura y entorno

| Criterio | Evidencia ejecutada | Resultado |
| --- | --- | --- |
| HEX-01 Dependencias hacia el núcleo | check-architecture.py; referencias de ensamblados en InnerAssembliesCannotDependOnFrameworksOrAdapters; Domain/Application sin paquetes o FrameworkReference. | PASS |
| HEX-02 Casos de uso independientes | 21 casos .NET sobre puertos: normalización, edición parcial/null, revisión, archivo, locks, defensa organizacional, roles, sujeto revocado y auditoría. | PASS |
| HEX-03 Frontend independiente | test:core compila sin DOM/Angular; 5 casos TypeScript comprueban payload mutable, esquema, tipos de extremos, CSRF/If-Match y conflicto 412. | PASS |
| HEX-04 Adaptadores y límites transaccionales | 16 casos sobre PostgreSQL real y 19 de navegador con OIDC: RLS, rollback, archivo/relación concurrentes, roles y protocolos conservados. | PASS |
| HEX-05 Compatibilidad | SPEC y ontología idénticas a main/25c4fcc; contratos y bytes/checksums de migraciones 001–006 idénticos a 81f560f. Traslado físico, sin nueva migración. | PASS |
| HEX-06 Entorno reproducible | Aip.slnx, verify-core-i1.py sin servicios y verify-clean-i1.py; controles de arquitectura en la aceptación y dependencias bloqueadas. | PASS |

## Ajustes de revisión

- La composición registra el pool PostgreSQL mediante fábrica: el contenedor es propietario de su ciclo de vida y lo dispone al cerrar el host. Los adaptadores comparten el DbSession de la solicitud.
- Una repetición inmediata se detuvo antes del build/pruebas porque el preflight confundía TIME_WAIT con un listener. Se usa SO_REUSEADDR, igual que los servidores locales; una comprobación con sockets efímeros demuestra rechazo de listener activo y reutilización del socket cerrado. El intento fallido se conserva en la evidencia; el commit final vuelve a pasar la exportación limpia completa.
- Las pruebas reales conservan permisos por endpoint, validación Unicode/contratos, auditoría atómica y validación date-time añadidos en la revisión anterior. No se sustituye la prueba real de aislamiento por dobles de puertos.

## Matriz final de aceptación de SPEC-CAT-001

Referencias de pruebas: [API/PostgreSQL](../../tests/test_catalog.py), [dominio](../../tests/Aip.Catalog.Domain.Tests/Invariants.cs), [navegador de catálogo](../../web/e2e/catalog.spec.ts), [OIDC](../../web/e2e/oidc.spec.ts), [contratos](../../web/e2e/contracts.mjs) y [verificador](../../scripts/verify-i1.py). Los identificadores de caso y sus resultados están en la evidencia JSON.

| Criterio | Evidencia verificable | Resultado |
| --- | --- | --- |
| AC-01 | API `test_01`, dominio y recorrido Angular: tres tipos creados/recuperados con PostgreSQL real; tipo/esquema/campos inválidos rechazados. | PASS |
| AC-02 | API `test_02` y `test_14`: normalización, duplicado 409, mismo código permitido en otra organización/tipo y prohibido tras archivo. | PASS |
| AC-03 | API `test_03`/`test_14` y navegador: supports/uses válidas y navegables; dirección, autorrelación, duplicado y extremos inválidos rechazados. | PASS |
| AC-04 | API `test_04`/`test_10`/`test_14`: UUID, listas, filtros, conteos, relaciones y auditoría aislados; FK organizacional rechaza SQL cruzado. Cambio A/B probado en UI. | PASS |
| AC-05 | API `test_05`/`test_12` y navegador: 401/403, escritura de lector denegada y auditoría exclusiva de admin, incluidas variantes de ruta. | PASS |
| AC-06 | API `test_06`: ETag fuerte, 428/400/412 y dos PATCH simultáneos con un solo éxito. UI conserva formulario y exige reconciliación. | PASS |
| AC-07 | API `test_07`/`test_14` y navegador: archivo bloqueado por relaciones, secuencia correcta y archivados consultables sin edición/restauración/borrado. | PASS |
| AC-08 | API `test_08`: seis carreras relación/archivo solo admiten 201/409 o 409/200; no queda relación activa hacia un extremo archivado. | PASS |
| AC-09 | API `test_09`/`test_15`: creación, edición y archivo se revierten ante fallo inducido de auditoría; sin eventos parciales ni revisión alterada. | PASS |
| AC-10 | API `test_10`/`test_12`, inspección SQL del rol PostgreSQL: conexión A→B→sin contexto, reader sin escritura, RLS forzada y rol real no propietario/sin superusuario/BYPASSRLS. | PASS |
| AC-11 | API `test_11`/`test_13`/`test_16`: filtros, orden, límites, páginas y rechazo de entradas; errores sin SQL/secretos. | PASS |
| AC-12 | Navegador: HTML no ejecutado, lector denegado, conflicto sin pérdida del formulario y limpieza al cambiar organización. | PASS |
| AC-13 | Verificador detiene/reinicia la API y recupera la misma representación/revisión con la base y sesión persistentes. El wrapper parte de servicios/datos nuevos. | PASS |
| AC-14 | OpenAPI 3.1 y 12 definiciones JSON Schema 2020-12 validados; comparación de archivos generados/servidos; formatos y ejemplos inválidos rechazados; respuestas reales contrastadas. | PASS |

## Autenticación y seguridad

| Control de PLAN-I1-B / ADR-0013 | Evidencia | Resultado |
| --- | --- | --- |
| OIDC-01 | Cuatro perfiles reales en Keycloak 26.8.1; Code + PKCE S256, PAR, cookie Secure/HttpOnly/Lax, CSRF y logout con retorno. | PASS |
| OIDC-02 | Proveedor HTTPS de fallos separado: state, firma, issuer, audiencia, exp/nbf y nonce inválidos no crean sesión ni sujeto interno. Retorno externo rechazado. | PASS |
| OIDC-03 | Segundo realm/cliente real con mismo email no hereda membresías; otro issuer con el mismo sub tampoco. Mismo binario, solo cambia perfil. | PASS |
| OIDC-04 | ClientSecret vacío impide arrancar; IdP inaccesible falla cerrado y API permanece en 401, sin login local ni cabeceras de suplantación. | PASS |
| OIDC-05 | Sin membresía 403; revocación afecta sesión existente; sujeto revocado no recibe cookie. | PASS |
| OIDC-06 | Cookies protegidas, CSRF, 401/403 JSON sin redirect, localStorage vacío, credencial bearer no admitida y secretos fuera de Git. | PASS |
| OIDC-07 | Cambio real de clave activa de Keycloak y renovación de JWKS; PAR anunciado utilizado. No se desactiva verificación TLS o de tokens. | PASS |

## Dependencias, ADR y riesgos residuales

Auditorías npm y NuGet de API/pruebas de dominio/aplicación, incluidas transitivas: **0 avisos**; `pip check` correcto. El núcleo no añade paquetes; se conservan versiones y lockfiles del producto. ADR-0015 complementa el monolito modular de ADR-0002 y respeta ADR-0003/0004/0006/0008/0010/0013/0014. Los permisos, tipos y aceptación permanecen en SPEC-CAT-001; ADR-0005/0007 y el metamodelo futuro siguen diferidos.

Persisten los límites de operación ya documentados: Keycloak/H2 y claves locales de desarrollo, ausencia de validación productiva de recuperación/carga/disponibilidad y gobierno manual de membresías. La cobertura de interoperabilidad se limita a Keycloak y al proveedor controlado de fallos. El override de Vite debe revisarse con el siguiente parche oficial de Angular build. El análisis completo del SBOM JVM/IdP y la inconsistencia de rangos de GHSA-f8m4-v488-rmrm siguen pendientes según la [revisión anterior](revision-tecnica-final-i1.md); no se configura SAML. Esta refactorización no modifica esas dependencias ni acredita preparación productiva.

La API GitHub continúa respondiendo Forbidden al consultar el PR; por tanto no se afirma edición de descripción, aprobación remota o estado de CI. Código y dictamen se publican por Git en la rama del PR #1. Main permanece sin modificar; la integración requiere decisión humana sobre el head publicado.
