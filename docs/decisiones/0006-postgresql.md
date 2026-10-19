# ADR-0006: PostgreSQL como fuente de verdad

**Estado:** Propuesto. **Fecha:** 2026-10-19. **Origen:** RQ-006, RQ-007.

## Contexto y decisión propuesta

Persistir catálogo, relaciones, revisiones y auditoría de mutaciones en PostgreSQL con integridad referencial y transacciones. Mantenerlo como autoridad aunque existan proyecciones de lectura posteriores. Incluir organización en claves de integridad y restricciones según ADR-0010.

## Alternativas

- Grafo como base primaria: atractivo para recorridos, pero cambia el modelo transaccional y de aislamiento antes de validarlo.
- Otros motores relacionales: posibles; requieren justificar el cambio a la dirección inicial.

## Consecuencias y validación

Consultas relacionales cubren el alcance de I1; no se necesita un segundo almacén para demostrarlo. Verificar versión soportada, compatibilidad del adaptador .NET, transacciones concurrentes, RLS con rol real y estrategias de migración antes de aceptar. Backup, residencia y recuperación productiva se diseñarán antes del piloto, sin desplegarlos en esta fase.

**Aprobación:** pendiente; no hay base provisionada ni esquema aplicado.
