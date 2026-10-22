#!/usr/bin/env python3
from pathlib import Path
import json, os

root = Path(__file__).resolve().parents[1]
dev = root / ".local/i1"
config = json.loads((dev / "secrets.json").read_text())
os.environ["KC_BOOTSTRAP_ADMIN_USERNAME"] = "bootstrap-admin"
os.environ["KC_BOOTSTRAP_ADMIN_PASSWORD"] = config["admin_password"]
os.environ["JAVA_OPTS_APPEND"] = "-Xms128m -Xmx512m -XX:ActiveProcessorCount=3"
kc = (
    Path(os.environ.get("KEYCLOAK_HOME", "/workspace/.aip-tools/keycloak-26.8.1"))
    / "bin/kc.sh"
)
os.execv(
    str(kc),
    [str(kc), "--config-file=" + str(dev / "keycloak.conf"), "start", "--import-realm"],
)
