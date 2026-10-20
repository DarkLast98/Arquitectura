"""HTTPS fault-injection IdP, ONLY tests. Real success acceptance uses Keycloak.
No changes to API validation; this is an independently configured Authority.
"""

from pathlib import Path
from http.server import ThreadingHTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs, urlencode
import base64, hashlib, json, secrets, ssl, subprocess, time

ROOT = Path(__file__).resolve().parents[1]
DEV = ROOT / ".local/i1"
TLS = DEV / "tls"
ISSUER = "https://localhost:7443"
b64 = lambda b: base64.urlsafe_b64encode(b).decode().rstrip("=")
for name in ["oidc-key", "wrong-key"]:
    p = DEV / (name + ".pem")
    if not p.exists():
        subprocess.run(
            ["openssl", "genrsa", "-out", str(p), "2048"],
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
        p.chmod(0o600)
modulus = (
    subprocess.check_output(
        ["openssl", "rsa", "-in", str(DEV / "oidc-key.pem"), "-modulus", "-noout"],
        stderr=subprocess.DEVNULL,
    )
    .decode()
    .strip()
    .split("=")[1]
)
MODE = "valid"
CODES = {}
PKCE_OK = False


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass  # Never log codes/tokens/credentials.

    def send(self, data, status=200):
        body = json.dumps(data).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        q = parse_qs(urlparse(self.path).query)
        path = urlparse(self.path).path
        if path == "/.well-known/openid-configuration":
            return self.send(
                {
                    "issuer": ISSUER,
                    "authorization_endpoint": ISSUER + "/authorize",
                    "token_endpoint": ISSUER + "/token",
                    "jwks_uri": ISSUER + "/jwks",
                    "response_types_supported": ["code"],
                    "subject_types_supported": ["public"],
                    "id_token_signing_alg_values_supported": ["RS256"],
                    "token_endpoint_auth_methods_supported": ["client_secret_post"],
                    "code_challenge_methods_supported": ["S256"],
                }
            )
        if path == "/jwks":
            return self.send(
                {
                    "keys": [
                        {
                            "kty": "RSA",
                            "use": "sig",
                            "kid": "test-1",
                            "alg": "RS256",
                            "e": "AQAB",
                            "n": b64(bytes.fromhex(modulus)),
                        }
                    ]
                }
            )
        if path == "/authorize":
            code = secrets.token_urlsafe(24)
            CODES[code] = q
            self.send_response(302)
            self.send_header(
                "Location",
                q["redirect_uri"][0]
                + "?"
                + urlencode(
                    {
                        "state": "invalid-state" if MODE == "state" else q["state"][0],
                        "code": code,
                    }
                ),
            )
            self.end_headers()
            return
        if path == "/test-status":
            return self.send({"pkce_verified": PKCE_OK})
        self.send({}, 404)

    def do_POST(self):
        global MODE, PKCE_OK
        data = self.rfile.read(int(self.headers.get("Content-Length", "0")))
        if self.path == "/test-mode":
            MODE = json.loads(data)["mode"]
            return self.send({"ok": True})
        if self.path != "/token":
            return self.send({}, 404)
        q = parse_qs(data.decode())
        authorization = CODES.pop(q.get("code", [""])[0], None)
        if not authorization:
            return self.send({"error": "invalid_grant"}, 400)
        PKCE_OK = (
            authorization.get("code_challenge_method") == ["S256"]
            and b64(hashlib.sha256(q.get("code_verifier", [""])[0].encode()).digest())
            == authorization.get("code_challenge", [""])[0]
        )
        if (
            not PKCE_OK
            or q.get("client_id") != ["negative-test-client"]
            or q.get("client_secret") != ["local-test-only"]
        ):
            return self.send({"error": "invalid_grant"}, 400)
        now = int(time.time())
        claims = {
            "iss": ISSUER,
            "sub": "00000000-0000-4000-8000-000000000001",
            "aud": "negative-test-client",
            "exp": now + 300,
            "iat": now,
            "nonce": authorization["nonce"][0],
            "name": "admin from independent issuer",
            "email": "admin@example.invalid",
        }
        if MODE == "issuer":
            claims["iss"] = "https://wrong.invalid"
        if MODE == "audience":
            claims["aud"] = "wrong-client"
        if MODE == "expired":
            claims["exp"] = now - 300
        if MODE == "future":
            claims["nbf"] = now + 300
        if MODE == "nonce":
            claims["nonce"] = "wrong-nonce"
        if MODE == "missing-nonce":
            del claims["nonce"]
        header = {"alg": "RS256", "typ": "JWT", "kid": "test-1"}
        unsigned = (
            b64(json.dumps(header).encode()) + "." + b64(json.dumps(claims).encode())
        ).encode()
        key = "wrong-key.pem" if MODE == "signature" else "oidc-key.pem"
        sig = subprocess.check_output(
            ["openssl", "dgst", "-sha256", "-sign", str(DEV / key)], input=unsigned
        )
        self.send(
            {
                "access_token": "unused-test-only",
                "token_type": "Bearer",
                "expires_in": 300,
                "id_token": unsigned.decode() + "." + b64(sig),
            }
        )


server = ThreadingHTTPServer(("127.0.0.1", 7443), Handler)
context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
context.load_cert_chain(TLS / "server.crt", TLS / "server.key")
server.socket = context.wrap_socket(server.socket, server_side=True)
print("OIDC fault-injection test server on localhost:7443", flush=True)
server.serve_forever()
