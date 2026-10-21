# Dictamen técnico final — PR #1 / I1

**Fecha:** 2026-10-21, America/Bogota. **Dictamen:** **LISTO PARA INTEGRAR I1**, desde la validación técnica ejecutada. No quedan defectos bloqueantes identificados dentro de I1. La fusión sigue condicionada a una decisión humana; no se ha ejecutado ni habilitado una fusión automática. No autoriza producción ni I2.

**Código verificado:** `2a27cb828e788fb01ef64ba6ba522abc8ec34aaa`, posterior a las correcciones de revisión `10b4684` y `c5dbd76`. Base `main/25c4fccdec6304f34b3e068a93740bfbb830f20b`. Los commits posteriores de esta revisión solo añaden documentación/evidencia. La aprobación de arquitectura y alcance procede del usuario; este dictamen evalúa su implementación, no crea nuevas decisiones de arquitectura.

## Ejecución limpia y evidencia

Comando reproducible: `python3 scripts/verify-clean-i1.py`, con el checkout sin cambios pendientes y los prerrequisitos de la [guía local](ejecucion-local-i1.md). El script exporta **HEAD** mediante `git archive`, no crea worktrees ni modifica el checkout. Reinstala npm/Python, usa cachés NuGet/npm vacías y una copia de Keycloak sin datos. Genera PostgreSQL, certificados, secretos y sesiones nuevos. Verifica que el catálogo y la auditoría empiecen vacíos; detiene los servicios que crea.

Resultado: **PASS**, terminado **2026-10-21 17:23:28 America/Bogota** (`2026-10-21T22:23:28Z`). **41 pruebas pasan: 6 de dominio, 16 de API/PostgreSQL y 19 de navegador**, sin casos omitidos o inestables. Build .NET sin errores/advertencias, build Angular, contratos servidos y versionados iguales, reinicio con persistencia, reaplicación de seis migraciones y rechazo de checksum alterado correctos.

La [evidencia JSON sanitizada](evidence/i1-final-2026-10-21.json) registra commit, hash SHA-256 del archivo fuente, condiciones iniciales, casos de prueba, resultados, rol/tablas PostgreSQL y hashes de los reportes originales. La ejecución se conserva en `/tmp/aip-i1-review-7wy7c831/clean-review.json`; los reportes TRX/JSON originales están en `repo/.local/i1/test-results/` bajo ese directorio. Esas rutas son locales y temporales; los resultados sanitizados permanecen versionados. No se publican cookies, secretos, bases, certificados ni logs privados. Es una ejecución local en el entorno de revisión, no GitHub Actions.

La suite original de `e60c133` también se repitió en una exportación limpia: sus 35 pruebas pasaron. Las pruebas adicionales y la revisión descubrieron los defectos de abajo; ese resultado original no se usa como sustituto de la validación final ampliada.

## Hallazgos corregidos

| ID | Hallazgo y evidencia anterior | Corrección dentro de I1 | Verificación final |
| --- | --- | --- | --- |
| F-01 | `architect` y `reader` obtenían 200 en `/audit-events/` y `/AUDIT-EVENTS`, aunque la ruta canónica daba 403. RLS impedía recuperar eventos; no se observó exposición de datos. | Permiso administrativo asociado al endpoint resuelto, independiente de la escritura de la URL. | API `test_12`: variantes 403 para lector/arquitecto y 200 para admin; SQL directo conserva defensa de RLS. |
| F-02 | Nombre/descripción con NUL y nombre con surrogate aislado producían 500; reproducción registrada en JSON. | Validación Unicode antes de persistencia; campos no representables dan 422, clave JSON malformada 400 y filtro inválido 400. Contratos reflejan texto representable. | Dominio, API `test_13` y AJV; sin mutación ni incremento de revisión ante rechazo. |
| F-03 | `npm audit` reportaba tres paquetes afectados: Vite (incluido aviso alto), AJV (moderado) y su propagación a Angular build. | AJV 8.20.0 y override acotado Vite 8.3.3; Angular conserva 22.2.3. Lockfile regenerado y reinstalado. No se ejecutó `audit fix --force` ni se cambió de framework. | Build y navegador pasan; `npm audit` final: 0 avisos en todas las severidades. |
| F-04 | En una instalación Python limpia, `date-time` no estaba registrado en `FormatChecker`; la validación podía omitirlo. | Dependencias transitivas fijadas y `rfc3339-validator` explícito; el test falla si falta el formato y rechaza una fecha inválida. | API `setUpClass`/`test_13`, contratos y `pip check` correctos. |

Se reforzó además el aseguramiento: conteos derivados de TRX/JSON, resultado RUNNING/PASS/FAIL actualizado por ejecución, rechazo de suites vacías/omitidas/inestables y prueba negativa de checksum sobre copias privadas. Los archivos de migración aplicados permanecen intactos. Se amplió evidencia de código archivado, FK entre organizaciones, páginas y rollback de edición/archivo si falla auditoría.

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
| AC-10 | API `test_10`/`test_12`, snapshot PostgreSQL: conexión A→B→sin contexto, reader sin escritura, RLS forzada y rol real no propietario/sin superusuario/BYPASSRLS. | PASS |
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

PostgreSQL observado: **17.11**, `aip_i1_app` sin superusuario, BYPASSRLS, CREATEDB o CREATEROLE; ninguna tabla es suya. Las seis tablas privadas tienen RLS habilitada y forzada; el registro global de tipos es de lectura y no contiene datos organizacionales. La aplicación no tiene UPDATE/DELETE de auditoría ni UPDATE de identidad/código/extremos. Las credenciales de migración se usan aparte de la API.

Dependencias: npm **0 avisos**, NuGet de API y pruebas —incluidas transitivas— **0 avisos**, ocho versiones Python sin avisos en sus respuestas PyPI consultadas y `pip check` correcto. Estas consultas son evidencia fechada de sus fuentes, no una garantía de ausencia de vulnerabilidades desconocidas. Versiones y fuentes se conservan en JSON y [ADR-0014](../decisiones/0014-versiones-soportadas-i1.md).

## ADR y alcance

| ADR aceptado para I1 | Contraste realizado |
| --- | --- |
| 0002 / 0004 | Un host .NET modular, biblioteca de dominio sin HTTP/Npgsql/OIDC; transacción compartida con escritor de auditoría. Sin microservicios. |
| 0003 / 0014 | Angular/TypeScript/Node y SDK/runtime soportados fijados; parches de seguridad de herramientas limitados y verificados. |
| 0006 / 0010 | PostgreSQL transaccional real, claves/FKs con organización, permisos de rol y RLS, concurrencia y auditoría. |
| 0008 | API v1/esquema 0.1.0; OpenAPI y JSON Schema versionados, generables y contrastados con HTTP. |
| 0009 | Documentos/ADR/evidencia en Git; aprobación y versiones trazables. |
| 0011, solo fronteras | Core fijo sin paquetes dinámicos; exactamente tres entidades y dos relaciones. No se acepta ni implementa el motor futuro. |
| 0013 | Manejador OIDC oficial, proveedor configurable y sujeto por `(issuer,sub)` sin membresías automáticas; controles anteriores comprobados. |

SPEC-CAT-001 y ontología son idénticas a `main/25c4fcc`; migraciones 001–006 también conservan sus bytes originales. No hay fuente v0.2 remota publicada para una comparación semántica. ADR-0005/0007 y capacidades de agentes/grafo siguen diferidos. Los epígrafes históricos «Decisión propuesta» se interpretan junto al estado y la evidencia posterior de aprobación, sin aceptar capacidades nuevas.

## Riesgos residuales y condición de integración

- **Operación productiva diferida:** claves locales sin cifrado externo, Keycloak/H2 de desarrollo, gobierno manual de membresías y ausencia de validación de recuperación, carga o alta disponibilidad. No bloquea integrar el slice local aprobado; sí impide afirmar preparación productiva.
- **Herramientas:** override de Vite limitado a 8.3.3 para eliminar los avisos; revisar/retirar al adoptar un Angular build que ya incluya el parche. La compilación y el recorrido aprobado pasan con esa combinación.
- **Interoperabilidad:** se verifican Keycloak y un proveedor controlado de fallos, no todos los IdP. Una rotación de JWKS puede requerir un reintento seguro de acceso, como documenta la evidencia anterior.
- **Cobertura de avisos del IdP:** se leyeron diez avisos del índice oficial de Keycloak; no es un análisis exhaustivo del SBOM JVM. OSV fue inaccesible. GHSA-f8m4-v488-rmrm muestra rangos affected/patched inconsistentes; la importación SAML no está configurada en I1. Resolver esa inconsistencia y ampliar el análisis antes de incorporar SAML u operar el IdP en producción.
- **Integración remota:** Git funciona, pero el proxy devuelve 403 al túnel hacia `api.github.com`, aunque el dominio figura en la configuración guardada. Por eso la descripción/estado de revisión de GitHub no se pueden actualizar por REST en esta ejecución. El dictamen y los cambios se publican mediante commits en la rama del PR; no se simula una aprobación remota ni un check de CI.

Condición final: decisión humana de integrar el **head publicado** tras leer esta matriz y sus límites. Cualquier cambio posterior de código exige revisar su impacto y ejecutar las comprobaciones afectadas. Este dictamen no inicia I2 ni autoriza una fusión automática.
