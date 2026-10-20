# ADR-0011: Core y paquetes declarativos versionados

**Estado:** Aceptado para I1 / Trabajo B. **Fecha:** 2026-10-19. **Origen:** RQ-001, RQ-003, RQ-005.

## Contexto y decisión propuesta

Separar Core transversal, conocimiento sectorial, conocimiento jurisdiccional y configuración privada. Extensiones declarativas con namespaces, definiciones inmutables, versiones exactas, procedencia, derechos y dependencias. No ejecutar código ni aceptar instrucciones privilegiadas desde un paquete. Core no depende de un sector o país concreto.

## Alternativas

- Fork por cliente o industria: duplica reglas y dificulta evolución de producto independiente.
- Único repositorio de conocimiento sin versiones: impide reproducir análisis y controlar cambios normativos.
- Plugins ejecutables arbitrarios: mayor flexibilidad y superficie de ataque; no necesarios para los objetivos conocidos.

## Consecuencias y validación

Separa contenido reutilizable y privado, pero requiere gobierno editorial, compatibilidad, confianza del editor y licencia. I1 solo usa un registro fijo de tipos Core; I2 evalúa activación, conflictos y migración sin borrar historia. Fuentes nuevas no aprueban automáticamente conclusiones legales. Detalles en [paquetes](../architecture/paquetes.md).

**Aprobación:** usuario «aprobado, implementa el PR», referido al PR #1 / PLAN-I1-B v1.0 en `469f2dc`. Aceptación limitada a I1; capacidades futuras y despliegue siguen diferidos.
