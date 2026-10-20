#!/usr/bin/env python3
"""Run the single BFF/API with private config and normal TLS verification."""

from pathlib import Path
import os, sys

root = Path(__file__).resolve().parents[1]
dev = root / ".local/i1"
alt = "--alternate" in sys.argv
test = "--test-provider" in sys.argv
os.environ["AIP_CONFIG"] = str(
    dev
    / (
        "appsettings-test.json"
        if test
        else "appsettings-alt.json" if alt else "appsettings.json"
    )
)
os.environ["SSL_CERT_FILE"] = str(dev / "tls/bundle.pem")
os.environ["DOTNET_CLI_HOME"] = os.environ.get(
    "DOTNET_CLI_HOME", "/workspace/.aip-tools/dotnet-home"
)
os.environ["NUGET_PACKAGES"] = os.environ.get(
    "NUGET_PACKAGES", "/workspace/.aip-tools/nuget"
)
dotnet = os.environ.get("AIP_DOTNET", "/workspace/.aip-tools/dotnet/dotnet")
os.chdir(root)
os.execv(
    dotnet,
    [
        dotnet,
        "run",
        "--no-build",
        "--project",
        "src/Aip.Api",
        "--urls",
        f"https://localhost:{7242 if test else 7241 if alt else 7240}",
    ],
)
