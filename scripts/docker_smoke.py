#!/usr/bin/env python3
"""Read-only smoke checks from the Compose network, with real TLS verification."""
from pathlib import Path
import json
import ssl
from urllib.request import urlopen

tls = ssl.create_default_context(cafile=str(Path("/state/tls/ca.crt")))


def read(url: str):
    with urlopen(url, timeout=12, context=tls) as response:
        if response.status != 200:
            raise SystemExit(f"HTTP {response.status}: {url}")
        return json.load(response)


assert read("https://api:7240/health")["status"] == "ok"
assert read("https://api:7240/contracts/openapi/catalog.v1.json")["openapi"].startswith("3.")
issuer = read("https://auth.localhost:8443/realms/aip/.well-known/openid-configuration")["issuer"]
assert issuer == "https://auth.localhost:8443/realms/aip"
assert read("https://api:7240/api/session")["username"] is None
print("Smoke PASS: BFF/API, contrato, sesión anónima y autoridad OIDC con TLS válido.")
