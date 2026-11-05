# AIP I1: ejecución local con Docker Compose

**Alcance:** desarrollo y prueba local con datos sintéticos; no es una configuración de producción. Complementa la [ejecución nativa en Linux](ejecucion-local-i1.md) y no altera SPEC-CAT-001 ni sus migraciones.

## Prerrequisitos

- Docker Desktop con motor Linux/WSL2 en Windows, o Docker Engine + plugin Compose en Linux.
- Git y acceso a las imágenes base de Docker Hub, Microsoft Container Registry y Quay.
- Puertos libres `7240` y `8443` del host. Solo se publican en `127.0.0.1`.
- Memoria suficiente para construir Angular/.NET y arrancar Keycloak y PostgreSQL.
- En el navegador, `auth.localhost` debe resolver a `127.0.0.1`. En Windows, si no sucede, añadir `127.0.0.1 auth.localhost` al archivo `C:\Windows\System32\drivers\etc\hosts` con permisos administrativos.

La identidad visible en el navegador y la usada por la API es **la misma**: `https://auth.localhost:8443/realms/aip`. Dentro de la red Compose, el alias `auth.localhost` apunta al contenedor Keycloak. Mantener ese issuer evita desactivar validaciones OIDC y es distinto del issuer del entorno Linux nativo.

## Primer arranque

Desde la raíz del repositorio en PowerShell, WSL2 o Linux:

```bash
docker compose up --build -d
docker compose ps
docker compose logs -f api keycloak
```

La primera compilación puede tardar y Keycloak necesita completar la importación del realm. No se necesita instalar Node, .NET, PostgreSQL, Java o Python en el host: Compose utiliza imágenes. El archivo `compose.yaml` incluye:

| Servicio | Responsabilidad |
| --- | --- |
| `setup` | Genera, solo al faltar, credenciales aleatorias, CA/certificados locales, configuración API y realm OIDC en el volumen privado `aip-state`. |
| `postgres` | PostgreSQL 17 con volumen persistente, sin puerto publicado al host. |
| `migrate` | Crea el rol de aplicación no propietario, aplica migraciones existentes con checksums, y carga dos organizaciones y usuarios sintéticos; termina. |
| `keycloak` | Keycloak de desarrollo y realm `aip`; persiste estado en `keycloak-data`. |
| `api` | Compilación multietapa de Angular y .NET, BFF de origen único por HTTPS; mantiene claves de cookies en `aip-state`. |

`setup` y `migrate` deben aparecer como terminados correctamente; que no queden ejecutándose es normal. Si uno falla, inspeccionar `docker compose logs setup migrate postgres`.

## Certificado de desarrollo en Windows

El entorno **no desactiva la validación TLS**. Para que el navegador confíe en la CA generada, después del arranque, en PowerShell:

```powershell
New-Item -ItemType Directory -Force .local | Out-Null
docker compose cp api:/state/tls/ca.crt .local/aip-docker-ca.crt
Import-Certificate -FilePath .local/aip-docker-ca.crt -CertStoreLocation Cert:\CurrentUser\Root
```

Importa **solo** la CA de este entorno local; confía en ella únicamente si estás ejecutando tu propio repositorio y has revisado los scripts. Remuévela del almacén de certificados del usuario cuando dejes de utilizar el entorno. En otros sistemas, importa la CA en el almacén del navegador/sistema correspondiente.

Abre [AIP](https://localhost:7240) e inicia sesión vía Keycloak. Usuarios sintéticos: `admin`, `architect`, `reader`, `outsider`. Para consultar **solo** la contraseña del usuario `admin` en tu consola local:

```bash
docker compose run --rm --no-deps setup python3 -c "import json; print(json.load(open('/state/secrets.json'))['passwords']['admin'])"
```

No copies `secrets.json` a commits, capturas, incidencias o chats. Ninguna contraseña está escrita en `compose.yaml`.

## Verificación

```bash
docker compose ps
docker compose run --rm --no-deps setup python3 scripts/docker_smoke.py
```

El smoke usa TLS validado y revisa `/health`, OpenAPI, sesión anónima y el issuer de Keycloak. También es necesario validar manualmente login, escritura/consulta de catálogo, selección organizacional y persistencia tras `docker compose restart api postgres`. Este smoke **no sustituye** las 67 pruebas funcionales y de seguridad I1: la suite Linux `verify-i1.py` existente está diseñada para su otro entorno local y aún no se ha adaptado a Compose.

## Vida útil y limpieza

```bash
docker compose logs -f api
docker compose restart api
docker compose down
```

`docker compose down` conserva los tres volúmenes. `docker compose down -v` **elimina permanentemente** la base, el estado del IdP, los usuarios, las credenciales y la CA; solo usarlo cuando quieras reiniciar una demostración vacía. No mezclar los volúmenes con el PostgreSQL de la preparación nativa `.local/i1`.

Los certificados duran 30 días. Para renovarlos sin borrar la base de datos, detén los servicios, elimina **solo** `/state/tls` desde un contenedor efímero de herramientas, ejecuta nuevamente `setup`, recrea `api` y `keycloak` e importa la CA nueva. Evita cambiar el issuer OIDC sobre un estado persistente sin plan de migración.

## Límites y riesgos conocidos

- Configuración **solo** para desarrollo en equipo local. Keycloak utiliza `start-dev` y H2 embebida; los secretos y claves viven en un volumen compartido accesible a contenedores de desarrollo. No hay gestor externo de secretos, backup, cifrado de volúmenes ni operación del IdP.
- La CA es propia del entorno; importarla amplía la confianza del usuario. Revisar fecha de vencimiento y limpiar al desinstalar.
- Los tags de imagen se fijan por versión mayor/versión de producto, no por digest inmutable. Para un release repetir validación de seguridad y fijar digests.
- OIDC `auth.localhost` exige resolución DNS consistente en navegador y Docker. No se sustituye por HTTP, por un issuer distinto o por TLS sin verificación.
- No se han agregado agentes, Neo4j, I2 ni despliegue cloud/productivo.

## Estructura agregada

```text
compose.yaml
.dockerignore
docker/Dockerfile.api
docker/Dockerfile.tools
scripts/docker_setup.py
scripts/docker_db_init.py
scripts/docker_smoke.py
```

**Evidencia CI 2026-11-05:** [GitHub Actions #42072985825](https://github.com/DarkLast98/Arquitectura/actions/runs/42072985825) ejecutó correctamente `docker compose config --quiet`, `docker compose up --build -d` y `docker compose run --rm --no-deps setup python3 scripts/docker_smoke.py` para `ea9ef066de0c189e03a9123cede6a3107d515946`. Se verificó construcción, inicio de servicios, migración con fixtures, TLS, API, contrato, sesión anónima y issuer OIDC. **No sustituye** las pruebas de login y autorización/aislamiento de dos organizaciones, persistencia funcional o una ejecución manual en Windows; pendientes antes de aprobar su uso generalizado.
