# Estado de AIP

**Versión documental:** 0.1.0 · **Fecha:** 2026-10-19.

**Fase:** planificación de Trabajo B. **Implementación de Trabajo B:** pendiente de autorización del plan; este PR es exclusivamente documental.

## Evidencia y acuerdos

| Fecha | Fuente y acuerdo | Alcance |
| --- | --- | --- |
| 2026-10-19 | Instrucción inicial del usuario; [ADR-0001](decisiones/0001-definir-antes-de-implementar.md). | Definir antes de codificar. |
| 2026-10-19 | Instrucción del usuario que identifica AIP y su visión, dirección tecnológica y restricciones. | Confirma RQ-001–RQ-008 de [GOALS](../GOALS.md) y autoriza exclusivamente la fundación documental. |
| 2026-10-19 | Publicación de la fundación y respuesta previa «aprobado». | Existió una implementación local `52c653d` en `work`; no está en `main` ni se importa a esta rama. No equivale a aprobar el nuevo plan con OIDC. |
| 2026-10-19 | Petición «AIP — Trabajo B: Preparación e implementación de I1». | Autoriza inspección, plan y preparación de PR en `feature/i1-catalog-foundation`; prohíbe código y aceptación de ADR hasta registrar aprobación correspondiente. |

La hipótesis inicial sobre qué producto construir queda resuelta por la definición explícita de AIP. En la línea base de esta rama los ADR técnicos permanecen propuestos. No se registra aprobación de PLAN-I1-B, integración OIDC, código adicional o infraestructura. La creación del PR no amplía la autorización.

## Inspección de Trabajo B

Rama creada desde `main` en `25c4fccdec6304f34b3e068a93740bfbb830f20b`. Solo se observó `main` al consultar las ramas remotas; no hay fuente publicada del metamodelo v0.2 para comparar todavía. SPEC-CAT-001 y ontología estable de I1 se conservan sin cambios. El commit local `52c653d` queda recuperable en `work` y sus resultados no se presentan como validación de OIDC ni de esta rama.

Propuesta principal: [PLAN-I1-B v1.0](delivery/plan-implementacion-i1-trabajo-b.md). Cambios de autenticación/versiones: ADR-0013 y ADR-0014, ambos propuestos. Frontera del trabajo paralelo: [compatibilidad v0.2](delivery/compatibilidad-metamodelo-v02.md).

## Documentación preparada

| Área | Estado de contenido | Fuente canónica |
| --- | --- | --- |
| Visión y dirección | Confirmadas por usuario; personas y mercado propuestos/hipotéticos. | [Producto](product/vision.md), [GOALS](../GOALS.md) |
| Alcance y requisitos derivados | Propuestos. | [Requisitos](product/requisitos.md), [alcance financiero](product/alcance-financiero.md) |
| Arquitectura y ontología | Propuestas. | [Referencia](architecture/referencia.md), [ontología](architecture/ontologia.md) |
| Agentes y regulación | Modelos propuestos, sin implementación ni fuentes normativas verificadas. | [Agentes](architecture/agentes.md), [regulación](architecture/regulatorio.md) |
| Decisiones | ADR-0001 de proceso aceptado; ADR-0002–0011 propuestos. | [ADR](decisiones/README.md) |
| Backlog y catálogo | Especificados para revisión, no aprobados. | [Backlog](delivery/backlog.md), [SPEC-CAT-001](specs/catalogo-arquitectonico.md) |
| Trabajo B | Plan detallado y propuestas OIDC/toolchain pendientes de aprobación; sin implementación. | [PLAN-I1-B](delivery/plan-implementacion-i1-trabajo-b.md) |

## Pendientes

| ID | Asunto | Cuándo bloquea |
| --- | --- | --- |
| P-01 | Aprobación expresa de PLAN-I1-B, alcance y decisiones aplicables de Trabajo B. | Todo código nuevo de Trabajo B. |
| P-02 | Proveedor OIDC de desarrollo, registro de cliente/callbacks HTTPS y membresías; propuesta en ADR-0013. | Implementación de acceso e integración del slice. |
| P-03 | Revisar combinación soportada propuesta en ADR-0014 y volver a validar parches al autorizar. | Preparación del toolchain de I1. |
| P-04 | Acordar tipos, operaciones, roles y aceptación de SPEC-CAT-001. Puede cubrirse al aprobar esa versión. | Implementación del catálogo. |
| P-05 | Validar comprador, usuario y subsegmento financiero con evidencia. | Priorización comercial y paquete financiero piloto; no catálogo genérico. |
| P-06 | Seleccionar jurisdicción, materia, fuentes oficiales verificables y revisores competentes. | Implementación y evaluación regulatoria. |
| P-07 | Acordar proveedores de modelos, política de datos, framework y evaluación agéntica. | Ejecución de agentes; no I1. |
| P-08 | Acordar licencias/derechos de contenido y confianza editorial de paquetes. | Distribución e instalación de paquetes. |
| P-09 | Definir residencia, retención, recuperación, disponibilidad y modalidad comercial/de despliegue. | Piloto productivo con datos reales. |
| P-10 | Demostrar necesidad, aislamiento y viabilidad comercial de Neo4j. | Solo una eventual adopción de grafo. |

## Próxima acción recomendada

Revisar [PLAN-I1-B](delivery/plan-implementacion-i1-trabajo-b.md), sus ADR propuestos y el PR documental. Registrar aprobación del alcance/decisiones y resolver P-02/P-03 antes del código dependiente. Mantener I1 independiente de v0.2; no fusionar automáticamente. El resto queda diferido según [PLANS](../PLANS.md).

## Registro de aprobación futura

Añadir una entrada solo cuando exista respuesta explícita: fecha, evidencia de conversación, incremento, versión/commit o secciones cubiertas, ADR aceptados, exclusiones y condiciones. Si la autorización ya existe, no pedirla de nuevo. Hasta entonces no cambiar la fase.

## Verificación de esta entrega

Revisión documental de estructura, enlaces locales, estados de ADR, identificadores y trazabilidad; verificación de soporte/compatibilidad con fuentes oficiales citadas en ADR-0014. Se compara SPEC/ontología con la base para confirmar que permanecen intactos. No se ejecutan pruebas de aplicación ni se trasladan resultados del prototipo local a Trabajo B. La entrega se prepara para revisión humana, no como arquitectura o interoperabilidad OIDC aprobadas.
