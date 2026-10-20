# Registro de decisiones de AIP

Los ADR técnicos y de diseño están **propuestos**, incluso cuando recogen la dirección inicial indicada por el usuario. El único acuerdo aceptado es el proceso de definir antes de implementar; no aprueba tecnologías ni habilita código.

| ID | Tema | Estado | Aplicación |
| --- | --- | --- | --- |
| [ADR-0001](0001-definir-antes-de-implementar.md) | Definir antes de implementar. | Aceptada: instrucción explícita del usuario. | Todas las fases. |
| [ADR-0002](0002-backend-modular.md) | Backend inicialmente como monolito modular. | Propuesto | I1. |
| [ADR-0003](0003-angular-typescript.md) | Frontend Angular y TypeScript. | Propuesto | I1. |
| [ADR-0004](0004-dotnet.md) | .NET para casos de uso y API de dominio. | Propuesto | I1. |
| [ADR-0005](0005-python-agentes.md) | Python para agentes detrás de contratos. | Propuesto | I4; no bloquea I1. |
| [ADR-0006](0006-postgresql.md) | PostgreSQL como fuente transaccional de verdad. | Propuesto | I1. |
| [ADR-0007](0007-neo4j-evaluacion.md) | Evaluar Neo4j; no adoptarlo todavía. | Propuesto | Evaluación posterior. |
| [ADR-0008](0008-contratos.md) | OpenAPI y JSON Schema versionados. | Propuesto | I1, ampliable. |
| [ADR-0009](0009-documentacion.md) | Markdown, Mermaid y ADR en Git. | Propuesto | Fundación documental. |
| [ADR-0010](0010-aislamiento-organizaciones.md) | Aislamiento lógico por organización con defensa en base. | Propuesto | I1. |
| [ADR-0011](0011-core-paquetes.md) | Core transversal y paquetes declarativos versionados. | Propuesto | Fronteras desde I1; motor en I2. |
| [ADR-0013](0013-oidc-desarrollo-configurable.md) | OIDC configurable con Code + PKCE y BFF. | Propuesto | Trabajo B; requiere acuerdo del proveedor y autorización del plan. |
| [ADR-0014](0014-versiones-soportadas-i1.md) | Combinación estable y soportada del toolchain. | Propuesto | Trabajo B; fuentes oficiales y revalidación antes de instalar. |

El número 0012 está reservado por el registro local de identidad de `work/52c653d`, que no se importa a esta rama. Los ADR-0013/0014 son propuestas de [PLAN-I1-B](../delivery/plan-implementacion-i1-trabajo-b.md); ningún estado existente cambia con este PR.

## Evolución

Crear el siguiente número disponible; registrar fecha, fuente, contexto, decisión propuesta, alternativas, consecuencias, validación necesaria y aprobación pendiente o evidencia real. No cambiar el estado a aceptado por el mero hecho de implementar o usar una herramienta.

Una decisión aceptada conserva su historia. Si se reemplaza, registrar el nuevo ADR y enlazarlo desde el anterior. Requisitos y especificaciones deben citar decisiones aplicables. La aprobación de un incremento debe indicar qué ADR acepta y cuáles permanecen diferidos.
