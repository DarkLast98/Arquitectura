# Estado de AIP

**Versión documental:** 0.1.0 · **Fecha:** 2026-10-19.

**Fase:** fundación documental. **Implementación:** no autorizada.

## Evidencia y acuerdos

| Fecha | Fuente y acuerdo | Alcance |
| --- | --- | --- |
| 2026-10-19 | Instrucción inicial del usuario; [ADR-0001](decisiones/0001-definir-antes-de-implementar.md). | Definir antes de codificar. |
| 2026-10-19 | Instrucción del usuario que identifica AIP y su visión, dirección tecnológica y restricciones. | Confirma RQ-001–RQ-008 de [GOALS](../GOALS.md) y autoriza exclusivamente la fundación documental. |

La hipótesis inicial sobre qué producto construir queda resuelta por la definición explícita de AIP. No hay aprobación de los ADR técnicos, del slice, de aplicaciones, agentes ni infraestructura. La creación o el versionado de los documentos no amplían la autorización.

## Documentación preparada

| Área | Estado de contenido | Fuente canónica |
| --- | --- | --- |
| Visión y dirección | Confirmadas por usuario; personas y mercado propuestos/hipotéticos. | [Producto](product/vision.md), [GOALS](../GOALS.md) |
| Alcance y requisitos derivados | Propuestos. | [Requisitos](product/requisitos.md), [alcance financiero](product/alcance-financiero.md) |
| Arquitectura y ontología | Propuestas. | [Referencia](architecture/referencia.md), [ontología](architecture/ontologia.md) |
| Agentes y regulación | Modelos propuestos, sin implementación ni fuentes normativas verificadas. | [Agentes](architecture/agentes.md), [regulación](architecture/regulatorio.md) |
| Decisiones | ADR-0001 de proceso aceptado; ADR-0002–0011 propuestos. | [ADR](decisiones/README.md) |
| Backlog y catálogo | Especificados para revisión, no aprobados. | [Backlog](delivery/backlog.md), [SPEC-CAT-001](specs/catalogo-arquitectonico.md) |

## Pendientes

| ID | Asunto | Cuándo bloquea |
| --- | --- | --- |
| P-01 | Aprobación expresa del alcance I1, su especificación y ADR aplicables. | Cualquier implementación. |
| P-02 | Identidad de desarrollo y aprovisionamiento/verificación de membresías; sin suplantación por cabecera. | Implementación de acceso e integración del slice. |
| P-03 | Versiones soportadas y compatibles de Angular/Node, .NET y PostgreSQL. | Preparación del toolchain de I1. |
| P-04 | Acordar tipos, operaciones, roles y aceptación de SPEC-CAT-001. Puede cubrirse al aprobar esa versión. | Implementación del catálogo. |
| P-05 | Validar comprador, usuario y subsegmento financiero con evidencia. | Priorización comercial y paquete financiero piloto; no catálogo genérico. |
| P-06 | Seleccionar jurisdicción, materia, fuentes oficiales verificables y revisores competentes. | Implementación y evaluación regulatoria. |
| P-07 | Acordar proveedores de modelos, política de datos, framework y evaluación agéntica. | Ejecución de agentes; no I1. |
| P-08 | Acordar licencias/derechos de contenido y confianza editorial de paquetes. | Distribución e instalación de paquetes. |
| P-09 | Definir residencia, retención, recuperación, disponibilidad y modalidad comercial/de despliegue. | Piloto productivo con datos reales. |
| P-10 | Demostrar necesidad, aislamiento y viabilidad comercial de Neo4j. | Solo una eventual adopción de grafo. |

## Próxima acción recomendada

Revisar [I1](delivery/primer-incremento.md) y [SPEC-CAT-001](specs/catalogo-arquitectonico.md). Resolver P-01–P-04 y registrar la aprobación antes de codificar. El resto queda diferido según [PLANS](../PLANS.md).

## Registro de aprobación futura

Añadir una entrada solo cuando exista respuesta explícita: fecha, evidencia de conversación, incremento, versión/commit o secciones cubiertas, ADR aceptados, exclusiones y condiciones. Si la autorización ya existe, no pedirla de nuevo. Hasta entonces no cambiar la fase.

## Verificación de esta entrega

Revisión documental de estructura, enlaces locales, estados de ADR, identificadores y trazabilidad. No se ejecutan pruebas de aplicación ni se validan runtime, controles de base, agentes o comportamiento regulatorio: aún no existen. La base se prepara para revisión humana, no como arquitectura aprobada.
