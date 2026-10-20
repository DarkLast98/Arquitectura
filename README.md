# AIP — Architecture Intelligence Platform

Plataforma propia de gestión agéntica de arquitectura, desde la estrategia empresarial hasta la arquitectura de solución. Producto comercializable, inicialmente orientado al sector financiero, con un Core transversal y paquetes de conocimiento por industria y jurisdicción.

**Versión documental: 0.1.0 · Fase: fundación documental · Implementación: no autorizada.**

La visión procede del usuario. Los diseños, prioridades y ADR son propuestas para revisión; este repositorio todavía no contiene aplicaciones, agentes ni infraestructura. La primera entrega sugerida es un catálogo de entidades y relaciones arquitectónicas aislado por organización.

## Lectura recomendada

| Tema | Documento |
| --- | --- |
| Reglas permanentes para Codex | [AGENTS.md](AGENTS.md) |
| Objetivos, restricciones y origen de requisitos | [GOALS.md](GOALS.md) |
| Plan incremental y condiciones para comenzar | [PLANS.md](PLANS.md) |
| Acuerdos y asuntos pendientes | [Estado](docs/00-estado.md) |
| Visión, actores y mercado | [Visión](docs/product/vision.md), [actores](docs/product/actores.md), [problema de mercado](docs/product/problema-mercado.md) |
| Sector financiero y requisitos | [Alcance financiero](docs/product/alcance-financiero.md), [requisitos](docs/product/requisitos.md) |
| Arquitectura | [Funcional](docs/architecture/funcional.md), [referencia](docs/architecture/referencia.md), [ontología](docs/architecture/ontologia.md) |
| Extensibilidad y aislamiento | [Paquetes](docs/architecture/paquetes.md), [organizaciones](docs/architecture/aislamiento-organizaciones.md) |
| Inteligencia y regulación | [Agentes](docs/architecture/agentes.md), [modelo regulatorio](docs/architecture/regulatorio.md) |
| Decisiones | [Índice de ADR](docs/decisiones/README.md) |
| Próxima entrega | [Backlog](docs/delivery/backlog.md), [vertical slice](docs/delivery/primer-incremento.md), [especificación del catálogo](docs/specs/catalogo-arquitectonico.md) |
| Trabajo B en revisión | [Plan detallado de I1](docs/delivery/plan-implementacion-i1-trabajo-b.md), [convivencia con metamodelo v0.2](docs/delivery/compatibilidad-metamodelo-v02.md) |

Trabajo B prepara I1 en `feature/i1-catalog-foundation` desde `main`, con OIDC configurable propuesto. Esta rama mantiene la entrega documental hasta aprobar el plan; el prototipo anterior de `work/52c653d` permanece separado.

## Dirección tecnológica inicial

Angular y TypeScript; backend .NET modular; agentes Python; PostgreSQL transaccional; OpenAPI y JSON Schema para contratos; Markdown, Mermaid y ADR para arquitectura. Neo4j se evaluará como posible proyección de conocimiento. Todas las decisiones técnicas están registradas como propuestas, sin versiones de herramientas ni proveedor de despliegue aprobados.

## Límites de esta entrega

Solo documentación. No se han creado esqueletos de aplicaciones, dependencias, contenedores, despliegues ni agentes. No se atribuyen obligaciones a normas no verificadas. El diseño exige aislamiento entre organizaciones y aprobación humana de conclusiones regulatorias.

Para iniciar el desarrollo se necesita tu aprobación del incremento y sus ADR aplicables, y resolver los bloqueos de [PLANS.md](PLANS.md). El alcance de esa aprobación quedará registrado en el estado.

## Versionado

Los documentos Markdown se mantienen en Git. `0.1.0` identifica esta fundación, no una versión ejecutable del producto ni una aprobación. [CHANGELOG.md](CHANGELOG.md) resume cambios documentales. Las decisiones conservan identificadores y su historia; las versiones de API, esquemas, paquetes y registros tienen semánticas distintas definidas en la especificación.
