#!/usr/bin/env python3
"""Fast developer loop: compile boundaries and use cases without PostgreSQL or OIDC."""

from pathlib import Path
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
DOTNET = os.environ.get("AIP_DOTNET", "/workspace/.aip-tools/dotnet/dotnet")
environment = os.environ | {
    "DOTNET_CLI_HOME": os.environ.get(
        "DOTNET_CLI_HOME", "/workspace/.aip-tools/dotnet-home"
    ),
    "NUGET_PACKAGES": os.environ.get("NUGET_PACKAGES", "/workspace/.aip-tools/nuget"),
}
commands = [
    [sys.executable, "scripts/check-architecture.py"],
    [DOTNET, "restore", "Aip.slnx", "--locked-mode"],
    [DOTNET, "build", "Aip.slnx", "--no-restore"],
    [DOTNET, "test", "Aip.slnx", "--no-build", "--no-restore"],
    ["npm", "--prefix", "web", "run", "test:core"],
    ["npm", "--prefix", "web", "run", "test:unit"],
    ["npm", "--prefix", "web", "run", "build"],
]
for command in commands:
    subprocess.run(command, cwd=ROOT, env=environment, check=True)
