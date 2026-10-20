# ADR-0014: combinación de versiones soportadas para I1

**Estado:** Aceptado para I1 / Trabajo B. **Fecha de consulta:** 2026-10-19 (America/Bogota). **Origen:** Trabajo B, decisión propuesta 2; ADR-0003/0004/0006/0008.

## Contexto y propuesta

Seleccionar versiones estables con soporte comprobable y compatibilidad entre herramientas, sin crear todavía manifiestos, lockfiles ni instalaciones. La existencia de binarios o pruebas en el prototipo local no constituye aprobación ni validación de esta rama.

| Componente | Versión candidata | Evidencia de soporte/compatibilidad |
| --- | --- | --- |
| Angular | 22.2.3 | Rama 22 figura Active en la política oficial; ventana publicada hasta junio 2027 activa y junio 2028 LTS. La tabla de compatibilidad y el manifiesto real 22.2.3 permiten Node ^24.15.0. |
| Node | 24.19.0 | Release estable en índice oficial; rama 24 LTS, fin publicado 2028-04-30. Cumple el rango de Angular seleccionado. |
| TypeScript | 6.0.3 | Versión estable disponible en npm; satisface >=6.0 <6.1 declarado por compiler-cli 22.2.3. Compilador y paquetes Angular deben compartir versión. |
| .NET | SDK 10.0.402 / runtime 10.0.13 | Metadata oficial de .NET 10: soporte activo, LTS hasta 2028-11-14. Usar paquetes ASP.NET/OIDC compatibles con esa rama. |
| PostgreSQL | 17.11 | Notas oficiales: release estable 2026-08-13. Major 17 se publicó 2024-09-26 y está dentro de la política oficial de soporte de cinco años. No depende de elegir la major más nueva. |

No se presentan las fechas mensuales de Angular como un día exacto. Para PostgreSQL no se inventa una fecha final específica: la ventana de soporte se acredita con política y release inicial. Antes de implementar, revalidar parches vigentes y avisos; proponer cualquier ajuste que afecte compatibilidad. «Estable» no significa «última major».

OpenAPI 3.1 y JSON Schema 2020-12 son candidatos de contrato de ADR-0008, compatibles a nivel de diseño con campos/operaciones de SPEC-CAT-001. La selección de librerías de validación, generación y adaptador PostgreSQL se comprobará con sus dependencias reales al autorizar B0; no se afirma compatibilidad de ejecución aún no probada en Trabajo B.

## Fuentes oficiales verificadas

Se leyeron los contenidos de estas fuentes; páginas raw de GitHub se utilizaron cuando el sitio renderizado estaba restringido:

- [Angular: compatibilidad](https://github.com/angular/angular/blob/main/adev/src/content/reference/versions.md) y [política de releases](https://github.com/angular/angular/blob/main/adev/src/content/reference/releases.md).
- [Manifiesto publicado de Angular 22.2.3](https://registry.npmjs.org/@angular/core/22.2.3) y [compiler-cli 22.2.3](https://registry.npmjs.org/@angular/compiler-cli/22.2.3): engines y peerDependencies; no se instalaron paquetes del producto.
- [Calendario oficial de Node](https://github.com/nodejs/Release/blob/main/schedule.json) e [índice oficial de releases](https://nodejs.org/dist/index.json): rama 24 y existencia de v24.19.0.
- [Metadata oficial .NET 10](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json): latest-sdk, latest-runtime, support-phase y eol-date.
- [Política PostgreSQL, fuente del sitio oficial](https://github.com/postgres/pgweb/blob/master/templates/support/versioning.html), [release inicial 17](https://github.com/postgres/postgres/blob/REL_17_0/doc/src/sgml/release-17.sgml) y [notas de 17.11 en rama estable](https://github.com/postgres/postgres/blob/REL_17_STABLE/doc/src/sgml/release-17.sgml).

Las ramas web son fuentes vivas. Fijar versiones exactas y hashes/integridad de artefactos al instalar después de aprobación; conservar lockfiles de producto y registrar de nuevo la consulta si las versiones cambian. No desactivar firmas, checksums o TLS para hacer pasar la instalación.

## Alternativas y consecuencias

Angular 21 LTS u otra combinación soportada pueden ser alternativas si surge una restricción verificada. Node no-LTS o herramientas preview no aportan una necesidad conocida. PostgreSQL 18/19 no se introduce solo por novedad: 17 cubre transacciones, integridad y RLS del catálogo.

La combinación propuesta conserva la dirección tecnológica y no altera tipos, campos o relaciones estables. Una futura actualización mayor tendrá revisión de soporte, contratos y regresión. Neo4j y Python/agentes no son dependencias de I1.

**Aprobación:** usuario «aprobado, implementa el PR», referido al PR #1 / PLAN-I1-B v1.0 en `469f2dc`. Aceptación limitada a I1; capacidades futuras y despliegue siguen diferidos.

## Verificación tras aprobación

Las versiones de la tabla se materializan en `global.json`, `.nvmrc` y manifests/lockfiles de .NET/npm; compilación y pruebas reales de [Trabajo B](../delivery/evidencia-i1-trabajo-b.md) demuestran compatibilidad de esa combinación. Adaptador Npgsql 10.0.3 y manejador OIDC 10.0.13. Keycloak 26.8.1/JDK 21 se concreta en ADR-0013. Las referencias a «antes de implementar» describen la propuesta original, no un bloqueo vigente. El soporte declarado no equivale a una certificación productiva.
