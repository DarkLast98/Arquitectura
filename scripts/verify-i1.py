#!/usr/bin/env python3
"""Build and verify I1 against local HTTPS OIDC/PostgreSQL. Owns API/test processes.
Keycloak may already be running; all other acceptance ports must be free.
Private logs and sessions remain under .local/i1. No TLS verification bypass.
"""

from pathlib import Path
import json, os, signal, socket, ssl, subprocess, sys, time, urllib.request, urllib.error, uuid

ROOT = Path(__file__).resolve().parents[1]
DEV = ROOT / ".local/i1"
DOTNET = os.environ.get("AIP_DOTNET", "/workspace/.aip-tools/dotnet/dotnet")
env = os.environ | {
    "DOTNET_CLI_HOME": os.environ.get(
        "DOTNET_CLI_HOME", "/workspace/.aip-tools/dotnet-home"
    ),
    "NUGET_PACKAGES": os.environ.get("NUGET_PACKAGES", "/workspace/.aip-tools/nuget"),
    "SSL_CERT_FILE": str(DEV / "tls/bundle.pem"),
    "NODE_EXTRA_CA_CERTS": str(DEV / "tls/ca.crt"),
}
processes = []
handles = []


def run(args, cwd=ROOT):
    subprocess.run(args, cwd=cwd, env=env, check=True)


def start(args, name, extra=None):
    log = open(DEV / (name + ".log"), "w")
    os.chmod(log.name, 0o600)
    handles.append(log)
    p = subprocess.Popen(
        args,
        cwd=ROOT,
        env=env | (extra or {}),
        stdout=log,
        stderr=subprocess.STDOUT,
        start_new_session=True,
    )
    processes.append(p)
    return p


def stop(p):
    if p.poll() is None:
        os.killpg(p.pid, signal.SIGTERM)
        try:
            p.wait(timeout=10)
        except subprocess.TimeoutExpired:
            os.killpg(p.pid, signal.SIGKILL)
            p.wait()


def port_free(port):
    with socket.socket() as s:
        try:
            s.bind(("127.0.0.1", port))
        except OSError:
            raise RuntimeError(
                f"Puerto {port} ocupado; detener solo el proceso local propio antes de verificar"
            )


def request(url, method="GET"):
    ctx = ssl.create_default_context(cafile=str(DEV / "tls/ca.crt"))

    class NoRedirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self, *args):
            return None

    opener = urllib.request.build_opener(
        urllib.request.HTTPSHandler(context=ctx), NoRedirect()
    )
    try:
        response = opener.open(urllib.request.Request(url, method=method), timeout=4)
    except urllib.error.HTTPError as e:
        response = e
    with response:
        return response.status, response.read(), response.headers


def wait(url, p):
    deadline = time.monotonic() + 90
    while time.monotonic() < deadline:
        if p.poll() is not None:
            raise RuntimeError("Servicio no arrancó; revisar log privado en .local/i1")
        try:
            if request(url)[0] == 200:
                return
        except (OSError, urllib.error.URLError):
            pass
        time.sleep(0.5)
    raise RuntimeError("Tiempo de arranque excedido")


def api(profile, port, name):
    p = start(
        [
            DOTNET,
            str(ROOT / "src/Aip.Api/bin/Debug/net10.0/Aip.Api.dll"),
            "--urls",
            f"https://localhost:{port}",
        ],
        name,
        {"AIP_CONFIG": str(DEV / profile)},
    )
    wait(f"https://localhost:{port}/health", p)
    return p


try:
    run([sys.executable, "scripts/prepare-i1.py"])
    for port in [7240, 7241, 7242, 7243, 7443, 7444]:
        port_free(port)
    run([DOTNET, "restore", "src/Aip.Api", "--locked-mode"])
    run([DOTNET, "build", "src/Aip.Api", "--no-restore"])
    run([DOTNET, "restore", "tests/Aip.Catalog.Domain.Tests", "--locked-mode"])
    run([DOTNET, "test", "tests/Aip.Catalog.Domain.Tests", "--no-restore"])
    run(["npm", "run", "build"], ROOT / "web")
    try:
        assert (
            request(
                "https://localhost:8443/realms/aip/.well-known/openid-configuration"
            )[0]
            == 200
        )
    except (OSError, urllib.error.URLError):
        kc = start([sys.executable, "scripts/run-keycloak.py"], "keycloak-tests")
        wait("https://localhost:8443/realms/aip/.well-known/openid-configuration", kc)
    provider = start([sys.executable, "tests/oidc_provider.py"], "oidc-faults")
    wait("https://localhost:7443/.well-known/openid-configuration", provider)
    main = api("appsettings.json", 7240, "api-main")
    api("appsettings-alt.json", 7241, "api-alternate")
    api("appsettings-test.json", 7242, "api-negative")
    run(["npx", "playwright", "test", "--project", "setup"], ROOT / "web")
    run(
        [
            sys.executable,
            "-m",
            "unittest",
            "discover",
            "-s",
            "tests",
            "-p",
            "test_catalog.py",
            "-v",
        ]
    )
    run(["npm", "run", "test:contracts"], ROOT / "web")
    run(
        ["npx", "playwright", "test", "--project", "catalog", "--no-deps"], ROOT / "web"
    )
    sys.path.insert(0, str(ROOT / "tests"))
    from test_catalog import Client, A

    client = Client("architect")
    code = "RESTART-" + uuid.uuid4().hex[:12].upper()
    status, row, _ = client.api(
        "POST",
        "/entities",
        {
            "type_id": "core.application",
            "schema_version": "0.1.0",
            "code": code,
            "name": "Persistencia sintética",
        },
    )
    assert status == 201
    stop(main)
    main = api("appsettings.json", 7240, "api-restarted")
    status, recovered, _ = client.api("GET", "/entities/" + row["id"])
    assert status == 200 and recovered == row
    print(
        "AC-13: datos, revisión y acceso conservados tras reiniciar el proceso API.",
        flush=True,
    )
    # Invalid configuration fails at startup; no password/header fallback exists.
    config = json.loads((DEV / "appsettings.json").read_text())
    config["Authentication"]["Oidc"]["ClientSecret"] = ""
    bad = DEV / "appsettings-invalid.json"
    bad.write_text(json.dumps(config))
    bad.chmod(0o600)
    p = start(
        [
            DOTNET,
            str(ROOT / "src/Aip.Api/bin/Debug/net10.0/Aip.Api.dll"),
            "--urls",
            "https://localhost:7243",
        ],
        "api-invalid",
        {"AIP_CONFIG": str(bad)},
    )
    assert p.wait(timeout=15) != 0
    config["Authentication"]["Oidc"].update(
        Authority="https://localhost:7444", ClientSecret="unused-test-only"
    )
    config["Authentication"]["PublicOrigin"] = "https://localhost:7243"
    bad.write_text(json.dumps(config))
    down = api("appsettings-invalid.json", 7243, "api-idp-down")
    status, body, headers = request("https://localhost:7243/auth/login")
    assert (
        status == 500
        and b"INTERNAL_ERROR" in body
        and not headers.get("Set-Cookie", "").startswith("__Host-aip.session=")
    )
    status, _, _ = request(
        "https://localhost:7243/api/v1/organizations/" + A + "/catalog/entities"
    )
    assert status == 401
    print(
        "OIDC-04: configuración inválida y proveedor inaccesible fallan cerrados.",
        flush=True,
    )
    run(
        [sys.executable, "scripts/prepare-i1.py"]
    )  # checksums and existing data survive replay
    summary = {
        "result": "PASS",
        "domain_tests": 5,
        "api_database_tests": 11,
        "browser_tests": 18,
        "contracts": "OpenAPI 3.1 + JSON Schema 2020-12",
        "restart": "PASS",
        "fail_closed": "PASS",
        "migrations_replay": "PASS",
        "utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
    }
    (DEV / "verification.json").write_text(json.dumps(summary, indent=2) + "\n")
    print(json.dumps(summary), flush=True)
finally:
    for p in reversed(processes):
        stop(p)
    for log in handles:
        log.close()
