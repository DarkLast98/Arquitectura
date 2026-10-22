#!/usr/bin/env python3
"""Import a development CA into this user's Chromium NSS store, not into source."""

from pathlib import Path
import hashlib, subprocess

root = Path(__file__).resolve().parents[1]
cert = root / ".local/i1/tls/ca.crt"
db = Path.home() / ".pki/nssdb"
db.mkdir(mode=0o700, parents=True, exist_ok=True)
if not (db / "cert9.db").exists():
    subprocess.run(
        ["certutil", "-N", "--empty-password", "-d", "sql:" + str(db)], check=True
    )
name = "AIP I1 local " + hashlib.sha256(cert.read_bytes()).hexdigest()[:12]
exists = (
    subprocess.run(
        ["certutil", "-L", "-n", name, "-d", "sql:" + str(db)],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    ).returncode
    == 0
)
if not exists:
    subprocess.run(
        [
            "certutil",
            "-A",
            "-n",
            name,
            "-t",
            "CT,,",
            "-i",
            str(cert),
            "-d",
            "sql:" + str(db),
        ],
        check=True,
    )
print("CA de desarrollo confiada para Chromium mediante NSS; no se desactiva TLS.")
