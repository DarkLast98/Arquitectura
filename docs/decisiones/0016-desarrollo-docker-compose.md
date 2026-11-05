# ADR-0016 — Docker Compose para desarrollo local de I1

- **Estado:** aceptada exclusivamente para la implementación de entorno Docker local de I1.
- **Fecha:** 2026-11-05 (America/Bogota).
- **Origen:** solicitud expresa del usuario: «migra a la estructura para que se pueda desplegar en docker».
- **Alcance:** empaquetado/desarrollo local. No autoriza producción, I2 o agentes.

## Contexto

I1 es una API/BFF .NET con Angular compilado, PostgreSQL y OIDC Keycloak. La preparación Linux original utiliza rutas y procesos dependientes del host; el usuario necesita un arranque reproducible desde Windows/Docker Desktop.

## Decisión

- Crear `compose.yaml`, Dockerfile multietapa Angular/.NET, imagen de herramientas, preparación idempotente y migración de PostgreSQL por proceso de una sola ejecución.
- Mantener Angular bajo el mismo origen HTTPS que API/BFF; conservar cookies Secure/HttpOnly, CSRF, OIDC Code + PKCE y contratos.
- Publicar en loopback `https://localhost:7240` y `https://auth.localhost:8443`. Utilizar el alias DNS Docker `auth.localhost` para que la autoridad OIDC conserve **el mismo issuer externo e interno**.
- Mantener el rol `aip_i1_app` sin superusuario/BYPASSRLS ni permisos de migración. Aplicar migraciones 001–006 sin editarlas, con sus checksums y una tarea de inicialización separada.
- Credenciales y certificados aleatorios en volumen privado, sin incorporarlos a Git. Persistir por separado PostgreSQL, Keycloak y claves BFF.
- Restringir publicación de puertos al loopback. Ejecutar con datos sintéticos.

## Alternativas

- Instalación manual en WSL2: sigue documentada, pero es menos reproducible.
- Kubernetes o cloud: fuera de alcance para I1 y aumenta complejidad operativa.

## Riesgos / validación

El volumen de inicialización es compartido entre contenedores; Keycloak usa modo desarrollo y H2. No hay gestor externo de secretos ni backup. La confianza en la CA local requiere aprobación del usuario del equipo. El DNS `auth.localhost` debe verificarse desde Windows y desde los contenedores.

**Criterio de validación:** build, arranque, OIDC real, RLS entre dos organizaciones, operaciones del catálogo, migraciones idempotentes y persistencia tras reinicio. Los scripts existentes de suite Linux no equivalen a las pruebas de Compose. La comprobación CI de build/arranque/smoke fue **PASS** el 2026-11-05 ([ejecución #42072985825](https://github.com/DarkLast98/Arquitectura/actions/runs/42072985825)). Permanecen pendientes login real, permisos/RLS y ejecución local en Windows; sin aprobación productiva.
