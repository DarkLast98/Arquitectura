#!/usr/bin/env python3
"""Migrate I1 unchanged with checksum verification; seed synthetic OIDC identities."""
from pathlib import Path
import json
import os
import subprocess

from postgres_migrations import build_migration_sql

ROOT = Path(__file__).resolve().parents[1]
creds = json.loads(Path("/state/secrets.json").read_text())
env = dict(os.environ, PGPASSWORD=creds["owner_password"])


def psql(sql: str, database: str = "postgres") -> str:
    proc = subprocess.run(
        ["psql", "-X", "-v", "ON_ERROR_STOP=1", "-At",
         "-h", "postgres", "-p", "5432", "-U", "aip_owner", "-d", database],
        input=sql, text=True, capture_output=True, env=env, check=True,
    )
    return proc.stdout


# Passwords are generated as hex, never supplied by a client.
psql(
    "DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'aip_i1_app') "
    "THEN CREATE ROLE aip_i1_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS; "
    "END IF; END $$; ALTER ROLE aip_i1_app PASSWORD '" + creds["app_password"] + "';"
)
if psql("SELECT 1 FROM pg_database WHERE datname = 'aip_i1';").strip() != "1":
    psql("CREATE DATABASE aip_i1;")
psql(build_migration_sql(ROOT / "src/Aip.Infrastructure/Postgres/Migrations"), "aip_i1")

issuer = "https://auth.localhost:8443/realms/aip"
seed = [
    "BEGIN;",
    "INSERT INTO identity.organizations(id,name) VALUES "
    "('00000000-0000-4000-8000-00000000000a','Organización A · Demostración'),"
    "('00000000-0000-4000-8000-00000000000b','Organización B · Demostración') "
    "ON CONFLICT DO NOTHING;",
]
for i, role in enumerate(("organization_admin", "architect", "reader", "reader"), 1):
    uid = f"00000000-0000-4000-8000-{i:012d}"
    seed.append(
        f"INSERT INTO identity.subjects(id,issuer,external_subject) "
        f"VALUES('{uid}','{issuer}','{uid}') ON CONFLICT DO NOTHING;"
    )
    for org in (("b",) if i == 4 else ("a", "b")):
        seed.append(
            "INSERT INTO identity.memberships(organization_id,subject_id,role) "
            f"VALUES('00000000-0000-4000-8000-00000000000{org}','{uid}','{role}') "
            "ON CONFLICT DO NOTHING;"
        )
seed.append("COMMIT;")
psql("\n".join(seed), "aip_i1")
print("AIP Docker: migraciones verificadas y organizaciones sintéticas inicializadas.")
