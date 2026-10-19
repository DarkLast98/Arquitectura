# Actores y responsabilidades

**Estado:** personas y separación de responsabilidades propuestas; validar con usuarios del piloto. No son todavía perfiles de permisos implementados.

| Actor | Necesidad y responsabilidad | Acceso previsto |
| --- | --- | --- |
| Patrocinador o responsable de estrategia | Conectar iniciativas con objetivos y capacidades; priorizar inversión. | Lectura de vistas y decisiones autorizadas de su organización. |
| Arquitecto empresarial | Mantener lenguaje común, relaciones y vistas transversales. | Edición del catálogo según permisos; propone decisiones. |
| Arquitecto de dominio o solución | Diseñar alternativas y relacionar aplicaciones, datos y restricciones. | Edición y análisis del ámbito autorizado. |
| Especialista financiero | Curar conocimiento sectorial y validar su relevancia de negocio. | Propone contenido de paquete; no certifica obligaciones jurídicas. |
| Especialista legal-regulatorio | Verificar fuentes, vigencia y aplicabilidad; revisar interpretaciones. | Aprobación regulatoria dentro de organización y competencia asignadas. |
| Seguridad y riesgo | Revisar controles, amenazas y evidencia. | Análisis y revisión de cambios autorizados. |
| Revisor de gobierno | Aceptar o rechazar propuestas y decisiones de arquitectura. | Permisos de revisión; no puede autoconcedérselos. |
| Auditor o lector | Consultar relaciones, versiones y evidencia sin modificarlas. | Solo lectura dentro de su organización. |
| Administrador de organización | Gestionar membresías y configuración de su organización. | No accede a otras organizaciones ni aprueba por defecto conclusiones legales. |
| Operador de AIP | Mantener el producto y paquetes públicos curados. | Sin acceso implícito al contenido privado; soporte privilegiado futuro, explícito y auditado. |
| Agente de software | Elaborar propuestas a partir de contexto autorizado. | Identidad técnica acotada, nunca autoridad humana de aprobación. |

## Autoridad y separación

Una persona puede pertenecer a varias organizaciones, pero cada operación debe seleccionar un contexto activo y verificar su membresía y rol. No combinar datos de esas organizaciones en una sola respuesta por compartir usuario.

El propietario comercial, el administrador técnico y el revisor regulatorio son responsabilidades diferentes. La revisión legal debe identificar a la persona, su competencia y el ámbito aprobado; el nivel de independencia entre autor y revisor se acordará para el piloto. No se afirma que una norma exija esta separación sin verificarla.

Para I1 se proponen únicamente `organization_admin`, `architect` y `reader`, con capacidades limitadas al catálogo según [la especificación](../specs/catalogo-arquitectonico.md). Los actores futuros no amplían ese alcance.
