using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aip.Infrastructure.Postgres;
using Aip.Modules.Catalog;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Npgsql;

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
                // Resolve identity only after the complete OIDC protocol (including nonce) succeeds.
                options.Events.OnTicketReceived = async context =>
                {
                    var principal = context.Principal!;
                    var sub = principal.FindFirstValue("sub");
                    var iss = principal.FindFirstValue("iss");
                    if (string.IsNullOrEmpty(sub) || iss != authority) { context.Fail("Identidad no válida"); return; }
                    await using var db = new DbSession(context.HttpContext.RequestServices.GetRequiredService<NpgsqlDataSource>());
                    await db.Begin(Guid.Empty);
                    await db.Execute("SELECT set_config('app.issuer',@issuer,true),set_config('app.external_subject',@subject,true)", ("issuer", iss), ("subject", sub));
                    await db.Execute("INSERT INTO identity.subjects(id,issuer,external_subject) VALUES(@id,@issuer,@subject) ON CONFLICT(issuer,external_subject) DO NOTHING", ("id", Guid.NewGuid()), ("issuer", iss), ("subject", sub));
                    var id = await db.Scalar("SELECT id FROM identity.subjects WHERE issuer=@issuer AND external_subject=@subject AND status='active'", ("issuer", iss), ("subject", sub));
                    if (id is not Guid actor) { context.Fail("Identidad no habilitada"); return; }
                    await db.Transaction.CommitAsync();
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
        app.MapGet("/api/session", async (HttpContext context, IAntiforgery csrf, DbSession db) =>
        {
            object[] organizations = [];
            string? username = null;
            if (context.User.Identity?.IsAuthenticated == true)
            {
                await db.Begin(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!));
                if (await db.Scalar("SELECT id FROM identity.subjects WHERE id=@actor AND status='active'", ("actor", db.ActorId)) is null)
                    throw new ApiProblem(401, "AUTH_SUBJECT", "Identidad revocada");
                organizations = (await db.Rows("SELECT json_build_object('id',o.id,'name',o.name,'role',m.role)::text FROM identity.organizations o JOIN identity.memberships m ON m.organization_id=o.id WHERE m.subject_id=@actor ORDER BY o.name", ("actor", db.ActorId))).Cast<object>().ToArray();
                username = context.User.Identity.Name;
                await db.Transaction.CommitAsync();
            }
            var tokens = csrf.GetAndStoreTokens(context);
            return Results.Ok(new { username, organizations, csrf_token = tokens.RequestToken });
        });
    }
}
