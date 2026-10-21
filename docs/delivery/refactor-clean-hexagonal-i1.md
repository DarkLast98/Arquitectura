# Refactorización de I1 a Clean Architecture / hexagonal

**Fecha:** 2026-10-21 America/Bogota. **Estado:** implementación y verificación en curso. **Autorización:** petición explícita del usuario recogida en ADR-0015. Rama de PR #1: feature/i1-catalog-foundation. Sin fusión, producción o I2.

## Plan y criterios

1. Separar Domain, Application, Infrastructure y Api; conservar un solo host y los módulos catálogo, identidad/acceso y auditoría.
2. Sustituir el puerto SQL por operaciones tipadas de repositorio, auditoría, identidad y transacción. Retirar HttpContext/JSON/SQL de los casos de uso.
3. Mantener contratos, invariantes, orden de locks, RLS, ámbito transaccional, OIDC y seis migraciones sin cambios semánticos.
4. Extraer transporte del componente Angular, componer CatalogWorkspace con un puerto HTTP y comprobar el núcleo sin Angular/DOM.
5. Incluir solución, comprobación de dependencias, pruebas de casos de uso y verificador reproducible actualizado.
6. Compilar y ejecutar la suite completa en exportación limpia; registrar evidencia sanitizada y actualizar el dictamen antes de considerar el PR listo.

| Criterio | Evidencia prevista |
| --- | --- |
| Dependencias hacia el núcleo | Proyectos sin paquetes externos en Domain/Application, referencias e importaciones verificadas. |
| Casos de uso independientes | Pruebas .NET sin servidor/base y TypeScript sin Angular/DOM. |
| Contratos y migraciones intactos | Comparación de archivos/checksums, generación de contratos y pruebas HTTP reales. |
| Aislamiento y autorización | Casos negativos API/SQL con PostgreSQL real y sesiones OIDC. |
| Consistencia | Revisiones, carreras archivo/relación y rollback de auditoría. |
| Entorno reproducible | scripts/verify-clean-i1.py desde commit exportado y datos/cachés nuevos. |

## Riesgos y decisiones

El principal riesgo es alterar serialización, orden de errores o límites transaccionales al extraer adaptadores. Se conserva la suite existente y se añade validación específica de puertos. No se usa una base en memoria para acreditar aislamiento/rollback. Las migraciones se mueven físicamente, por lo que preparación y verificador deben usar la nueva ruta. El dictamen técnico anterior es evidencia histórica de otro commit hasta completar esta validación.

Las decisiones de operación productiva, versión futura del metamodelo y dependencias pendientes del IdP siguen con el alcance ya documentado. No se amplía funcionalidad.
