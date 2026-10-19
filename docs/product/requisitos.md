# Requisitos derivados y trazabilidad

**Requisitos de origen usuario:** [RQ-001–RQ-008](../../GOALS.md), confirmados como dirección. **Esta descomposición:** propuesta; no autoriza implementación. Las hipótesis de mercado se mantienen en [su documento](problema-mercado.md).

| ID | Requisito derivado propuesto | Origen | Aceptación resumida | Backlog |
| --- | --- | --- | --- | --- |
| RF-CAT-001 | Registrar, consultar, editar y archivar entidades tipadas por organización. | RQ-002, RQ-008 | Persistencia y recuperación del mismo registro; validación de tipo y versión; concurrencia y archivo controlados. | BL-02 |
| RF-CAT-002 | Registrar, consultar y archivar relaciones dirigidas tipadas. | RQ-002, RQ-008 | Extremos válidos de la misma organización; tipos compatibles; sin duplicado activo ni vínculo a entidad archivada. | BL-03 |
| RF-CAT-003 | Consultar un recorrido capacidad → aplicación → dato desde una interfaz manual. | RQ-002, RQ-008 | Un usuario autorizado completa el escenario y sus errores; un lector no escribe. | BL-04 |
| RF-PKG-001 | Versionar y habilitar conocimiento Core, sectorial y jurisdiccional sin modificar el Core. | RQ-003 | Dependencias y compatibilidad resueltas; paquetes no ejecutables y aislados de datos privados. | BL-06 |
| RF-GOV-001 | Asociar evidencia, versiones y revisión a decisiones o hallazgos. | RQ-004, RQ-005 | Una revisión referencia el contenido exacto; un cambio relevante exige reevaluación. | BL-07 |
| RF-STR-001 | Vincular objetivos y capacidades con alternativas, vistas y decisiones de solución. | RQ-002 | Recorrido estratégico completo con relaciones tipadas y evidencia; sin asumir generación automática. | BL-12 |
| RF-FIN-001 | Habilitar un vocabulario financiero piloto revisado por especialista. | RQ-001, RQ-003 | Contenido de un subsegmento, con procedencia y derechos; sin atribuirle fuerza normativa. | BL-13 |
| RF-AGT-001 | Ejecutar tareas asistidas acotadas que produzcan propuestas estructuradas. | RQ-004 | Contexto y herramientas autorizados; resultados validados y sin escritura directa por el modelo. | BL-08 |
| RF-REG-001 | Mantener fuentes regulatorias verificadas y su historia temporal. | RQ-005 | Referencia oficial verificable, versión, evidencia de consulta y estado de verificación; contenido no verificado no fundamenta obligaciones. | BL-09 |
| RF-REG-002 | Evaluar aplicabilidad y registrar aprobación humana contextual. | RQ-005 | Incluye organización, hechos, ámbito, fechas, fuente y revisión; desconocido no se transforma en no aplicable. | BL-09 |
| RNF-ISO-001 | Aplicar aislamiento por organización a toda operación y almacenamiento. | RQ-006 | Dos organizaciones sin lectura, escritura, enlaces, conteos o contexto cruzados; contexto ausente deniega. | BL-01, BL-05 |
| RNF-AUD-001 | Registrar mutaciones con actor, organización, recurso, revisión y correlación. | RQ-005, RQ-006 | Cambio y evento de auditoría se confirman o revierten juntos; lectura auditada por ámbito, sin contenido sensible innecesario. | BL-02, BL-03, BL-05 |
| RNF-CON-001 | Mantener contratos OpenAPI y JSON Schema versionados y validables. | RQ-007 | Contrato coincide con operaciones y errores; entradas inválidas se rechazan de manera consistente. | BL-00, BL-05 |
| RNF-REG-001 | Diferenciar fuente, interpretación, aplicabilidad y conclusión de cumplimiento. | RQ-005 | Sin afirmaciones legales basadas solo en generación de texto; no se representa vigencia desconocida como vigente. | BL-09 |
| RNF-EXT-001 | Evitar dependencia del Core de un cliente, industria, jurisdicción o proveedor de modelos. | RQ-001, RQ-003, RQ-004 | Paquetes/adaptadores reemplazables, sin datos de una empresa codificados en el núcleo. | BL-06, BL-08 |

Los criterios completos de I1 están en [el catálogo](../specs/catalogo-arquitectonico.md). Rendimiento, disponibilidad, recuperación, residencia de datos y retención deben acordarse para el piloto productivo; no hay SLAs aprobados. El primer incremento no constituye validación de esos atributos para producción.

## Regla de trazabilidad

Usar estos identificadores en backlog y evidencias futuras. Un requisito pasa de propuesto a acordado solo con evidencia en [el estado](../00-estado.md); de implementado a validado solo con resultados verificables. Ninguna prueba de aplicación ha sido ejecutada en esta fase.
