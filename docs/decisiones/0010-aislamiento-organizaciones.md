# ADR-0010: aislamiento lógico con defensa en PostgreSQL

**Estado:** Propuesto. **Fecha:** 2026-10-19. **Origen:** RQ-006.

## Contexto y decisión propuesta

El aislamiento es obligatorio; su mecanismo no está aprobado. Proponer para I1 un esquema compartido con `organization_id` obligatorio en datos privados, autorización por membresía, restricciones/foráneas compuestas y RLS como defensa adicional. El contexto de base vive en la transacción y falla cerrado si falta. La aplicación usa rol no propietario y sin bypass; migración y administración usan roles separados.

## Alternativas

- Esquema por organización: mayor separación de nombres, más complejidad de migración y operación.
- Base por organización: más aislamiento físico y opciones de residencia, mayor costo operativo.
- Solo filtros de aplicación: deja menos defensas frente a omisiones y acceso accidental; insuficiente como propuesta inicial.

## Consecuencias y validación

La opción compartida facilita I1, pero no garantiza por sí sola cumplimiento, aislamiento de backups o requisitos de un cliente. Validar lecturas/escrituras con el rol real, pooling, contexto ausente, relaciones cruzadas, conteos y auditoría. Revisar necesidad de base dedicada antes de datos productivos o requisitos de residencia específicos. Las garantías cubren también futuros agentes, índices y archivos.

**Aprobación:** pendiente; no se han aplicado políticas ni ejecutado sus pruebas.
