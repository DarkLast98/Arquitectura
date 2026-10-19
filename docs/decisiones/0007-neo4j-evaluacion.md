# ADR-0007: evaluar Neo4j como proyección opcional

**Estado:** Propuesto. **Fecha:** 2026-10-19. **Origen:** RQ-007.

## Contexto y decisión propuesta

Mantener Neo4j como candidato, sin incorporarlo a I1. Evaluar una proyección de conocimiento solo cuando haya consultas reales de impacto o recorrido que lo justifiquen. PostgreSQL conserva autoridad; no hay escritura doble independiente ni dependencia obligatoria de un grafo en Core.

## Alternativas

- Relaciones y consultas recursivas en PostgreSQL: base inicial, menor costo operativo.
- Otro motor o índice de grafos: comparar con los mismos casos y restricciones si fuese necesario.

## Consecuencias y validación

Una adopción añadiría sincronización, consistencia eventual, reconstrucción y un nuevo límite de seguridad. Comparar utilidad, latencia, volumen, costo, edición/licencia y aislamiento verificable. Probar autorización por organización, retirada de permisos, borrados, retraso y recuperación de proyección. Definir umbrales a partir del caso medido, no benchmarks inventados.

**Aprobación:** pendiente. Este ADR propone evaluar, no adoptar Neo4j.
