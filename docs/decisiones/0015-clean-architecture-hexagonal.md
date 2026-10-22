# ADR-0015: Clean Architecture y puertos/adaptadores para I1

**Estado:** Aceptado para la refactorización de I1. **Fecha:** 2026-10-21 America/Bogota.
**Requisito confirmado y autorización:** usuario «trabaja el entorno de desarrollo y codigo bajo clean architecture, en una arquitectura exagonal». Complementa ADR-0002, 0003, 0004, 0006, 0010 y 0013; mantiene SPEC-CAT-001 sin cambios.

## Contexto

I1 ya dispone de pruebas sobre PostgreSQL y OIDC reales, pero su servicio de aplicación contiene SQL y recibe HttpContext. El puerto SQL genérico no permite ejecutar casos de uso independientes del transporte y la persistencia. El componente Angular también construye peticiones, cabeceras y formularios OIDC. Cambiar solo nombres de carpetas no establece la dependencia hacia el núcleo.

## Decisión

Mantener un único monolito modular .NET y crear proyectos compilables Domain, Application, Infrastructure y Api. Application depende únicamente de Domain; Domain solo de BCL. Application define comandos, resultados tipados, casos de uso y puertos de catálogo, auditoría, identidad y transacción organizacional. Infrastructure implementa PostgreSQL; Api contiene adaptadores HTTP/OIDC y la raíz de composición. Ningún puerto recibe SQL, HttpContext, JsonObject o tipos Npgsql.

En Angular, separar modelos y casos de uso TypeScript del adaptador HTTP y la presentación. La composición registra el adaptador mediante inyección; CSRF, cookies, URL y ETag se resuelven en el adaptador. El servidor conserva la autoridad sobre permisos y reglas de negocio.

Una solicitud autorizada abre una única transacción. Catálogo y auditoría comparten conexión/contexto; la confirmación ocurre únicamente al completar el adaptador de entrada y la autorización. Las excepciones propagan el fallo y la disposición revierte. Persisten RLS forzada, membresías activas, locks de extremos ordenados y revisiones. OIDC resuelve identidad solo después de la validación completa del protocolo.

## Alternativas y consecuencias

- Mantener carpetas dentro del host: no impide dependencias de aplicación hacia ASP.NET o Npgsql al compilar.
- Añadir microservicios, bus, mediador o repositorio genérico: no resuelve una necesidad de I1 y añade coordinación.
- Capas compilables con puertos específicos: elegida; añade proyectos y mapeos tipados, permite pruebas sin base/servidor y conserva una sola unidad de ejecución.

No se añaden capacidades, entidades, paquetes, agentes ni grafo. Las seis migraciones se trasladan sin alterar bytes/checksums; no hay nueva migración ni cambio de contrato HTTP. Las decisiones operativas productivas siguen pendientes.

## Validación

Grafo de proyectos y restricciones de importación ejecutables; núcleo TypeScript compilado sin DOM/Angular; pruebas de casos de uso con dobles de puertos y pruebas de adaptadores reales; suite limpia de contratos, aislamiento, concurrencia, auditoría y OIDC. El resultado de esta refactorización se registra en [su entrega](../delivery/refactor-clean-hexagonal-i1.md); el dictamen anterior no valida automáticamente el código nuevo.
