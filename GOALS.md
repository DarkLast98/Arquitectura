# Objetivos y restricciones de AIP

**Versión documental:** 0.1.0. **Origen:** instrucciones del usuario del 2026-10-19. La dirección de producto y el alcance/arquitectura de I1 están aprobados; los incrementos futuros siguen propuestos. Ver [autorización y dictamen](docs/delivery/revision-tecnica-final-i1.md).

## Requisitos confirmados de origen usuario

| ID | Requisito |
| --- | --- |
| RQ-001 | Construir progresivamente una plataforma comercializable e independiente de cualquier empresa, inicialmente orientada al sector financiero. |
| RQ-002 | Cubrir la gestión de arquitectura desde estrategia empresarial hasta arquitectura de solución. |
| RQ-003 | Separar un núcleo transversal de paquetes de conocimiento por sector y jurisdicción. |
| RQ-004 | Incorporar inteligencia multiagente para análisis, diseño, evaluación y gobierno. |
| RQ-005 | Incluir un agente legal-regulatorio con trazabilidad normativa, control de vigencia, análisis de aplicabilidad y aprobación humana. |
| RQ-006 | Diseñar aislamiento por organización desde el inicio. |
| RQ-007 | Partir de Angular/TypeScript, .NET modular, Python, PostgreSQL, OpenAPI, JSON Schema y documentación Markdown/Mermaid/ADR; evaluar Neo4j como alternativa. Registrar las decisiones tecnológicas como propuestas. |
| RQ-009 | Trabajar el entorno de desarrollo y código con Clean Architecture y arquitectura hexagonal, por instrucción posterior del usuario; ADR-0015. |
| RQ-008 | Producir primero una fundación documental versionada, backlog, primer vertical slice y especificación del catálogo. No implementar sin aprobación. |

## Resultados buscados

1. Un lenguaje común para conectar objetivos, capacidades, aplicaciones, datos y decisiones de arquitectura.
2. Análisis y recomendaciones con evidencia, autoría, versiones y revisión humana.
3. Conocimiento reutilizable entre organizaciones sin compartir sus datos privados.
4. Evolución por incrementos verificables, con fronteras claras entre producto base y especialización.

Son objetivos de diseño. Su valor comercial y su impacto operativo necesitan validación con usuarios; no se presentan como resultados ya obtenidos.

## Señales de éxito propuestas

| Objetivo | Cómo evaluarlo | Estado |
| --- | --- | --- |
| Utilidad inicial | Un arquitecto modela una capacidad, una aplicación y un activo de datos y navega sus relaciones. | I1 demostrado con interfaz, API y PostgreSQL reales; ver matriz final. |
| Aislamiento | Casos negativos con dos organizaciones no revelan ni alteran datos ajenos. | I1 verificado con SQL bajo rol real, API y OIDC; ver matriz final. |
| Trazabilidad | Cada recomendación o evaluación futura identifica evidencia, versiones y revisor. | Propuesto para entregas posteriores. |
| Valor de mercado | Entrevistas y un piloto miden esfuerzo actual y mejora observada. | Hipótesis; métricas y umbrales pendientes. |

## Exclusiones actuales

No definir precios, licencias comerciales, SLAs, certificaciones, jurisdicción inicial ni proveedor cloud como si estuvieran acordados. AIP no ejecutará transacciones financieras en el alcance planteado y no certificará cumplimiento jurídico automáticamente. No usar datos reales de clientes en la fundación ni en el primer incremento propuesto.

La lista de capacidades futuras no compromete su entrega en el primer incremento. [Requisitos derivados](docs/product/requisitos.md), [backlog](docs/delivery/backlog.md) y [pendientes](docs/00-estado.md) concretan esa separación.
