# I1 y metamodelo v0.2: frontera de compatibilidad

**Estado:** frontera aplicada en I1 / Trabajo B. **Fecha:** 2026-10-19 (America/Bogota).

## Evidencia disponible

`git ls-remote --heads origin` mostró únicamente `main` en `25c4fcc` durante la inspección inicial de Trabajo B. No se dispone de una rama, commit o documento publicado de v0.2 para una comparación semántica real. No se afirma que v0.2 sea compatible o incompatible ni se inventan sus entidades.

La implementación local previa `52c653d`, en `work`, tampoco es el metamodelo v0.2 y no forma parte de esta rama.

## Línea estable de I1

Contrato: [SPEC-CAT-001 v0.1.0](../specs/catalogo-arquitectonico.md) y [ontología preliminar](../architecture/ontologia.md), sin modificación en este trabajo. API `v1`, esquema `0.1.0` y registro `core.catalog` v0.1.0 son identificadores distintos de la versión documental o del futuro metamodelo.

- Entidades: `core.business_capability`, `core.application`, `core.data_asset`.
- Relaciones: `core.supports` (application → business_capability) y `core.uses` (application → data_asset).
- Organización/tipo/esquema/código y extremos inmutables según SPEC; roles, campos, archivo y aceptación conservados.
- OIDC resuelve identidad y contexto; no agrega entidades de negocio ni cambia esa semántica.

## Comparación cuando exista v0.2

Solicitar o localizar su commit, registrar la fuente y construir una tabla de diferencias verificadas: concepto v0.2, equivalente I1, cambio de campos/tipos/cardinalidades, impacto en contratos/datos, compatibilidad y decisión propuesta. No mezclar ramas automáticamente.

| Diferencia posible, todavía no observada | Tratamiento propuesto |
| --- | --- |
| Nuevos conceptos o relaciones opcionales | Diferir al siguiente incremento; no se vuelven requisitos ni seeds de I1. |
| Renombrar/redefinir un ID estable | Incompatible: conservar I1 y proponer nueva versión/ADR con mapeo explícito. |
| Cambiar extremos/cardinalidad o semántica de supports/uses | Incompatible: pruebas de contrato deben detectarlo; presentar decisión para el siguiente incremento. |
| Exigir un nuevo atributo obligatorio | Incompatible para productores/datos I1: requerir migración y transición acordadas, sin añadirlo silenciosamente. |
| Alterar organización, propiedad o autorización | Revisar frontera de seguridad; no relajar aislamiento para adaptar el modelo. |
| Añadir Neo4j, agentes o paquetes dinámicos | Fuera de alcance; no introducir una dependencia técnica de I1. |

Estas filas son criterios de evaluación, no hallazgos sobre el trabajo paralelo.

## Control de cambios

Antes de cada PR de implementación, comparar definiciones, ejemplos y pruebas de contrato con la línea estable. Si `main` recibe v0.2, analizar su diff y rebase/merge solo después de preservar I1 y resolver conflictos con evidencia. No resetear commits de otros trabajos ni dar por aprobado un cambio de ontología.

La aprobación del plan B no aprueba v0.2; la aprobación de v0.2 no migra datos ni cambia contratos I1. Una incompatibilidad real se documentará en un ADR propuesto del siguiente incremento, con fuente exacta, alternativas, transición y aceptación requerida.

## Comprobación de la entrega

2026-10-20 UTC: nueva consulta remota muestra `main/25c4fcc` y `feature/i1-catalog-foundation`; no aparece una fuente v0.2 publicada. SPEC-CAT-001 y ontología no tienen diff respecto de la base. Pruebas de dominio/contrato rechazan tipos y versiones futuros; se mantienen exactamente tres entidades y dos relaciones. Se reutilizan partes compatibles de catálogo/interfaz de `work`, sin su autenticación local. No se afirma compatibilidad semántica con un documento v0.2 no disponible.
