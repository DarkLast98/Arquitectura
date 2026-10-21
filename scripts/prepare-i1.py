#!/usr/bin/env python3
"""Local synthetic I1: private TLS, OIDC fixtures, a separate PostgreSQL database."""

from pathlib import Path
import hashlib, json, os, secrets, subprocess, urllib.request

ROOT = Path(__file__).resolve().parents[1]
LOCAL = ROOT / ".local"
DEV = LOCAL / "i1"
TLS = DEV / "tls"
for p in [LOCAL, DEV, TLS]:
    p.mkdir(mode=0o700, parents=True, exist_ok=True)
    p.chmod(0o700)
PG = Path(
    os.environ.get("AIP_PG_BIN", "/workspace/.aip-tools/pg/usr/lib/postgresql/17/bin")
)
KC = Path(os.environ.get("KEYCLOAK_HOME", "/workspace/.aip-tools/keycloak-26.8.1"))


def run(args, **kw):
    return subprocess.run(args, check=True, **kw)


def private(p, value):
    p.write_text(value)
    p.chmod(0o600)


def ssl(args):
    run(["openssl", *args], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


if not (TLS / "ca.crt").exists():
    ssl(
        [
            "req",
            "-x509",
            "-newkey",
            "rsa:3072",
            "-nodes",
            "-keyout",
            str(TLS / "ca.key"),
            "-out",
            str(TLS / "ca.crt"),
            "-days",
            "30",
            "-subj",
            "/CN=AIP I1 Local CA",
            "-addext",
            "basicConstraints=critical,CA:TRUE",
            "-addext",
            "keyUsage=critical,keyCertSign,cRLSign",
        ]
    )
    ssl(
        [
            "req",
            "-newkey",
            "rsa:2048",
            "-nodes",
            "-keyout",
            str(TLS / "server.key"),
            "-out",
            str(TLS / "server.csr"),
            "-subj",
            "/CN=localhost",
        ]
    )
    (TLS / "extensions.cnf").write_text(
        "subjectAltName=DNS:localhost,IP:127.0.0.1\nbasicConstraints=critical,CA:FALSE\nkeyUsage=critical,digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\n"
    )
    ssl(
        [
            "x509",
            "-req",
            "-in",
            str(TLS / "server.csr"),
            "-CA",
            str(TLS / "ca.crt"),
            "-CAkey",
            str(TLS / "ca.key"),
            "-CAcreateserial",
            "-out",
            str(TLS / "server.crt"),
            "-days",
            "30",
            "-extfile",
            str(TLS / "extensions.cnf"),
        ]
    )
    for p in TLS.iterdir():
        p.chmod(0o600)
# OpenSSL/.NET standard trust bundle: retain system roots and add only our local CA.
(TLS / "bundle.pem").write_bytes(
    Path("/etc/ssl/certs/ca-certificates.crt").read_bytes()
    + (TLS / "ca.crt").read_bytes()
)
secrets_path = DEV / "secrets.json"
if not secrets_path.exists():
    private(
        secrets_path,
        json.dumps(
            {
                "db_password": secrets.token_hex(24),
                "client_secret": secrets.token_urlsafe(32),
                "alt_client_secret": secrets.token_urlsafe(32),
                "admin_password": secrets.token_urlsafe(24),
                "passwords": {
                    u: secrets.token_urlsafe(20)
                    for u in ["admin", "architect", "reader", "outsider"]
                },
            },
            indent=2,
        ),
    )
settings = json.loads(secrets_path.read_text())
for i, (realm, client, port) in enumerate(
    [("aip", "aip-bff", 7240), ("aip-alt", "aip-bff-alt", 7241)]
):
    origin = f"https://localhost:{port}"
    issuer = f"https://localhost:8443/realms/{realm}"
    secret = settings["client_secret" if i == 0 else "alt_client_secret"]
    config = {
        "ConnectionStrings": {
            "Aip": f'Host=127.0.0.1;Port=55432;Database=aip_i1;Username=aip_i1_app;Password={settings["db_password"]};Maximum Pool Size=20'
        },
        "Authentication": {
            "PublicOrigin": origin,
            "Oidc": {
                "Authority": issuer,
                "ClientId": client,
                "ClientSecret": secret,
                "CallbackPath": "/signin-oidc",
                "SignedOutCallbackPath": "/signout-callback-oidc",
            },
        },
        "Kestrel": {
            "Certificates": {
                "Default": {
                    "Path": str(TLS / "server.crt"),
                    "KeyPath": str(TLS / "server.key"),
                }
            }
        },
        "Aip": {
            "KeysPath": str(DEV / "keys"),
            "WebRoot": str(ROOT / "web/dist/aip/browser"),
        },
        "Logging": {
            "LogLevel": {
                "Default": "Warning",
                "Microsoft.Hosting.Lifetime": "Information",
            }
        },
    }
    private(
        DEV / ("appsettings.json" if i == 0 else "appsettings-alt.json"),
        json.dumps(config, indent=2),
    )
    users = []
    for n, name in enumerate(["admin", "architect", "reader", "outsider"], 1):
        users.append(
            {
                "id": f"{i:08d}-0000-4000-8000-{n:012d}",
                "username": name,
                "enabled": True,
                "emailVerified": True,
                "firstName": name,
                "lastName": "Demo",
                "email": name + "@example.invalid",
                "credentials": [
                    {
                        "type": "password",
                        "value": settings["passwords"][name],
                        "temporary": False,
                    }
                ],
            }
        )
    document = {
        "realm": realm,
        "enabled": True,
        "sslRequired": "all",
        "registrationAllowed": False,
        "resetPasswordAllowed": False,
        "loginWithEmailAllowed": False,
        "users": users,
        "clients": [
            {
                "clientId": client,
                "enabled": True,
                "protocol": "openid-connect",
                "publicClient": False,
                "secret": secret,
                "standardFlowEnabled": True,
                "directAccessGrantsEnabled": False,
                "serviceAccountsEnabled": False,
                "redirectUris": [origin + "/signin-oidc"],
                "webOrigins": [origin],
                "attributes": {
                    "pkce.code.challenge.method": "S256",
                    "post.logout.redirect.uris": origin + "/signout-callback-oidc",
                },
            }
        ],
    }
    target = KC / "data/import"
    target.mkdir(mode=0o700, parents=True, exist_ok=True)
    private(target / (realm + "-realm.json"), json.dumps(document, indent=2))
test_config = json.loads((DEV / "appsettings.json").read_text())
test_config["Authentication"]["PublicOrigin"] = "https://localhost:7242"
test_config["Authentication"]["Oidc"].update(
    Authority="https://localhost:7443",
    ClientId="negative-test-client",
    ClientSecret="local-test-only",
)
private(DEV / "appsettings-test.json", json.dumps(test_config, indent=2))
private(
    DEV / "keycloak.conf",
    f"db=dev-file\ndb-url=jdbc:h2:file:{DEV}/keycloakdb;NON_KEYWORDS=VALUE\ncache=local\nhttp-host=127.0.0.1\nhttp-management-host=127.0.0.1\nhttp-enabled=false\nhttps-port=8443\nhttps-certificate-file={TLS}/server.crt\nhttps-certificate-key-file={TLS}/server.key\nhostname=https://localhost:8443\n",
)
# Reuse the existing local cluster, but never modify the old prototype database.
env = os.environ | {
    "LD_LIBRARY_PATH": os.environ.get(
        "AIP_PG_LIB", "/workspace/.aip-tools/pg/usr/lib/x86_64-linux-gnu"
    )
    + ":"
    + os.environ.get("LD_LIBRARY_PATH", "")
}
data = LOCAL / "pgdata"
socket = LOCAL / "pgsocket"
socket.mkdir(mode=0o700, exist_ok=True)
if not data.exists():
    owner = LOCAL / "owner.password"
    private(owner, secrets.token_hex(24))
    run(
        [
            str(PG / "initdb"),
            "-D",
            str(data),
            "-U",
            "aip_owner",
            "--auth-local=trust",
            "--auth-host=scram-sha-256",
            "--pwfile=" + str(owner),
        ],
        env=env,
        stdout=subprocess.DEVNULL,
    )
if subprocess.run(
    [str(PG / "pg_ctl"), "-D", str(data), "status"],
    env=env,
    stdout=subprocess.DEVNULL,
    stderr=subprocess.DEVNULL,
).returncode:
    run(
        [
            str(PG / "pg_ctl"),
            "-D",
            str(data),
            "-l",
            str(LOCAL / "postgres.log"),
            "-o",
            f"-h 127.0.0.1 -k {socket} -p 55432",
            "-w",
            "start",
        ],
        env=env,
        stdout=subprocess.DEVNULL,
    )


def owner_sql(sql, db="postgres"):
    return subprocess.check_output(
        [
            str(PG / "psql"),
            "-X",
            "-v",
            "ON_ERROR_STOP=1",
            "-At",
            "-h",
            str(socket),
            "-p",
            "55432",
            "-U",
            "aip_owner",
            "-d",
            db,
        ],
        input=sql,
        text=True,
        env=env,
    )


owner_sql(
    "DO $$ BEGIN IF NOT EXISTS(SELECT FROM pg_roles WHERE rolname='aip_i1_app') THEN CREATE ROLE aip_i1_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS; END IF; END $$; ALTER ROLE aip_i1_app PASSWORD '"
    + settings["db_password"]
    + "';"
)
if owner_sql("SELECT 1 FROM pg_database WHERE datname='aip_i1'").strip() != "1":
    owner_sql("CREATE DATABASE aip_i1;")
from postgres_migrations import build_migration_sql

owner_sql(
    build_migration_sql(ROOT / "src/Aip.Api/Infrastructure/Postgres/Migrations"),
    "aip_i1",
)
# Deterministic identity mappings are fixtures, not auto-provisioning permissions at login.
seed = "INSERT INTO identity.organizations(id,name) VALUES('00000000-0000-4000-8000-00000000000a','Organización A · Demostración'),('00000000-0000-4000-8000-00000000000b','Organización B · Demostración') ON CONFLICT DO NOTHING;\n"
for n, role in enumerate(["organization_admin", "architect", "reader", "reader"], 1):
    uid = f"00000000-0000-4000-8000-{n:012d}"
    seed += f"INSERT INTO identity.subjects(id,issuer,external_subject) VALUES('{uid}','https://localhost:8443/realms/aip','{uid}') ON CONFLICT DO NOTHING;\n"
    for org in (["b"] if n == 4 else ["a", "b"]):
        seed += f"INSERT INTO identity.memberships(organization_id,subject_id,role) VALUES('00000000-0000-4000-8000-00000000000{org}','{uid}','{role}') ON CONFLICT DO NOTHING;\n"
owner_sql(seed, "aip_i1")
print(
    "I1 local preparado: TLS, dos perfiles OIDC y PostgreSQL separado. Secretos privados en .local/i1; sin valores en salida."
)
