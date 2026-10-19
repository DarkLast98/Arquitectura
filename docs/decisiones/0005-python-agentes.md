# ADR-0005: Python para capacidades agénticas

**Estado:** Propuesto. **Fecha:** 2026-10-19. **Origen:** RQ-004, RQ-007.

## Contexto y decisión propuesta

Separar ejecución de IA del dominio transaccional y usar Python detrás de contratos versionados. Cada tarea opera con organización, herramientas y presupuesto acotados y devuelve propuestas; no escribe directamente en PostgreSQL ni reemplaza aprobadores humanos.

## Alternativas

- Agentes en .NET: simplifica runtime; evaluar si la capacidad necesaria y el equipo lo favorecen.
- Framework multiagente completo desde el principio: introduce decisiones no exigidas aún; no seleccionarlo sin caso y evaluación.

## Consecuencias y validación

Añade un lenguaje y un límite de integración; exige contratos, cancelación, errores y trazabilidad entre intentos. Protocolo de ejecución, framework, modelo, proveedor y versiones quedan pendientes de I4. Validar un caso acotado y sus riesgos antes de adoptarlos.

**Aprobación:** pendiente. No es dependencia ni trabajo incluido en I1.
