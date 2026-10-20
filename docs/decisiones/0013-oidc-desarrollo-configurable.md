# ADR-0013: OIDC configurable para el desarrollo de I1

**Estado:** Propuesto. **Fecha:** 2026-10-19. **Origen:** Trabajo B, decisión propuesta 1; P-02 y ADR-0010. **Implementación:** no autorizada por este ADR.

El número 0012 se reserva para el registro de identidad local existente solo en `work/52c653d`, no incorporado a `main`. Este ADR no cambia su historia ni lo declara sustituido como decisión aceptada.

## Contexto

Trabajo B requiere OIDC con proveedor configurable antes de implementar. El prototipo local previo usa contraseñas y no satisface ese requisito. Identidad, membresía y autorización de catálogo deben conservar responsabilidades separadas.

## Decisión propuesta

Usar el handler oficial `Microsoft.AspNetCore.Authentication.OpenIdConnect` con cliente confidencial, Authorization Code y PKCE. El backend .NET actúa como BFF; Angular consume una sesión de mismo origen mediante cookie protegida. No crear un emisor propio, login con contraseñas de AIP ni un bypass de cabeceras.

Recomendar Keycloak para el proveedor de desarrollo, o reutilizar un IdP OIDC existente que cumpla la misma prueba. La versión de ese proveedor, el registro de cliente y sus datos aún necesitan acuerdo/validación. No desplegarlo como infraestructura en esta entrega de planificación.

## Configuración por entorno

| Opción propuesta | Uso |
| --- | --- |
| `Authentication:Oidc:Authority` | Issuer/discovery aprobado para el entorno; nunca recibido del usuario de una petición. |
| `Authentication:Oidc:ClientId` | Cliente registrado para el BFF. |
| `Authentication:Oidc:ClientSecret` | Credencial de cliente confidencial, en user-secrets/archivo privado o almacén seguro; nunca un valor en Git. |
| `Authentication:Oidc:CallbackPath` | `/signin-oidc`, registrado exactamente en el IdP. |
| `Authentication:Oidc:SignedOutCallbackPath` | `/signout-callback-oidc`, registrado según el proveedor. |
| `Authentication:PublicOrigin` | Origen HTTPS local fijo y permitido, por ejemplo `https://localhost:7240`; no derivado libremente de Host/returnUrl. |
| Scopes | `openid profile`; agregar otros solo con necesidad y revisión. |

La aplicación comprueba configuración al inicio y exige metadata HTTPS. Confiar el certificado local mediante mecanismos de trust soportados; no desactivar validación TLS, firma, issuer, audience, nonce o state. Certificados, secretos y claves de protección de cookies quedan fuera del repositorio.

Un Authority por perfil de entorno en I1; no hay selección dinámica de issuer por organización/petición. Probar un segundo perfil demuestra configuración intercambiable, sin requerir multi-IdP concurrente. Un cambio de issuer necesita mapeos de sujeto/membresía explícitos.

## Flujo y límites de confianza

1. GET `/auth/login` inicia un challenge con retorno únicamente local.
2. El handler mantiene state/nonce y PKCE, intercambia el código en backend y valida la respuesta OIDC.
3. Resolver sujeto interno por `(issuer verificado, sub)`; no por email ni nombre. Un sujeto nuevo no obtiene membresía o rol automáticamente.
4. Emitir sesión protegida por ASP.NET. La API verifica sesión y consulta membresía/rol activos en PostgreSQL por petición.
5. POST `/auth/logout` exige antiforgery, termina la sesión local y usa signout del IdP cuando esté soportado y configurado.

Cookies HttpOnly y Secure; política SameSite compatible con el flujo, incluyendo cookies de correlación/nonce del handler. No poner tokens en localStorage/sessionStorage ni hacerlos credencial de JavaScript. No guardar access/refresh tokens en la cookie: I1 no llama APIs downstream. Si el proveedor exige UserInfo, limitar claims, permisos y retención y validarlo explícitamente.

El catálogo usa la sesión del BFF; no trata un ID token recibido como bearer de API. No se confunde audience del ID token (cliente OIDC) con una audiencia de API no usada. Las APIs responden 401/403 JSON y no redirigen a login; el challenge solo ocurre en endpoints de navegación de acceso.

Aplicar CSRF a todas las mutaciones basadas en cookie, sin confiar solo en SameSite. CORS cerrado al mismo origen, claves privadas de sesión, caducidad explícita, no logs de tokens/códigos y validación de returnUrl/forwarded headers. No confiar roles del IdP como permisos de catálogo sin un futuro mapeo aprobado.

## Alternativas

- SPA pública con Code + PKCE y access tokens para API: viable con diseño de audiencias, almacenamiento, renovación y CORS; añade superficie que I1 no necesita.
- Contraseñas locales del prototipo: no satisface la instrucción OIDC de Trabajo B; no se importa como fallback.
- Acoplamiento a un SDK exclusivo de proveedor: dificulta configurabilidad; usarlo solo si un requisito verificado lo exige y se revisa la decisión.

## Consecuencias y validación

Requiere un IdP de desarrollo, HTTPS confiable, registro de callbacks y credencial del cliente. Las membresías siguen bajo control de AIP. El perfil local con OIDC no implica identidad empresarial/productiva aprobada.

Probar login/logout real, cambio de perfil sin recompilar, sujeto sin membresía, emisor/firma/audiencia/tiempo/state/nonce incorrectos, CSRF, sesión revocada/contexto y respuestas API sin redirect. El handler de .NET 10 puede usar PAR si el proveedor lo anuncia: validar esa variante y la renovación de JWKS. No deshabilitar controles para resolver incompatibilidades.

La fuente técnica consultada es [la documentación oficial ASP.NET Core sobre OIDC y BFF](https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/security/authentication/configure-oidc-web-authentication.md), obtenida mediante su contenido raw. La documentación confirma el patrón; la interoperabilidad con el proveedor elegido sigue pendiente de prueba real.

**Aprobación:** pendiente en el contexto de PLAN-I1-B; este documento no registra aceptación ni implementación.
