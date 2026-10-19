# ADR-0008: OpenAPI y JSON Schema versionados

**Estado:** Propuesto. **Fecha:** 2026-10-19. **Origen:** RQ-007, RQ-008.

## Contexto y decisión propuesta

Documentar la API HTTP con OpenAPI y definir estructuras de tipos, paquetes y salidas de agentes con JSON Schema. Como base candidata, OpenAPI 3.1 y JSON Schema 2020-12; validar soporte real de las herramientas elegidas antes de fijarlos. API `v1`, esquema de tipo y revisión del recurso son conceptos distintos.

## Alternativas

- Convenciones sin contrato formal: no permiten comprobar compatibilidad con suficiente precisión.
- Protocolos adicionales como gRPC: posibles para una necesidad futura, sin incluirlos en I1.

## Consecuencias y validación

Exige gobernar cambios, dialectos y resolución de referencias, además de validación de dominio y permisos que un esquema no expresa. Verificar entradas, respuestas, errores, tipos de datos y compatibilidad de consumidores. La especificación del catálogo describe los contratos; los archivos ejecutables de contrato se crearán al autorizar I1.

**Aprobación:** pendiente; esta entrega no genera clientes ni servidores.
