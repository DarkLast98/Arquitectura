# ADR-0003: Angular y TypeScript para la interfaz

**Estado:** Aceptado para I1 / Trabajo B. **Fecha:** 2026-10-19. **Origen:** RQ-007.

## Contexto y decisión propuesta

La dirección del usuario sitúa la experiencia web en Angular y TypeScript. Proponer una aplicación web modular con formularios, consulta y navegación de relaciones para I1. Los permisos y validaciones de negocio siguen siendo autoridad del backend.

## Alternativas

- Otras bibliotecas SPA: posibles, pero cambiarían la dirección inicial sin un motivo establecido.
- Renderizado de servidor: puede reducir lógica de cliente, con distinta experiencia y capacidades; evaluar solo si los requisitos lo favorecen.

## Consecuencias y validación

Requiere una cadena Node/Angular compatible y mantenida, accesibilidad y manejo consistente de errores y contexto organizacional. Elegir versiones soportadas verificando fuentes oficiales al iniciar I1; no fijar versiones por suposición. Validar el recorrido manual y los estados de acceso denegado y concurrencia.

**Aprobación:** usuario «aprobado, implementa el PR», referido al PR #1 / PLAN-I1-B v1.0 en `469f2dc`. Aceptación limitada a I1; capacidades futuras y despliegue siguen diferidos.
