# Plan de evolución de AIP

**I1 / Trabajo B autorizado e implementado; pendiente revisión del PR #1.** Evidencia: usuario «aprobado, implementa el PR», referido a PLAN-I1-B v1.0 en `469f2dc`. No se autoriza fusión automática, despliegue ni I2.

## Trabajo B

[Plan aprobado](docs/delivery/plan-implementacion-i1-trabajo-b.md), [resultado verificable](docs/delivery/evidencia-i1-trabajo-b.md), [ejecución local](docs/delivery/ejecucion-local-i1.md). Rama `feature/i1-catalog-foundation`, creada desde `main/25c4fcc`.

| Etapa de I1 | Resultado | Estado |
| --- | --- | --- |
| B0 | Aprobación registrada, versiones fijadas, contratos, OIDC configurable y migraciones locales. | Implementado y verificado. |
| B1 | Recorrido completo capacidad/aplicación/relación con membresía, RLS, revisión y auditoría. | Implementado y verificado. |
| B2 | Tres entidades, dos relaciones, operaciones y pantallas de SPEC-CAT-001. | Implementado y verificado. |
| B3 | Contratos, concurrencia, reinicio y seguridad OIDC; instrucciones y evidencia. | Verificación técnica superada; revisión humana del PR pendiente. |

Se reutilizaron partes compatibles del prototipo `work/52c653d`; su autenticación por contraseña no se importa y su evidencia no valida esta rama. Cada prueba reportada corresponde a la implementación OIDC actual. La [frontera con v0.2](docs/delivery/compatibilidad-metamodelo-v02.md) se conserva.

## Resoluciones y pendientes

| ID | Resolución / estado |
| --- | --- |
| P-01 | Resuelto: aprobación explícita de PLAN-I1-B v1.0 / PR #1 registrada en [estado](docs/00-estado.md). |
| P-02 | Resuelto para desarrollo: Keycloak 26.8.1, dos clientes/realms HTTPS, Code + PKCE/PAR y membresías verificadas en servidor/base. |
| P-03 | Resuelto para I1: versiones soportadas de ADR-0014 fijadas y compiladas; dependencias con lockfiles. |
| P-04 | Resuelto para I1: SPEC-CAT-001 v0.1.0 aprobada en el alcance del plan y conservada intacta. |
| P-05–P-10 | Mercado financiero, jurisdicción, agentes, derechos de paquetes, operación productiva y necesidad de grafo continúan diferidos en [estado](docs/00-estado.md). |

## Evolución posterior

| Etapa | Resultado orientativo | Condición de salida |
| --- | --- | --- |
| D0 | Fundación de producto, arquitectura y catálogo. | Completada; decisiones y límites documentados. |
| I1 | Catálogo manual aislado por organización. | Pruebas superadas y revisión humana del PR. |
| I2 | Paquetes de conocimiento versionados y activación organizacional. | Alcance/ADR aprobados y compatibilidad con registros I1 demostrada. |
| I3 | Evidencia, vistas y gobierno desde estrategia hasta solución. | Revisión ligada a versiones exactas y evidencia recuperable. |
| I4 | Un caso asistido acotado por agentes. | Calidad, límites y aprobación antes de persistir. |
| I5 | Especialización financiera y corpus jurisdiccional acotado. | Fuentes verificadas, aplicabilidad, vigencia y revisión profesional. |

I2–I5 son propuestas, sin fechas ni capacidad asignadas. No se introducen Neo4j, agentes o entidades futuras como dependencia del catálogo.

## Siguiente incremento recomendado

Tras revisar y fusionar I1 mediante decisión humana, preparar **solo la definición de I2**: comparar el metamodelo v0.2 publicado, mapear diferencias con los cinco tipos estables, definir manifiesto/versionado de paquetes y activación por organización y registrar ADR propuestos de compatibilidad/migración. Mantener datos y contratos I1 durante la transición. Implementar I2 requiere una nueva aprobación expresa.
