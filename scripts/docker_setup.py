#!/usr/bin/env python3
"""Idempotent private development-only certificates, credentials and OIDC fixtures."""
from pathlib import Path
import json
import os
import secrets
import subprocess

STATE = Path("/state")
TLS = STATE / "tls"
REALMS = STATE / "realms"
for folder in (STATE, TLS, REALMS):
    folder.mkdir(parents=True, exist_ok=True)
    folder.chmod(0o750)
KEYS = STATE / "keys"
KEYS.mkdir(exist_ok=True)
KEYS.chmod(0o770)


def private(path: Path, content: str):
    path.write_text(content)
    path.chmod(0o640)


def openssl(*args: str):
    subprocess.run(["openssl", *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


secrets_file = STATE / "secrets.json"
if not secrets_file.exists():
    creds = {
        "owner_password": secrets.token_hex(32),
        "app_password": secrets.token_hex(32),
        "client_secret": secrets.token_urlsafe(32),
        "admin_password": secrets.token_urlsafe(32),
        "passwords": {name: secrets.token_urlsafe(20) for name in ("admin", "architect", "reader", "outsider")},
    }
    private(secrets_file, json.dumps(creds, indent=2))
creds = json.loads(secrets_file.read_text())
private(STATE / "postgres-owner-password", creds["owner_password"] + "\n")
private(STATE / "kc-admin-password", creds["admin_password"] + "\n")

if not (TLS / "ca.crt").exists() or not (TLS / "server.crt").exists():
    if any(TLS.iterdir()):
        raise SystemExit("Certificados incompletos; revisa /state/tls, no regeneres parcialmente")
    openssl(
        "req", "-x509", "-newkey", "rsa:3072", "-nodes",
        "-keyout", str(TLS / "ca.key"), "-out", str(TLS / "ca.crt"),
        "-days", "30", "-subj", "/CN=AIP Docker Local CA",
        "-addext", "basicConstraints=critical,CA:TRUE",
        "-addext", "keyUsage=critical,keyCertSign,cRLSign",
    )
    openssl(
        "req", "-newkey", "rsa:2048", "-nodes",
        "-keyout", str(TLS / "server.key"), "-out", str(TLS / "server.csr"),
        "-subj", "/CN=localhost",
    )
    (TLS / "extensions.cnf").write_text(
        "subjectAltName=DNS:localhost,DNS:auth.localhost,DNS:keycloak,DNS:api,IP:127.0.0.1\n"
        "basicConstraints=critical,CA:FALSE\n"
        "keyUsage=critical,digitalSignature,keyEncipherment\n"
        "extendedKeyUsage=serverAuth\n"
    )
    openssl(
        "x509", "-req", "-in", str(TLS / "server.csr"),
        "-CA", str(TLS / "ca.crt"), "-CAkey", str(TLS / "ca.key"),
        "-CAcreateserial", "-out", str(TLS / "server.crt"),
        "-days", "30", "-extfile", str(TLS / "extensions.cnf"),
    )

# The browser trusts the CA via an explicit user action; .NET gets the same CA
# without disabling certificate validation.
(TLS / "bundle.pem").write_bytes(
    Path("/etc/ssl/certs/ca-certificates.crt").read_bytes() + b"\n" + (TLS / "ca.crt").read_bytes()
)
for file in TLS.iterdir():
    file.chmod(0o640 if file.name.endswith((".crt", ".pem", ".key")) else 0o640)
(TLS / "ca.key").chmod(0o600)

issuer = "https://auth.localhost:8443/realms/aip"
origin = "https://localhost:7240"
settings = {
    "ConnectionStrings": {
        "Aip": f"Host=postgres;Port=5432;Database=aip_i1;Username=aip_i1_app;Password={creds['app_password']};Maximum Pool Size=20"
    },
    "Authentication": {
        "PublicOrigin": origin,
        "Oidc": {
            "Authority": issuer,
            "ClientId": "aip-bff",
            "ClientSecret": creds["client_secret"],
            "CallbackPath": "/signin-oidc",
            "SignedOutCallbackPath": "/signout-callback-oidc",
        },
    },
    "Kestrel": {
        "Certificates": {"Default": {"Path": "/state/tls/server.crt", "KeyPath": "/state/tls/server.key"}}
    },
    "Aip": {"KeysPath": "/state/keys", "WebRoot": "/app/web"},
    "Logging": {"LogLevel": {"Default": "Warning", "Microsoft.Hosting.Lifetime": "Information"}},
}
private(STATE / "appsettings.json", json.dumps(settings, indent=2, ensure_ascii=False))

users = []
for i, name in enumerate(("admin", "architect", "reader", "outsider"), 1):
    users.append({
        "id": f"00000000-0000-4000-8000-{i:012d}",
        "username": name, "enabled": True, "emailVerified": True,
        "firstName": name, "lastName": "Demo", "email": name + "@example.invalid",
        "credentials": [{"type": "password", "value": creds["passwords"][name], "temporary": False}],
    })
realm = {
    "realm": "aip", "enabled": True, "sslRequired": "all",
    "registrationAllowed": False, "resetPasswordAllowed": False, "loginWithEmailAllowed": False,
    "users": users,
    "clients": [{
        "clientId": "aip-bff", "enabled": True, "protocol": "openid-connect",
        "publicClient": False, "secret": creds["client_secret"],
        "standardFlowEnabled": True, "directAccessGrantsEnabled": False,
        "serviceAccountsEnabled": False,
        "redirectUris": [origin + "/signin-oidc"],
        "webOrigins": [origin],
        "attributes": {
            "pkce.code.challenge.method": "S256",
            "post.logout.redirect.uris": origin + "/signout-callback-oidc",
        },
    }],
}
private(REALMS / "aip-realm.json", json.dumps(realm, indent=2, ensure_ascii=False))
print("AIP Docker: secretos, TLS y realm de desarrollo preparados en volumen privado.")
