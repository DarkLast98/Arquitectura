# Backlog priorizado

**Estado:** BL-00–BL-05 implementados y verificados para revisión del PR #1; futuras tareas propuestas, sin autorización ni estimación. P0 = necesario para el slice; P1 = evolución principal; P2 = condicionado a evidencia. La [aprobación de Trabajo B](../00-estado.md) cubre únicamente I1.

| ID | Prioridad / etapa | Resultado y aceptación | Dependencias | Requisitos |
| --- | --- | --- | --- | --- |
| BL-00 | P0 / antes de I1 | Acordar slice, tipos, contratos, ADR, identidad y versiones; al autorizar I1, materializar contratos verificables. | Aprobación y P-02–P-04. | RQ-007, RQ-008, RNF-CON-001 |
| BL-01 | P0 / I1 | Contexto autenticado y membresías de dos organizaciones sintéticas; roles, denegación por defecto y defensa en base. | BL-00. | RNF-ISO-001 |
| BL-02 | P0 / I1 | Entidades tipadas con consulta, edición, archivo, revisión y auditoría transaccional. | BL-01 y definiciones de tipo. | RF-CAT-001, RNF-AUD-001 |
| BL-03 | P0 / I1 | Relaciones dirigidas compatibles; rechazo de extremos ajenos, archivados y duplicados; archivo auditado. | BL-02. | RF-CAT-002, RNF-AUD-001 |
| BL-04 | P0 / I1 | Interfaz Angular para el recorrido completo y sus errores, cambio de contexto y modo lector. | BL-02, BL-03 y contrato HTTP. | RF-CAT-003 |
| BL-05 | P0 / I1 | Evidencia de aceptación, contratos, concurrencia, permisos y aislamiento con PostgreSQL real y rol de aplicación. | BL-01–BL-04; pruebas construidas junto a cada tarea. | RNF-ISO-001, RNF-AUD-001, RNF-CON-001 |
| BL-11 | P1 / descubrimiento paralelo | Validar problema, comprador, subsegmento y valor con usuarios; registrar evidencia y límites sin datos no autorizados. | Participantes y consentimiento. | RQ-001; H-01–H-05 |
| BL-06 | P1 / I2 | Paquetes declarativos, activación por organización, versiones y dependencias; conflicto falla y actualización no altera historia. | I1; confianza editorial y derechos. | RF-PKG-001, RNF-EXT-001 |
| BL-07 | P1 / I3 | Evidencia, propuestas y revisión vinculadas a versiones exactas; cambios invalidan revisión actual cuando corresponde. | I1 y modelo de gobierno acordado. | RF-GOV-001 |
| BL-12 | P1 / I3 | Relacionar objetivos y capacidades con alternativas de solución, vistas y decisiones; navegación de un caso estratégico completo. | BL-06, BL-07 y ontología ampliada acordada. | RF-STR-001, RQ-002 |
| BL-08 | P1 / I4 | Un caso asistido Python devuelve propuesta con evidencia; límites, validación y revisión antes de aplicar. | BL-07, evaluación y proveedor autorizados. | RF-AGT-001, RNF-EXT-001 |
| BL-13 | P1 / I5 | Paquete financiero piloto de un subsegmento, revisado por especialista y separado de obligaciones legales. | BL-06, BL-11 y derechos de contenido. | RF-FIN-001, RQ-001, RQ-003 |
| BL-09 | P1 / I5 | Corpus jurisdiccional pequeño verificado y caso legal con vigencia, aplicabilidad, revisión humana e invalidación. | BL-06–BL-08, BL-13, jurisdicción, fuentes y revisores. | RF-REG-001, RF-REG-002, RNF-REG-001 |
| BL-10 | P2 / evaluación | Comparar consultas reales PostgreSQL/Neo4j y emitir decisión con seguridad, licencia, costo y recuperación. Puede concluir no adopción. | Casos y volúmenes medidos, no tamaño de backlog. | RQ-007; ADR-0007 |

## Orden y criterios

Dentro de I1, cerrar primero identidad/contexto y contratos; construir un recorrido de entidad y relación completo y ampliar sin perder pruebas de aislamiento. BL-05 es una condición de aceptación, no una actividad de seguridad aplazada al final.

P1 no implica alcance de I1. No hay fechas, puntos ni promesas de entrega. El resultado de entrevistas puede cambiar el orden posterior mediante una decisión documentada. El grafo nunca es prerrequisito implícito del catálogo.

**Siguiente trabajo recomendado:** revisión humana del PR #1 y de [su evidencia](evidencia-i1-trabajo-b.md). Después, definir I2, comparar v0.2 publicado y proponer compatibilidad de paquetes antes de autorizar ese desarrollo.
