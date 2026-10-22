"""SQL migration batches shared by local preparation and checksum fault tests."""

import hashlib
from pathlib import Path


def build_migration_sql(directory: Path) -> str:
    statements = [
        "BEGIN; SELECT pg_advisory_xact_lock(134345134); CREATE TABLE IF NOT EXISTS public.schema_migrations(version text PRIMARY KEY,checksum text NOT NULL,applied_at timestamptz NOT NULL DEFAULT now());"
    ]
    for migration in sorted(directory.glob("*.sql")):
        contents = migration.read_bytes()
        digest = hashlib.sha256(contents).hexdigest()
        version = migration.stem
        statements.append(
            f"DO $migration$ BEGIN IF EXISTS(SELECT FROM public.schema_migrations WHERE version='{version}' AND checksum<>'{digest}') THEN RAISE EXCEPTION 'Migration checksum mismatch: {version}'; END IF; IF NOT EXISTS(SELECT FROM public.schema_migrations WHERE version='{version}') THEN EXECUTE $body${contents.decode()}$body$; INSERT INTO public.schema_migrations(version,checksum) VALUES('{version}','{digest}'); END IF; END $migration$;"
        )
    return "\n".join(statements + ["COMMIT;"])
