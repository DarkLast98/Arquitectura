# ADR-0004: .NET para backend y dominio

**Estado:** Propuesto. **Fecha:** 2026-10-19. **Origen:** RQ-007; relacionado con ADR-0002.

## Contexto y decisión propuesta

Usar .NET para API, casos de uso, autorización, reglas de integridad y transacciones. Mantener dominio desacoplado de proveedores de IA y adaptadores de persistencia. El lenguaje candidato es C#, propuesto aquí; no está aprobado por una elección implícita de .NET.

## Alternativas

- Backend Python único: reduce lenguajes, pero aparta la dirección inicial y mezcla necesidades de ejecución de IA y dominio.
- Otra plataforma backend: requiere justificar el cambio respecto a RQ-007.

## Consecuencias y validación

Permite límites y contratos explícitos, a cambio de mantener interoperabilidad con Python futuro. Elegir versión con soporte oficial vigente y validar librerías PostgreSQL, contratos y autenticación antes de fijarlas. Framework de persistencia y organización física de proyectos siguen pendientes; no es necesario imponer una biblioteca concreta en la fundación.

**Aprobación:** pendiente; sin código ni toolchain instalados para AIP.
