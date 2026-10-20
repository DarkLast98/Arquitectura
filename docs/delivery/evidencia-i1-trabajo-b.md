# Evidencia de I1 — Trabajo B

**Alcance aprobado:** PLAN-I1-B v1.0 de `469f2dc`, PR #1; usuario «aprobado, implementa el PR». Autorización registrada antes de los commits de código en `6a187c1`. Solo I1; revisión humana y fusión pendientes.

**Resultado final: PASS**, ejecución terminada 2026-10-20T03:43:11Z. Código y pruebas verificados en `a07a65c32acb8ae03d0d05f26d73e81ad2e4998a` (la documentación de cierre se añade después). Este informe no reutiliza resultados del prototipo `work/52c653d`.

## Entorno y reproducción

Linux, Node 24.19.0/npm 11.9, Angular 22.2.3, TypeScript 6.0.3, SDK .NET 10.0.402/runtime 10.0.13, Npgsql 10.0.3, manejador OIDC 10.0.13, PostgreSQL 17.11, Keycloak 26.8.1/JDK 21 y Chromium del entorno. Dependencias bloqueadas en Git. [Preparación y ejecución](ejecucion-local-i1.md).

Comando global: `python3 scripts/verify-i1.py`. Usa PostgreSQL real y su rol no propietario, TLS verificado, Keycloak real para los casos positivos y un proveedor HTTPS independiente solo para respuestas deliberadamente inválidas. La API no incorpora una opción para omitir validaciones. Los procesos API/proveedor de fallos del verificador se detienen al terminar; PostgreSQL persiste. No se despliega infraestructura.

| Verificación | Comando / evidencia | Resultado final |
| --- | --- | --- |
| Dependencias Node | `npm ci --cache /workspace/.aip-tools/npm-cache --ignore-scripts` en `web/`; integridades del lockfile. | Completado. |
| Backend y dominio | Restore `--locked-mode` y build `--no-restore` de `src/Aip.Api`. | PASS. |
| Dominio independiente | `dotnet test tests/Aip.Catalog.Domain.Tests --no-restore`; proyecto BCL sin HTTP/Npgsql/OIDC. | 5 casos, PASS. |
| UI | `npm run build` en `web/`. | PASS. |
| API/base | `python3 -m unittest discover -s tests -p test_catalog.py -v`. | 11 casos, PASS. |
| Contratos | `npm run test:contracts` en `web/`, SwaggerParser y AJV; respuestas reales validadas además por jsonschema Python. | OpenAPI 3.1 y 12 esquemas 2020-12. |
| Navegador | Playwright, proyecto `setup` y después `catalog --no-deps`. | 19 casos, PASS. |
| Reinicio / fallo seguro / migraciones | Pasos adicionales de `scripts/verify-i1.py`. | PASS. |

## Trazabilidad de aceptación

| Criterios | Comprobación ejecutable |
| --- | --- |
| AC-01/02 | API `test_01` normaliza código/nombre, comprueba campos de sistema y contratos; `test_02` unicidad por organización/tipo incluyendo archivados. Límites Unicode y trim también en dominio. |
| AC-03 | API `test_03`: supports/uses, dirección, tipos, duplicados y extremos; recorrido Angular crea los tres tipos y ambas relaciones. |
| AC-04 | API `test_04`: UUID ajeno, listado/conteo, edición, extremos y auditoría no revelan recursos de otra organización; navegador cambia A/B y comprueba 404. |
| AC-05 | API `test_05` y navegador: anónimo 401, sin membresía 403, lector sin escritura y auditoría solo admin; CSRF obligatorio. |
| AC-06 | API `test_06`: If-Match obligatorio/exacto, dos PATCH paralelos dan 200/412; navegador conserva formulario y exige reconciliar. |
| AC-07/08 | API `test_07`: archivo bloqueado por relaciones, secuencia de archivo válida e historia; `test_08`: seis carreras creación de relación/archivo admiten solo resultados coherentes. |
| AC-09 | API `test_09`: trigger de fallo sintético de auditoría provoca 500 y rollback de entidad; se retira en finally. |
| AC-10 | API `test_10`: SQL bajo rol real sin superusuario/BYPASSRLS; misma conexión A→B→sin contexto, escritura ajena/lector denegada, columnas inmutables y auditoría sin UPDATE. |
| AC-11 | API `test_11`: límites, filtros, páginas, tipos, errores y respuesta contractual; UI página entidades/relaciones/auditoría en lotes de 20. |
| AC-12 | Playwright: texto con HTML malicioso se muestra sin ejecutarlo, intento de escritura como lector, conflicto y limpieza de contexto al cambiar organización. |
| AC-13 | Verificador crea entidad, detiene/reinicia su API y comprueba representación/revisión idénticas usando la base y sesión persistentes. |
| AC-14 | SwaggerParser valida OpenAPI, AJV compila 12 definiciones y contrasta ejemplos/respuesta 401 real; Python valida entidades/relaciones/páginas reales con formatos. |

| OIDC | Comprobación ejecutable |
| --- | --- |
| 01 | Cuatro usuarios se autentican en Keycloak mediante navegador; S256 exigido por cliente, PAR anunciado usado, cookie Secure/HttpOnly/Lax; logout antiforgery y regreso desde el IdP. |
| 02 | Proveedor HTTPS de fallos altera state, firma, issuer, audiencia, expiración, nbf y nonce (incluida ausencia). Sin sesión; no crea sujetos internos tras un protocolo inválido. Retorno externo rechazado. |
| 03 | Segundo realm/ClientId real con mismo email autentica sin heredar membresías. Proveedor independiente con mismo sub que admin tampoco se fusiona. Solo cambia perfil, mismo binario. |
| 04 | ClientSecret vacío impide arranque; Authority inaccesible produce error seguro y catálogo 401. Sin contraseña ni header de fallback. |
| 05 | Usuario autenticado sin membresía recibe 403; revocación de membresía afecta sesión existente. Sujeto revocado no recibe cookie al autenticar. |
| 06 | CSRF obligatorio, API devuelve Problem Details 401/403 sin redirección HTML; localStorage vacío y sesión reducida a identidad. Secretos/sesiones ignorados por Git y verificación de valores privados sin imprimirlos. |
| 07 | Rotación real de firma Keycloak: prioridad mayor y comprobación de cambio de clave activa; manejador renueva JWKS y permite nuevo acceso. Si aparece un kid aún no conocido puede ser necesario reintentar el acceso una vez; se mantiene el rechazo seguro inicial. PAR permanece habilitado cuando se anuncia. |

## Revisiones y límites

Revisión de cierre: 93 archivos versionados/propuestos y siete logs privados comprobados sin coincidencias de los secretos generados ni JWT en logs; 44 documentos Markdown sin enlaces locales rotos. No se publican los valores buscados, logs, sesiones, certificados ni bases locales. `git diff --check` sin errores.

SPEC-CAT-001 y `docs/architecture/ontologia.md` se comparan contra `main/25c4fcc`, sin diff. No hay fuente v0.2 remota publicada para afirmar compatibilidad; [frontera](compatibilidad-metamodelo-v02.md). Los cinco tipos y esquema 0.1.0 se conservan; pruebas rechazan tipos/versiones futuros.

Los tests positivos usan servicios reales; el proveedor de fallos no sustituye Keycloak como prueba de éxito. La cobertura demuestra el entorno probado, no todos los IdP del mercado, una operación productiva ni certificación regulatoria. No se realizan pruebas de carga o recuperación de desastres.

Concreciones frente a la estructura orientativa: biblioteca de dominio independiente, integración API/base Python, contratos Node y navegador Playwright; módulos pequeños sin carpetas vacías. Migración 006 refuerza privilegios de columnas y no altera migraciones ya aplicadas. La API coordina comandos, membresía y transacción; el módulo Audit es el escritor de eventos. Más detalle en el resultado añadido al [plan](plan-implementacion-i1-trabajo-b.md).

La revisión final corrigió la vinculación de identidad para ejecutarla tras validar nonce/state, conservando solo metadatos previamente verificados entre eventos del manejador. La prueba de sujeto revocado y la comprobación de que las respuestas inválidas no aprovisionan sujetos cubren ese límite.

Producción requiere operación del IdP y PostgreSQL, protección/cifrado de claves y secretos, backup/restore, observabilidad, límites y administración de membresías. Agentes, Neo4j, paquetes dinámicos, datos financieros reales y conclusiones normativas quedan fuera de I1.
