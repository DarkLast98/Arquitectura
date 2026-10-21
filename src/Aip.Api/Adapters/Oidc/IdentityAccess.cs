using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aip.Modules.Catalog;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Aip.Modules.IdentityAccess;

public static class IdentityAccess
{
    public static void AddIdentityAccess(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        string Required(string key) => string.IsNullOrWhiteSpace(config[key]) ? throw new InvalidOperationException($"Falta configuración {key}") : config[key]!;
        var authority = Required("Authentication:Oidc:Authority").TrimEnd('/');
        var origin = Required("Authentication:PublicOrigin");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var issuer) || issuer.Scheme != "https" ||
            !Uri.TryCreate(origin, UriKind.Absolute, out var publicUri) || publicUri.Scheme != "https" || publicUri.AbsolutePath != "/" || !string.IsNullOrEmpty(publicUri.Query))
            throw new InvalidOperationException("Authority y PublicOrigin deben ser HTTPS absolutos; PublicOrigin no admite rutas ni query.");
        var client = Required("Authentication:Oidc:ClientId");
        var secret = Required("Authentication:Oidc:ClientSecret");
        // Cookies from another provider/client are not trusted, even on the same host.
        var scope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authority + "\n" + client)));
        builder.Services.AddDataProtection().SetApplicationName("AIP.I1." + scope)
            .PersistKeysToFileSystem(new DirectoryInfo(Required("Aip:KeysPath")));
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-aip.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
                options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority;
                options.ClientId = client;
                options.ClientSecret = secret;
                options.RequireHttpsMetadata = true;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.UsePkce = true;
                options.SaveTokens = false;
                options.MapInboundClaims = false;
                options.Scope.Clear(); options.Scope.Add("openid"); options.Scope.Add("profile");
                options.CallbackPath = config["Authentication:Oidc:CallbackPath"] ?? "/signin-oidc";
                options.SignedOutCallbackPath = config["Authentication:Oidc:SignedOutCallbackPath"] ?? "/signout-callback-oidc";
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    context.ProtocolMessage.RedirectUri = origin.TrimEnd('/') + options.CallbackPath;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToIdentityProviderForSignOut = context =>
                {
                    context.ProtocolMessage.PostLogoutRedirectUri = origin.TrimEnd('/') + options.SignedOutCallbackPath;
                    context.ProtocolMessage.ClientId = client;
                    return Task.CompletedTask;
                };
                options.Events.OnTokenValidated = context =>
                {
                    // These claims are cryptographically validated here, then standard claim actions
                    // remove issuer. Carry only identity metadata until full protocol validation ends.
                    var issuerClaim = context.Principal?.FindFirstValue("iss");
                    var subject = context.Principal?.FindFirstValue("sub");
                    if (issuerClaim != authority || string.IsNullOrEmpty(subject)) context.Fail("Identidad no válida");
                    else
                    {
                        context.Properties!.Items["aip:verified_issuer"] = issuerClaim;
                        context.Properties.Items["aip:verified_subject"] = subject;
                    }
                    return Task.CompletedTask;
                };
                // Resolve identity only after the complete OIDC protocol (including nonce) succeeds.
                options.Events.OnTicketReceived = async context =>
                {
                    var principal = context.Principal!;
                    context.Properties!.Items.TryGetValue("aip:verified_subject", out var sub);
                    context.Properties.Items.TryGetValue("aip:verified_issuer", out var iss);
                    context.Properties.Items.Remove("aip:verified_subject"); context.Properties.Items.Remove("aip:verified_issuer");
                    if (string.IsNullOrEmpty(sub) || iss != authority) { context.HandleResponse(); await WriteAuthFailure(context.HttpContext); return; }
                    var identity = context.HttpContext.RequestServices.GetRequiredService<IdentityService>();
                    var id = await identity.ResolveVerifiedSubject(iss, sub);
                    if (id is not Guid actor) { context.HandleResponse(); await WriteAuthFailure(context.HttpContext); return; }
                    // The BFF retains only identity, never access/refresh/ID tokens or provider roles.
                    context.Principal = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim(ClaimTypes.NameIdentifier, actor.ToString()),
                        new Claim(ClaimTypes.Name, principal.FindFirstValue("name") ?? principal.FindFirstValue("preferred_username") ?? "Usuario"),
                        new Claim("aip:issuer", iss), new Claim("aip:subject", sub)
                    ], CookieAuthenticationDefaults.AuthenticationScheme));
                };
                options.Events.OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    return WriteAuthFailure(context.HttpContext);
                };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-aip.csrf";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
    }
    private static async Task WriteAuthFailure(HttpContext context)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { type = "about:blank", title = "No se pudo validar el acceso OIDC", status = 401, code = "AUTH_OIDC", correlation_id = context.TraceIdentifier }, options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
    public static void MapIdentityAccess(this WebApplication app)
    {
        app.MapGet("/auth/login", (HttpContext context, string? returnUrl) =>
        {
            if (returnUrl is not null && (!returnUrl.StartsWith('/') || returnUrl.StartsWith("//") || returnUrl.Contains('\\'))) throw new ApiProblem(400, "AUTH_RETURN_URL", "Retorno inválido");
            return Results.Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/" }, [OpenIdConnectDefaults.AuthenticationScheme]);
        });
        app.MapPost("/auth/logout", async (HttpContext context, IAntiforgery csrf) =>
        {
            await csrf.ValidateRequestAsync(context);
            return Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        }).RequireAuthorization();
        app.MapGet("/api/session", async (HttpContext context, IAntiforgery csrf, IdentityService identity) =>
        {
            IReadOnlyList<OrganizationMembership> organizations = [];
            string? username = null;
            if (context.User.Identity?.IsAuthenticated == true)
            {
                organizations = await identity.Organizations(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!));
                username = context.User.Identity.Name;
            }
            var tokens = csrf.GetAndStoreTokens(context);
            return Results.Ok(new { username, organizations, csrf_token = tokens.RequestToken });
        });
    }
}
