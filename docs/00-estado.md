# Estado de AIP

**Fecha:** 2026-10-21 America/Bogota. **Fase:** refactorización de I1 a Clean Architecture/hexagonal autorizada, pendiente de nueva validación técnica del PR #1. **Fusión:** no autorizada automáticamente. **Producción e I2:** fuera de alcance.

## Evidencia y autorización

| Fecha | Fuente | Alcance |
| --- | --- | --- |
| 2026-10-19 | Definición del producto por el usuario y [ADR-0001](decisiones/0001-definir-antes-de-implementar.md). | Definir antes de codificar; fundación documental de AIP. |
| 2026-10-19 | Publicación documental y aprobación anterior. | Prototipo local `work/52c653d`, conservado; no valida OIDC de Trabajo B. |
| 2026-10-19 | Solicitud «AIP — Trabajo B: Preparación e implementación de I1». | Plan y PR desde la última main; no aceptar ADR ni codificar sin autorización posterior. |
| 2026-10-20 UTC / 2026-10-19 America/Bogota | Usuario «aprobado, implementa el PR», referido al PR [#1](https://github.com/DarkLast98/Arquitectura/pull/1), PLAN-I1-B v1.0 en `469f2dc`. | Autoriza B0–B3 e implementación exclusiva de SPEC-CAT-001, con OIDC configurable. Acepta ADR-0002/0003/0004/0006/0008/0009/0010, fronteras de 0011, y ADR-0013/0014 para I1. Sin agentes, Neo4j, cambios incompatibles del metamodelo, despliegue productivo o fusión automática. |

La petición del usuario de revisión técnica final del 2026-10-21 confirma arquitectura y alcance I1 para validación y mantiene la fusión condicionada. Autoriza corregir defectos bloqueantes dentro de I1; no autoriza producción, fusión automática o I2.

El commit `6a187c1` registra la autorización antes de los commits de implementación. Se concreta el proveedor de desarrollo recomendado en Keycloak **26.8.1**, release oficial estable de 2026-10-13, ejecutado con JDK 21. Certificados, secretos y sujetos de prueba se preparan localmente fuera de Git. La visión comercial y los diseños futuros no se consideran validados por estas pruebas.

## Autorización de refactorización

2026-10-21 America/Bogota: el usuario solicita «trabaja el entorno de desarrollo y codigo bajo clean architecture, en una arquitectura exagonal». Autoriza reorganizar entorno/código de I1 conforme a [ADR-0015](decisiones/0015-clean-architecture-hexagonal.md), preservando SPEC-CAT-001, contratos, comportamiento y datos. No requiere volver a pedir aprobación del mismo trabajo. [Plan, criterios y evidencia](delivery/refactor-clean-hexagonal-i1.md). El dictamen anterior corresponde al commit evaluado y no valida automáticamente esta refactorización.

## Resultado de Trabajo B

- Un host .NET modular sirve catálogo y Angular por HTTPS; OIDC oficial Code + PKCE/PAR, cookie segura y antiforgery. Sin contraseñas en AIP ni tokens OIDC en el navegador.
- PostgreSQL transaccional con sujeto `(issuer,sub)`, membresías activas, RLS forzada, rol no propietario, permisos por columna, FKs organizacionales, revisión optimista y auditoría atómica.
- Tres tipos de entidad y dos relaciones estables; altas, consultas, edición permitida, archivo lógico, filtros/páginas y errores uniformes.
- Verificación completa: **6** pruebas de dominio, **16** API/base, **19** navegador; OpenAPI/JSON Schema, reinicio de API, migraciones repetibles y fallo seguro de configuración/IdP. [Dictamen final, matriz y evidencia](delivery/revision-tecnica-final-i1.md).
- [Ejecución local](delivery/ejecucion-local-i1.md) documentada. No se despliega infraestructura ni se fusiona el PR.

## Línea base y coordinación

`feature/i1-catalog-foundation` parte de `main/25c4fccdec6304f34b3e068a93740bfbb830f20b`. La última consulta remota sigue mostrando main en esa base y esta rama; no se dispone de v0.2 publicado para comparar semántica. [SPEC-CAT-001](specs/catalogo-arquitectonico.md) y [ontología estable](architecture/ontologia.md) permanecen intactas. Sus notas originales de propuesta son históricas; esta autorización posterior define el estado de I1.

Se reutilizó código compatible de catálogo/interfaz del prototipo local, sustituyendo completamente su identidad por OIDC y verificando de nuevo esta rama. No se importan ADR de identidad local ni resultados de pruebas previas. [Regla de compatibilidad con v0.2](delivery/compatibilidad-metamodelo-v02.md).

## Pendientes

| ID | Asunto | Estado / cuándo bloquea |
| --- | --- | --- |
| P-01 | Aprobación de Trabajo B. | Resuelto para I1 mediante la evidencia anterior. |
| P-02 | IdP/cliente/membresías de desarrollo. | Resuelto para I1; operación productiva permanece diferida. |
| P-03 | Versiones soportadas y compatibilidad. | Resuelto para I1; seguir mantenimiento de parches mediante revisión. |
| P-04 | Tipos, operaciones, roles y aceptación del catálogo. | Resuelto para I1; revisión del resultado del PR pendiente. |
| P-05 | Validar comprador, usuario y subsegmento financiero. | Priorización comercial y paquete financiero piloto. |
| P-06 | Jurisdicción, materia, fuentes oficiales y revisores. | Implementación y evaluación regulatoria. |
| P-07 | Proveedores, datos, framework y evaluación agéntica. | Ejecución de agentes; no I1. |
| P-08 | Licencias, derechos de contenido y confianza editorial. | Distribución/instalación de paquetes. |
| P-09 | Residencia, retención, recuperación, disponibilidad y operación de identidad/secretos. | Piloto productivo con datos reales. |
| P-10 | Necesidad, aislamiento y viabilidad comercial de Neo4j. | Solo eventual adopción de grafo. |

## Próxima acción

Completar la validación limpia de la refactorización Clean/hexagonal y actualizar la evidencia del PR #1; cualquier fusión corresponde a una decisión humana. Después, definir I2 y comparar v0.2 cuando exista una fuente publicada, sin alterar contratos I1 ni comenzar ese desarrollo por esta autorización. [PLANS](../PLANS.md) mantiene el alcance recomendado.

## Revisión final de integración

Código evaluado `2a27cb8`, con dependencias reinstaladas, cachés vacías, PostgreSQL y Keycloak nuevos. 41 pruebas pasan, contratos y migraciones verificados; se corrigieron permisos de auditoría en variantes de URL, validación Unicode y avisos npm, y se activó validación `date-time` en Python. La [matriz final](delivery/revision-tecnica-final-i1.md) vincula AC-01–14, OIDC-01–07, ADR y resultados sanitizados. SPEC y ontología no cambian. El PR recibe los commits mediante Git; la API REST de GitHub está bloqueada por el proxy, por lo que no se afirma una actualización de su descripción ni una aprobación remota. No se fusiona ni despliega.
