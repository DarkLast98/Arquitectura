#!/usr/bin/env python3
"""Fail the build when an I1 layer depends outward or leaks SQL into the core/HTTP."""

from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
LAYERS = {
    "Aip.Domain": set(),
    "Aip.Application": {"Aip.Domain"},
    "Aip.Infrastructure": {"Aip.Application"},
    "Aip.Api": {"Aip.Application", "Aip.Infrastructure"},
}
for name, allowed in LAYERS.items():
    project = ROOT / "src" / name / (name + ".csproj")
    tree = ET.parse(project)
    dependencies = {
        Path(item.attrib["Include"]).stem
        for item in tree.findall(".//ProjectReference")
    }
    if dependencies != allowed:
        raise SystemExit(f"Forbidden project dependencies in {name}: {dependencies}")
    if name in {"Aip.Domain", "Aip.Application"}:
        if tree.findall(".//PackageReference") or tree.findall(".//FrameworkReference"):
            raise SystemExit(f"Core must compile using BCL only: {name}")
    for source in project.parent.rglob("*.cs"):
        if "obj" in source.parts or "bin" in source.parts:
            continue
        text = source.read_text()
        if name != "Aip.Infrastructure" and re.search(
            r"\b(SELECT\s+.+?\s+FROM|INSERT\s+INTO|UPDATE\s+\w+[.\w]*\s+SET)\b",
            text,
            re.I,
        ):
            raise SystemExit(
                f"SQL outside PostgreSQL adapter: {source.relative_to(ROOT)}"
            )
        if name in {"Aip.Domain", "Aip.Application"} and re.search(
            r"\b(HttpContext|HttpRequest|JsonObject|Npgsql|Microsoft\.AspNetCore|Aip\.Infrastructure)\b",
            text,
        ):
            raise SystemExit(
                f"Transport/infrastructure type in core: {source.relative_to(ROOT)}"
            )
        if (
            name == "Aip.Api"
            and source.name != "Program.cs"
            and "Aip.Infrastructure" in text
        ):
            raise SystemExit(
                f"Only composition may reference infrastructure: {source.relative_to(ROOT)}"
            )
web = ROOT / "web/src/app"
for area in ["domain", "application"]:
    for source in (web / area).rglob("*.ts"):
        text = source.read_text()
        for specifier in re.findall(r"(?:from|import)\s*[\"']([^\"']+)[\"']", text):
            if not specifier.startswith("."):
                raise SystemExit(
                    f"External package in web core: {source.relative_to(ROOT)}"
                )
            target = (source.parent / specifier).resolve()
            if not any(
                target.is_relative_to(web / allowed)
                for allowed in (
                    ["domain"] if area == "domain" else ["domain", "application"]
                )
            ):
                raise SystemExit(
                    f"Outward dependency in web core: {source.relative_to(ROOT)}"
                )
print(
    "PASS: .NET dependency graph, core boundaries, SQL ownership and TypeScript imports"
)
