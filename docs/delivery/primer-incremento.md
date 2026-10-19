# I1 — Catálogo arquitectónico manual por organización

**Estado:** propuesto, implementación no autorizada. **Backlog:** BL-00–BL-05. **Especificación:** [catálogo](../specs/catalogo-arquitectonico.md).

## Resultado exacto

Un arquitecto autenticado selecciona su organización, registra una capacidad, una aplicación y un activo de datos, y conecta la aplicación con los otros dos. Puede consultarlos, cambiar sus descripciones con control de revisión y archivar registros respetando integridad. Un lector consulta sin escribir; el administrador organizacional consulta además la auditoría del catálogo.

Es un recorrido Angular → API .NET modular → PostgreSQL. No es solo un CRUD sin límites de acceso ni una pantalla con datos simulados. Los ejemplos son sintéticos y no se almacenan datos financieros operativos.

## Incluido

- Dos organizaciones de demostración, usuarios autenticados y membresías verificadas; mecanismo de identidad pendiente de acuerdo.
- Roles `organization_admin`, `architect`, `reader`; sin interfaz de administración de membresías.
- Registro fijo `core.catalog` v0.1.0 con tres tipos de entidad y dos relaciones dirigidas.
- Crear, listar, consultar, editar campos permitidos y archivar entidades; crear, consultar y archivar relaciones.
- Filtros, paginación, validación de contrato/dominio, errores uniformes, ETag/If-Match y auditoría transaccional.
- Pantallas sencillas de listado, detalle, formulario y relaciones; selección de organización autorizada y manejo de error/conflicto.
- Contratos versionados y pruebas de aceptación, integración con PostgreSQL y aislamiento con rol real.
- Instrucciones reproducibles de ejecución local y validación; sin desplegar infraestructura.

## Excluido

Agentes, llamadas a modelos, Neo4j, diagramador visual, importación masiva, paquetes instalables, tipos personalizados, edición de esquemas, documentos adjuntos, workflows de aprobación, evaluación normativa, cobros, SSO empresarial completo, despliegue cloud y administración productiva de clientes. No hay borrado físico ni restauración de archivados en I1.

## Escenario de demostración

En la organización ficticia A, crear «Gestión de pagos» (`core.business_capability`), «Portal de pagos de demostración» (`core.application`) y «Metadatos de instrucciones de pago» (`core.data_asset`). Conectar aplicación → capacidad mediante `core.supports` y aplicación → dato mediante `core.uses`. No cargar instrucciones de pago reales.

Cambiar a la organización ficticia B y comprobar que no aparecen esos elementos ni se pueden recuperar mediante sus UUID, búsquedas, conteos o relaciones. Volver a A con lector y comprobar que solo puede consultar. Mostrar un conflicto de revisión y la secuencia correcta de archivo de relaciones antes de archivar una entidad.

## Aceptación del incremento

Cumplir AC-01–AC-14 de la especificación con evidencia, incluyendo consultas directas bajo el rol real de base, conexión reutilizada, concurrencia e intento de relación entre organizaciones. Las pruebas de interfaz deben usar API y persistencia reales en el recorrido representativo.

## Condiciones previas y de cierre

Antes de implementar: aprobación explícita de alcance y decisiones aplicables, identidad acordada, versiones soportadas elegidas y contratos revisados. ADR aplicables: 0002, 0003, 0004, 0006, 0008, 0010 y fronteras de 0011. ADR-0009 corresponde a la convención documental; Python y Neo4j no necesitan ser aprobados para I1.

Para cerrar: aceptación demostrada, instrucciones locales reproducibles, migraciones y pruebas —cuando estén autorizadas— revisadas, sin datos reales ni credenciales en el repositorio, pendientes productivos identificados. El cierre técnico no equivale a disponibilidad para producción ni autoriza I2.
