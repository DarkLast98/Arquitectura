# ADR-0002: backend como monolito modular inicial

**Estado:** Propuesto. **Fecha:** 2026-10-19. **Origen:** RQ-007, RQ-008.

## Contexto

El catálogo necesita consistencia entre entidades, relaciones, autorización y auditoría. No existe evidencia de equipos, escalas o límites operativos que justifiquen distribuir esas transacciones.

## Decisión propuesta

Empezar con un backend .NET desplegable como unidad, organizado por módulos con propiedad de datos, casos de uso y contratos explícitos. Aislar organización/acceso, catálogo, definiciones y auditoría desde I1, sin crear servicios por cada responsabilidad.

## Alternativas

- Microservicios desde el inicio: añaden coordinación, red y consistencia distribuida antes de demostrar necesidad.
- Aplicación sin límites internos: acelera un prototipo, pero dificulta extensibilidad y gobierno.

## Consecuencias y validación

Facilita transacciones y operación inicial; exige disciplina para no acceder a escrituras internas de otros módulos. La extracción futura dependerá de autonomía y necesidades medidas. Revisar dependencias y recorrido completo de I1 antes de aceptar. No determina infraestructura ni proyectos de código concretos.

**Aprobación:** pendiente del usuario; no autoriza implementación.
