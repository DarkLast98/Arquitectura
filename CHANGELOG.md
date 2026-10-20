# Historial de AIP

## I1 / Trabajo B — implementación para revisión — 2026-10-20 UTC

- Aprobación explícita de PLAN-I1-B v1.0 registrada; ADR aplicables aceptados únicamente para I1.
- Catálogo Angular/.NET modular/PostgreSQL con tres entidades y dos relaciones, OIDC configurable Keycloak, membresías/RLS y permisos por columna.
- Revisión optimista, integridad de archivo/relaciones y auditoría transaccional; contratos OpenAPI/JSON Schema y seis migraciones con checksums.
- Pruebas reales de dominio, API/base y navegador, OIDC negativo/JWKS/PAR, reinicio y migraciones; [evidencia](docs/delivery/evidencia-i1-trabajo-b.md) e [instrucciones locales](docs/delivery/ejecucion-local-i1.md).
- SPEC/ontología estable intactas; sin agentes, Neo4j, despliegue cloud ni fusión automática.

## En revisión — Trabajo B — 2026-10-19

- Rama `feature/i1-catalog-foundation` creada desde la última `main` inspeccionada (`25c4fcc`).
- Plan detallado de I1: módulos, agregados, contratos, persistencia, autorización, migraciones, pruebas y criterios de cierre.
- ADR-0013 OIDC configurable y ADR-0014 de versiones soportadas, ambos propuestos.
- Frontera explícita con metamodelo v0.2 y preservación del prototipo local en `work`.
- Sin código, cambios de definiciones estables, aceptación de ADR, despliegue o fusión automática.

## 0.1.0 — 2026-10-19

- Identificación del producto como AIP y registro de requisitos de origen usuario.
- Fundación de producto, arquitectura funcional y de referencia, ontología, paquetes, agentes y modelo regulatorio.
- Diseño de aislamiento por organización y ADR técnicos en estado propuesto.
- Backlog incremental, primer vertical slice y especificación del catálogo arquitectónico.
- Conservación del acuerdo de definir antes de implementar; índices iniciales redirigidos a las fuentes canónicas.

La sección 0.1.0 corresponde a la fundación documental. La aprobación de I1 se registra por separado; un commit no implica aceptación humana de su resultado.
