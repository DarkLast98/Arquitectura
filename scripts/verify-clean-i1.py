#!/usr/bin/env python3
"""Verify committed I1 in a new export with empty caches, PostgreSQL and IdP data.
Requires the documented SDK/JDK/PostgreSQL/Keycloak/Chromium prerequisites.
Does not change the checkout, merge, publish, or use existing .local data.
"""

from pathlib import Path
import hashlib, json, os, shutil, subprocess, sys, tarfile, tempfile

ROOT = Path(__file__).resolve().parents[1]


def run(arguments, *, cwd=ROOT, env=None):
    subprocess.run(arguments, cwd=cwd, env=env, check=True)


def capture(arguments, *, cwd=ROOT, env=None):
    return subprocess.check_output(arguments, cwd=cwd, env=env, text=True)


if capture(["git", "status", "--porcelain"]).strip():
    raise SystemExit(
        "Commit or preserve pending changes before reviewing a committed revision."
    )
revision = capture(["git", "rev-parse", "HEAD"]).strip()
base = Path(tempfile.mkdtemp(prefix="aip-i1-review-"))
base.chmod(0o700)
repo = base / "repo"
repo.mkdir()
archive = base / "source.tar"
with archive.open("wb") as target:
    subprocess.run(["git", "archive", revision], cwd=ROOT, stdout=target, check=True)
with tarfile.open(archive) as source:
    source.extractall(repo, filter="data")
keycloak = base / "keycloak"
shutil.copytree(
    Path(os.environ.get("KEYCLOAK_HOME", "/workspace/.aip-tools/keycloak-26.8.1")),
    keycloak,
    ignore=shutil.ignore_patterns("data"),
)
fresh = {
    name: not (repo / name).exists()
    for name in [
        ".local",
        "web/node_modules",
        "web/dist",
        "src/Aip.Api/bin",
        "src/Aip.Api/obj",
    ]
}
if not all(fresh.values()):
    raise RuntimeError("Source export unexpectedly contains generated state")
run([sys.executable, "-m", "venv", str(base / "venv")])
python = str(base / "venv/bin/python")
environment = os.environ | {
    "PATH": str(base / "venv/bin") + os.pathsep + os.environ["PATH"],
    "KEYCLOAK_HOME": str(keycloak),
    "DOTNET_CLI_HOME": str(base / "dotnet-home"),
    "NUGET_PACKAGES": str(base / "nuget"),
    "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
    "LD_LIBRARY_PATH": os.environ.get(
        "AIP_PG_LIB", "/workspace/.aip-tools/pg/usr/lib/x86_64-linux-gnu"
    )
    + ":"
    + os.environ.get("LD_LIBRARY_PATH", ""),
}
manifest = {
    "source_commit": revision,
    "source_archive_sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
    "fresh": fresh,
    "root": str(base),
    "result": "RUNNING",
}
report = base / "clean-review.json"


def save():
    report.write_text(json.dumps(manifest, indent=2) + "\n")
    report.chmod(0o600)


save()
print(
    json.dumps({"clean_review_root": str(base), "source_commit": revision}), flush=True
)
pg = Path(
    os.environ.get("AIP_PG_BIN", "/workspace/.aip-tools/pg/usr/lib/postgresql/17/bin")
)
try:
    run(
        ["npm", "ci", "--ignore-scripts", "--cache", str(base / "npm-cache")],
        cwd=repo / "web",
        env=environment,
    )
    run(
        [
            python,
            "-m",
            "pip",
            "install",
            "--disable-pip-version-check",
            "--no-cache-dir",
            "-r",
            "tests/requirements.txt",
        ],
        cwd=repo,
        env=environment,
    )
    run([python, "-m", "pip", "check"], cwd=repo, env=environment)
    run([python, "scripts/prepare-i1.py"], cwd=repo, env=environment)
    psql = [
        str(pg / "psql"),
        "-X",
        "-At",
        "-v",
        "ON_ERROR_STOP=1",
        "-h",
        str(repo / ".local/pgsocket"),
        "-p",
        "55432",
        "-U",
        "aip_owner",
        "-d",
        "aip_i1",
    ]
    initial = capture(
        psql
        + [
            "-c",
            "SELECT json_build_object('entities',(SELECT count(*) FROM catalog.entities),'relationships',(SELECT count(*) FROM catalog.relationships),'audit',(SELECT count(*) FROM audit.catalog_events),'migrations',(SELECT count(*) FROM public.schema_migrations))::text;",
        ],
        cwd=repo,
        env=environment,
    )
    manifest["initial_database"] = json.loads(initial)
    if any(
        manifest["initial_database"][name] != 0
        for name in ["entities", "relationships", "audit"]
    ):
        raise RuntimeError("Review database is not empty")
    save()
    run([python, "scripts/trust-dev-ca.py"], cwd=repo, env=environment)
    run([python, "scripts/verify-i1.py"], cwd=repo, env=environment)
    verification = json.loads((repo / ".local/i1/verification.json").read_text())
    if verification["result"] != "PASS":
        raise RuntimeError("Verification report did not pass")
    manifest["verification"] = verification
    npm = json.loads(
        capture(
            ["npm", "audit", "--json", "--cache", str(base / "npm-cache")],
            cwd=repo / "web",
            env=environment,
        )
    )
    (base / "npm-audit.json").write_text(json.dumps(npm, indent=2) + "\n")
    manifest["npm_vulnerabilities"] = npm["metadata"]["vulnerabilities"]
    if manifest["npm_vulnerabilities"]["total"] != 0:
        raise RuntimeError("Unresolved npm advisories")
    dotnet = os.environ.get("AIP_DOTNET", "/workspace/.aip-tools/dotnet/dotnet")
    for name, project in [
        ("api", "src/Aip.Api"),
        ("domain_tests", "tests/Aip.Catalog.Domain.Tests"),
    ]:
        audit = json.loads(
            capture(
                [
                    dotnet,
                    "list",
                    project,
                    "package",
                    "--vulnerable",
                    "--include-transitive",
                    "--format",
                    "json",
                ],
                cwd=repo,
                env=environment,
            )
        )
        (base / ("nuget-" + name + ".json")).write_text(
            json.dumps(audit, indent=2) + "\n"
        )
        vulnerabilities = [
            v
            for p in audit["projects"]
            for f in p.get("frameworks", [])
            for group in ["topLevelPackages", "transitivePackages"]
            for dependency in f.get(group, [])
            for v in dependency.get("vulnerabilities", [])
        ]
        manifest["nuget_" + name + "_advisories"] = len(vulnerabilities)
        if vulnerabilities:
            raise RuntimeError("Unresolved NuGet advisories")
    manifest["result"] = "PASS"
except BaseException as failure:
    manifest.update(result="FAIL", failure_kind=type(failure).__name__)
    raise
finally:
    save()
    data = repo / ".local/pgdata"
    if data.exists():
        subprocess.run(
            [str(pg / "pg_ctl"), "-D", str(data), "-m", "fast", "-w", "stop"],
            env=environment,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=False,
        )
    print(
        json.dumps({"clean_review_report": str(report), "result": manifest["result"]}),
        flush=True,
    )
