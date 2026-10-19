# ADR-0001: definir antes de implementar

**Estado:** aceptada por instrucción explícita del usuario.

**Fecha:** 2026-10-19.

**Tipo:** proceso de trabajo.

## Contexto

El repositorio parte de un README, sin aplicación. El usuario solicita un entorno agéntico y quiere describir y definir primero el proyecto. Su instrucción es: «una vez establecida la arquitectura, alcance y definición procederemos con la codificación».

## Opciones

- **Definir y acordar primero:** permite decidir producto, límites y diseño con una base compartida. Es el enfoque solicitado.
- **Comenzar un prototipo de aplicación inmediatamente:** exigiría asumir funcionalidades y tecnologías todavía no acordadas. Se descarta para esta fase.

## Decisión

Preparar reglas y documentos para definir negocio, producto, alcance, requisitos y arquitectura. Mantener pendiente la implementación hasta establecer esas definiciones con el usuario y registrar el acuerdo en el estado del proyecto.

## Consecuencias

- Se puede avanzar en documentación, diagramas, alternativas y criterios de aceptación.
- No se crean todavía código de aplicación, dependencias ni infraestructura.
- La arquitectura de la aplicación y la forma de ejecutar agentes se decidirán a partir del problema y los requisitos.
- El diseño puede evolucionar; los cambios significativos quedarán registrados junto con su impacto.

## Acuerdo

La instrucción del usuario acepta el orden de trabajo. No constituye aprobación de un producto concreto, sus funcionalidades ni su arquitectura técnica.
