# ADR-0009: arquitectura documental en Git

**Estado:** Aceptado para I1 / Trabajo B. **Fecha:** 2026-10-19. **Origen:** RQ-007, RQ-008.

## Contexto y decisión propuesta

Mantener Markdown como fuente documental, Mermaid para diagramas de texto y ADR numerados para decisiones. Git conserva cambios y referencias revisables. La versión 0.1.0 identifica la fundación documental; no es versión del producto ejecutable ni señal de aprobación.

## Alternativas

- Herramienta externa como única fuente: puede facilitar colaboración visual, pero separa historia y decisiones del repositorio.
- Documentos binarios: apropiados para entregables específicos, menos útiles para comparar cambios de arquitectura.

## Consecuencias y validación

Exige enlaces válidos, vocabulario consistente, revisión de estados y actualización de índices. No se asume un renderer Mermaid instalado ni que un commit certifique el contenido. La fuente está creada para revisión conforme a la tarea documental; el ADR permanece propuesto hasta acuerdo explícito.

**Aprobación:** usuario «aprobado, implementa el PR», referido al PR #1 / PLAN-I1-B v1.0 en `469f2dc`. Aceptación limitada a I1; capacidades futuras y despliegue siguen diferidos.
