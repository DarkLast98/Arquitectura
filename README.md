# AIP — Architecture Intelligence Platform

Plataforma propia de gestión agéntica de arquitectura, desde la estrategia empresarial hasta la arquitectura de solución. Producto comercializable, inicialmente orientado al sector financiero, con un Core transversal y paquetes de conocimiento por industria y jurisdicción.

**I1 implementado para revisión en PR #1:** catálogo manual Angular → API .NET modular → PostgreSQL, con OIDC configurable, aislamiento por organización, revisión optimista y auditoría transaccional. La aprobación «aprobado, implementa el PR» cubre PLAN-I1-B v1.0; la fusión y los incrementos siguientes siguen pendientes.

El catálogo conserva tres tipos de entidad y dos relaciones de SPEC-CAT-001 v0.1.0. Keycloak 26.8.1 proporciona identidad local de desarrollo mediante Code + PKCE/PAR; AIP no recibe contraseñas. No hay agentes, Neo4j, extensiones del metamodelo ni despliegue cloud.

## Ejecutar y verificar

Prerrequisitos y configuración privada en [ejecución local](docs/delivery/ejecucion-local-i1.md). Con las herramientas instaladas:

```bash
npm --prefix web ci --cache /tmp/aip-npm-cache
python3 -m pip install -r tests/requirements.txt
python3 scripts/prepare-i1.py
python3 scripts/trust-dev-ca.py
python3 scripts/verify-i1.py
```

El verificador compila y ejecuta dominio, API/PostgreSQL, navegador, contratos, persistencia tras reinicio y fallos de OIDC. Usa únicamente datos sintéticos y certificados verificados. Secretos, sesiones y bases locales quedan fuera de Git. [Evidencia y límites de la entrega](docs/delivery/evidencia-i1-trabajo-b.md).

## Lectura recomendada

| Tema | Documento |
| --- | --- |
| Reglas para Codex, objetivos y autorización | [AGENTS](AGENTS.md), [GOALS](GOALS.md), [estado](docs/00-estado.md) |
| Plan y resultado de I1 | [PLANS](PLANS.md), [PLAN-I1-B](docs/delivery/plan-implementacion-i1-trabajo-b.md), [slice](docs/delivery/primer-incremento.md), [backlog](docs/delivery/backlog.md) |
| Contrato estable y ejecutable | [SPEC-CAT-001](docs/specs/catalogo-arquitectonico.md), [OpenAPI](contracts/openapi/catalog.v1.json), [JSON Schema](contracts/schemas/catalog/0.1.0/catalog.schema.json) |
| Producto y mercado | [Visión](docs/product/vision.md), [actores](docs/product/actores.md), [problema](docs/product/problema-mercado.md), [alcance financiero](docs/product/alcance-financiero.md), [requisitos](docs/product/requisitos.md) |
| Arquitectura | [Funcional](docs/architecture/funcional.md), [referencia](docs/architecture/referencia.md), [ontología](docs/architecture/ontologia.md), [paquetes](docs/architecture/paquetes.md), [aislamiento](docs/architecture/aislamiento-organizaciones.md) |
| Decisiones y coordinación | [ADR](docs/decisiones/README.md), [compatibilidad con v0.2](docs/delivery/compatibilidad-metamodelo-v02.md) |
| Diseño futuro | [Agentes](docs/architecture/agentes.md), [regulación](docs/architecture/regulatorio.md) |

## Estructura implementada

```text
src/Aip.Api/
  Program.cs                      # Composición, HTTP y transacción autorizada
  Modules/IdentityAccess/          # OIDC, sujetos internos, sesión y membresías
  Modules/Catalog/
    Public/                       # Entrada HTTP y precondiciones
    Domain/                       # Biblioteca BCL: agregados e invariantes
    Application/                  # Casos de uso y puerto transaccional
  Modules/Audit/Application/       # Escritura de eventos atómica
  Infrastructure/Postgres/        # Adaptador Npgsql y migraciones 001–006
web/src/app/                      # Catálogo Angular, relaciones y auditoría
contracts/{openapi,schemas}/       # API v1 y esquema 0.1.0
tests/                            # Dominio .NET, API/PostgreSQL Python, fallos OIDC
web/e2e/                          # Playwright contra servicios reales
scripts/                          # Preparación, ejecución y verificación locales
docs/                             # Producto, arquitectura, ADR y entrega
```

## Dirección y límites

Angular 22.2.3 / TypeScript 6.0.3, Node 24.19.0, .NET 10.0.13 (SDK 10.0.402), PostgreSQL 17.11, OpenAPI 3.1 y JSON Schema 2020-12. Los ADR aplicables están aceptados **para I1** con evidencia de autorización. Python de este incremento solo ejecuta herramientas/pruebas; la arquitectura agéntica, paquetes y regulación siguen como diseño futuro.

La ejecución local no habilita producción. Gestión de claves/secretos, operación del IdP, recuperación y gobierno de organizaciones requieren trabajo adicional acordado. La especificación y ontología estables se conservan byte por byte; sus notas históricas de propuesta se interpretan junto al registro posterior de aprobación. [CHANGELOG](CHANGELOG.md) conserva la evolución en Git.
