"""Private fixture administration; never prints credentials, tokens or cookies."""

from pathlib import Path
import json, ssl, sys, urllib.request

ROOT = Path(__file__).resolve().parents[1]
DEV = ROOT / ".local/i1"
sys.path.insert(0, str(ROOT / "tests"))
from test_catalog import db_sql
from urllib.parse import urlencode

secrets = json.loads((DEV / "secrets.json").read_text())
ctx = ssl.create_default_context(cafile=str(DEV / "tls/ca.crt"))
action = sys.argv[1]
if action in ["revoke", "restore", "revoke-subject", "restore-subject"]:
    status = "revoked" if action.startswith("revoke") else "active"
    sql = (
        f"UPDATE identity.subjects SET status='{status}' WHERE id='00000000-0000-4000-8000-000000000003';"
        if action.endswith("subject")
        else f"UPDATE identity.memberships SET status='{status}' WHERE subject_id='00000000-0000-4000-8000-000000000003' AND organization_id='00000000-0000-4000-8000-00000000000a';"
    )
    result = db_sql(sql, owner=True)
    if result.returncode:
        raise RuntimeError("Fixture membership update failed")
    print("{}")
    sys.exit()


def call(path, body=None, token=None):
    headers = {}
    data = None
    if body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(
        "https://localhost:8443" + path, data=data, headers=headers
    )
    with urllib.request.urlopen(req, context=ctx) as response:
        raw = response.read()
        return json.loads(raw) if raw else None


payload = urlencode(
    {
        "grant_type": "password",
        "client_id": "admin-cli",
        "username": "bootstrap-admin",
        "password": secrets["admin_password"],
    }
).encode()
req = urllib.request.Request(
    "https://localhost:8443/realms/master/protocol/openid-connect/token", data=payload
)
with urllib.request.urlopen(req, context=ctx) as response:
    token = json.load(response)["access_token"]
realm = call("/admin/realms/aip", token=token)
if action == "rotate":
    previous = call("/admin/realms/aip/keys", token=token)["active"]["RS256"]
    components = call(
        "/admin/realms/aip/components?type=org.keycloak.keys.KeyProvider", token=token
    )
    priority = (
        max(
            [int(c.get("config", {}).get("priority", ["0"])[0]) for c in components]
            + [0]
        )
        + 100
    )
    body = {
        "name": "i1-rotation-" + __import__("uuid").uuid4().hex[:8],
        "parentId": realm["id"],
        "providerId": "rsa-generated",
        "providerType": "org.keycloak.keys.KeyProvider",
        "config": {
            "priority": [str(priority)],
            "enabled": ["true"],
            "active": ["true"],
            "algorithm": ["RS256"],
            "keySize": ["2048"],
            "keyUse": ["SIG"],
        },
    }
    call("/admin/realms/aip/components", body, token)
    keys = call("/admin/realms/aip/keys", token=token)
    assert keys["active"]["RS256"] != previous, "Signing key did not rotate"
    print(json.dumps({"rotated": True}))
else:
    raise RuntimeError("Unknown fixture action")
