#!/usr/bin/env python3
"""Read-only Compose smoke checks using real TLS verification and bounded startup retries."""
from pathlib import Path
import json
import ssl
import time
from urllib.error import HTTPError, URLError
from urllib.request import urlopen

tls = ssl.create_default_context(cafile=str(Path("/state/tls/ca.crt")))


def read(url: str):
    last = None
    for attempt in range(30):
        try:
            with urlopen(url, timeout=10, context=tls) as response:
                if response.status != 200:
                    raise RuntimeError(f"HTTP {response.status}")
                return json.load(response)
        except (HTTPError, URLError, OSError, ValueError, RuntimeError) as error:
            last = error
            if attempt < 29:
                time.sleep(2)
    raise SystemExit(f"Error al comprobar {url}: {last}")


assert read("https://api:7240/health")["status"] == "ok"
assert read("https://api:7240/contracts/openapi/catalog.v1.json")["openapi"].startswith("3.")
issuer = read("https://auth.localhost:8443/realms/aip/.well-known/openid-configuration")["issuer"]
assert issuer == "https://auth.localhost:8443/realms/aip"
assert read("https://api:7240/api/session")["username"] is None
print("Smoke PASS: BFF/API, contrato, sesión anónima y autoridad OIDC con TLS válido.")
