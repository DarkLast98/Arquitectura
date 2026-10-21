# Ejecución y verificación local de I1

Trabajo B implementa únicamente SPEC-CAT-001 v0.1.0 y PLAN-I1-B aprobado en PR #1. Un host HTTPS sirve Angular, sesión BFF y API .NET; PostgreSQL conserva los datos. Keycloak es un proceso local de desarrollo, sin despliegue cloud.

## Prerrequisitos

Linux con Node **24.19.0** / npm 11, SDK .NET **10.0.402**, PostgreSQL **17.11** (binarios servidor/cliente), Java **21**, Keycloak **26.8.1**, Python 3.12+, OpenSSL, Chromium y `certutil` (NSS). Angular 22.2.3, TypeScript 6.0.3, Npgsql 10.0.3 y OpenID Connect 10.0.13 se fijan en manifests/lockfiles. Ver soporte y fuentes en [ADR-0014](../decisiones/0014-versiones-soportadas-i1.md).

Keycloak: [release oficial 26.8.1](https://github.com/keycloak/keycloak/releases/tag/26.8.1), archivo `keycloak-26.8.1.tar.gz`, SHA-256 `4e2740b181faa2ea623a1c2ecf00855935311e546046e2f857615a47ed24cbe8`. Descargar, verificar antes de extraer y usar JDK 21. No ejecutar el entorno local como root.

Los scripts usan las herramientas ya disponibles bajo `/workspace/.aip-tools`. En otra máquina indicar sus ubicaciones mediante `AIP_DOTNET`, `AIP_PG_BIN`, `AIP_PG_LIB`, `KEYCLOAK_HOME`, `AIP_CHROMIUM`, `DOTNET_CLI_HOME` y `NUGET_PACKAGES`; el último directorio debe ser escribible. `AIP_PG_BIN` contiene `initdb`, `pg_ctl` y `psql`. Un PostgreSQL instalado normalmente no necesita `AIP_PG_LIB`. No es necesario instalar otra base si esos binarios están disponibles.

```bash
npm --prefix web ci --cache /tmp/aip-npm-cache
python3 -m pip install -r tests/requirements.txt
python3 scripts/prepare-i1.py
python3 scripts/trust-dev-ca.py
python3 scripts/verify-i1.py
```

La confianza de la CA requiere permiso de escritura en el almacén NSS del usuario. `trust-dev-ca.py` instala únicamente la CA pública local, necesaria para Chromium; no importa claves privadas. Node y .NET reciben un bundle de confianza normal; Python verifica mediante la CA local explícita. No se usan `ignoreHTTPSErrors`, `curl -k` ni callbacks que acepten cualquier certificado. En sistemas con otro navegador/almacén se debe confiar esa CA explícitamente. Los certificados duran 30 días; para renovarlos, detener estos servicios, eliminar solo `.local/i1/tls` y repetir preparación/confianza.

`verify-i1.py` necesita libres los puertos 7240–7243, 7443 y 7444. Inicia y detiene sus propios procesos de API y proveedor de fallos; puede reutilizar Keycloak ya disponible en 8443 con los fixtures de este entorno. Mantiene PostgreSQL local persistente en 55432. No termina procesos ajenos. Resultados y logs privados quedan en `.local/i1/`; solo el informe documental sanitizado se versiona.

## Demostración manual

Tras preparar dependencias, compilar y mantener estos dos comandos en terminales separadas:

```bash
python3 scripts/run-keycloak.py
```

```bash
npm --prefix web run build
# Restaurar/compilar con el SDK indicado por global.json:
dotnet restore src/Aip.Api --locked-mode
dotnet build src/Aip.Api --no-restore
python3 scripts/run-api.py
```

Abrir `https://localhost:7240`, pulsar «Iniciar sesión con el proveedor de identidad» e ingresar en Keycloak. Los cuatro usuarios sintéticos son `admin`, `architect`, `reader` y `outsider`. Sus contraseñas aleatorias están **solo** en `.local/i1/secrets.json`, propiedad `passwords`; consultarlas localmente sin copiarlas al chat, PR o capturas. Los tres primeros tienen roles respectivos `organization_admin`, `architect` y `reader` en A/B; `outsider` solo tiene lectura en B. No hay interfaz de administración de membresías.

Seguir el [escenario del slice](primer-incremento.md). Cerrar sesión puede requerir confirmar la pantalla del proveedor: no se persisten ID tokens para evitársela. Tras volver a ejecutar `run-api.py`, registros y revisión se conservan. La selección organizacional limpia formularios y resultados anteriores.

## Configuración y contratos

`AIP_CONFIG` apunta al JSON privado de configuración. `prepare-i1.py` genera:

| Perfil | BFF | Authority / ClientId |
| --- | --- | --- |
| `appsettings.json` | `https://localhost:7240` | `https://localhost:8443/realms/aip` / `aip-bff` |
| `appsettings-alt.json` | `https://localhost:7241` | `https://localhost:8443/realms/aip-alt` / `aip-bff-alt` |
| `appsettings-test.json` | `https://localhost:7242` | Proveedor de fallos HTTPS, exclusivamente pruebas. |

Claves requeridas: `ConnectionStrings:Aip`, `Authentication:PublicOrigin`, `Authentication:Oidc:Authority`, `ClientId`, `ClientSecret`, `Aip:KeysPath`; rutas de callback predeterminadas `/signin-oidc` y `/signout-callback-oidc`. Authority/PublicOrigin deben ser HTTPS. Registrar ambas rutas exactas en cada cliente del proveedor. No hay login por contraseña en AIP, cabeceras de suplantación ni fallback local. El usuario OIDC se vincula por `(issuer,sub)`; su primera autenticación puede crear sujeto interno, nunca membresías. Cambiar proveedor/cliente separa también la protección de cookies.

El catálogo documenta sus rutas en [OpenAPI](../../contracts/openapi/catalog.v1.json) y [JSON Schema 2020-12](../../contracts/schemas/catalog/0.1.0/catalog.schema.json). Se sirven en esas mismas rutas `/contracts/...`; `/openapi.json` redirige al documento canónico. La cookie de sesión `__Host-aip.session` es Secure/HttpOnly/Lax. Las escrituras requieren `X-CSRF-TOKEN` obtenido en `GET /api/session`; el formulario de logout usa el token antiforgery de esa misma sesión. No hay tokens OIDC en localStorage ni respuestas API.

## Persistencia y migraciones

La preparación crea la base local independiente `aip_i1` y el rol `aip_i1_app`, no propietario, sin superusuario ni BYPASSRLS. El propietario/migrador usa socket Unix en directorio privado; la aplicación usa TCP con SCRAM. No conectar esta preparación a una base productiva.

Migraciones SQL 001–005 siguen el plan; 006 reduce UPDATE a columnas mutables. Se aplican dentro de una transacción con advisory lock y checksum SHA-256 por archivo. Una migración aplicada no se edita: crear la siguiente. La aplicación no puede modificar `schema_migrations` y no ejecuta migraciones al arrancar. Los fixtures de organizaciones/sujetos/membresías se ejecutan después, solo en la preparación local; repetirla no restaura membresías revocadas ni altera registros existentes.

RLS forzada requiere contexto transaccional de sujeto y organización y membresía activa; el pool no conserva ese contexto después de commit/rollback. Unicidad de código incluye archivados, FKs de extremos incluyen organización, unicidad de relación activa es parcial y la auditoría se inserta en la misma transacción. Creación de relación bloquea extremos en orden UUID; archivo de entidad bloquea su fila antes de verificar relaciones activas. El contexto de RLS procede de la API autenticada: las credenciales de aplicación no se entregan al navegador.

## Límites y operación posterior

Este entorno usa datos sintéticos y claves de protección de cookies en un directorio privado sin cifrado externo; PostgreSQL se comunica por loopback y Keycloak usa H2 local. Una puesta en producción necesita gestión/cifrado de secretos y claves, proveedor/base del IdP operados, TLS de base, backup/restore, observabilidad, disponibilidad, límites y gobierno de membresías. Esas tareas no están autorizadas en I1. Tampoco lo están agentes, Neo4j o extensiones del metamodelo.

## Revisión de un commit en entorno limpio

Con el checkout sin cambios pendientes, los prerrequisitos instalados y libres 55432, 8443, 7240–7243, 7443/7444, ejecutar `python3 scripts/verify-clean-i1.py`. El wrapper exporta HEAD en un directorio temporal privado, crea un venv y cachés nuevos, copia Keycloak sin datos y prepara PostgreSQL/TLS/sesiones desde cero. Requiere permiso para confiar la CA en NSS; no omite TLS. Al terminar detiene PostgreSQL y los procesos de prueba creados, conserva los artefactos privados y anuncia la ruta de `clean-review.json` con el commit exacto.

`verify-i1.py` escribe RUNNING/PASS/FAIL por ejecución y deriva conteos de reportes TRX/JSON, incluyendo la prueba de checksum alterado sobre copias privadas. El wrapper añade auditorías npm/NuGet y `pip check`; una auditoría fallida no produce dictamen PASS. El [dictamen de integración](revision-tecnica-final-i1.md) explica el alcance de las consultas adicionales de seguridad y contiene la evidencia sanitizada. AJV 8.20.0/Vite 8.3.3 corrigen los avisos detectados; el resto de versiones del producto permanece fijado como antes.
