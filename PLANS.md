# Plan de evolución de AIP

**Estado:** propuesto. Solo la elaboración documental está autorizada. No se asignan fechas ni capacidad de equipo no acordadas.

## Trabajo B — revisión previa a implementación

El [plan detallado PLAN-I1-B v1.0](docs/delivery/plan-implementacion-i1-trabajo-b.md) concreta proyectos, agregados, OIDC configurable, contratos, modelo relacional, autorización, migraciones, pruebas y aceptación. Se prepara en `feature/i1-catalog-foundation` desde la última `main` inspeccionada (`25c4fcc`). Este PR no incorpora código ni cambia ADR a aceptados.

La implementación anterior `52c653d` se conserva en `work` y no se importa. La instrucción de Trabajo B exige aprobar el nuevo plan antes de código, con la [frontera estable frente a v0.2](docs/delivery/compatibilidad-metamodelo-v02.md). Las etapas siguientes conservan su alcance orientativo.

| Etapa | Resultado | Condición de salida |
| --- | --- | --- |
| D0 · Fundación documental | Producto, arquitectura, ADR, backlog y especificación del catálogo. | Coherencia revisada; pendientes y propuestas visibles. No habilita código. |
| I1 · Catálogo arquitectónico | Recorrido manual Angular → API .NET modular → PostgreSQL, con entidades y relaciones aisladas por organización. | Casos de aceptación, aislamiento, autorización, concurrencia y auditoría del slice superados. |
| I2 · Conocimiento extensible | Activación de paquetes versionados y evolución compatible de tipos. | Dependencias, compatibilidad y aislamiento de extensiones comprobados. |
| I3 · Evidencia, vistas y gobierno | Artefactos, revisiones, decisiones y fuentes con historia; recorrido de estrategia a solución. | Revisiones vinculadas a versiones exactas, evidencia recuperable y vistas trazables. |
| I4 · Asistencia agéntica | Un caso asistido acotado, con propuestas estructuradas y límites. | Evaluaciones de calidad, seguridad y aprobación antes de persistir. |
| I5 · Especialización financiera y regulatoria | Paquete financiero piloto y un ámbito jurisdiccional acotado. | Fuentes verificadas, análisis de aplicabilidad, vigencia y revisión profesional. |

I2–I5 expresan orden orientativo, no entregas aprobadas. El trabajo regulatorio requiere I2, I3 y el control agéntico de I4; la investigación de fuentes y mercado puede hacerse antes sin implementar.

## Alcance recomendado para I1

Tres tipos de entidad (`core.business_capability`, `core.application`, `core.data_asset`), dos relaciones (`core.supports`, `core.uses`), consulta y mantenimiento manual, archivo lógico, control de concurrencia, auditoría de cambios y aislamiento de dos organizaciones de demostración. Identidad y autorización se incluyen como prerrequisito transversal, no como un módulo comercial completo.

Fuera: agentes, Neo4j, paquetes instalables, ingestión normativa, dictámenes regulatorios, microservicios, infraestructura desplegada y conectores externos. Detalle en [slice](docs/delivery/primer-incremento.md) y [especificación](docs/specs/catalogo-arquitectonico.md).

## Antes de iniciar I1

| Pendiente | Resolución necesaria | Referencia |
| --- | --- | --- |
| P-01 | Aprobación explícita de PLAN-I1-B, alcance y decisiones aplicables de Trabajo B; registrar evidencia antes de implementar. | [Estado](docs/00-estado.md) |
| P-02 | Acordar proveedor OIDC de desarrollo, cliente/callbacks y membresías verificadas en servidor; sin cabeceras de suplantación ni contraseñas locales como fallback. | [ADR-0013](docs/decisiones/0013-oidc-desarrollo-configurable.md), ADR-0010 y especificación |
| P-03 | Revisar/aprobar la combinación soportada propuesta; revalidar parches antes de fijar manifests y lockfiles tras autorización. | [ADR-0014](docs/decisiones/0014-versiones-soportadas-i1.md), ADR-0003/0004/0006 |
| P-04 | Acordar los cinco tipos de I1, operaciones, roles y aceptación. Puede quedar cubierto por la aprobación expresa de la especificación. | Backlog y especificación |

No bloquean I1: país inicial, Neo4j, proveedor de modelos, runtime de agentes, licenciamiento y arquitectura de despliegue productivo. Sí bloquean sus respectivas entregas futuras.

## Forma de avanzar

Cada incremento tendrá requisito, alcance, dependencias, contratos, aceptación y evidencia. Preparar primero la propuesta; implementar solo lo aprobado. Los cambios que alteren límites, acceso a datos o decisiones relevantes requieren revisar su impacto. Mantener en [el backlog](docs/delivery/backlog.md) la prioridad y en [el estado](docs/00-estado.md) la evidencia de aprobación.
