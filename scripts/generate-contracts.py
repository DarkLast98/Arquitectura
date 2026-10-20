#!/usr/bin/env python3
"""Genera contratos de I1: revisar los cambios antes de confirmar en Git."""

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "contracts"
uuid = {"type": "string", "format": "uuid"}
text = lambda n: {"type": "string", "maxLength": n}
nullable = lambda s: {"anyOf": [s, {"type": "null"}]}
entity_types = ["core.business_capability", "core.application", "core.data_asset"]
relation_types = ["core.supports", "core.uses"]


def obj(properties, required=None):
    return {
        "type": "object",
        "additionalProperties": False,
        "properties": properties,
        "required": list(properties) if required is None else required,
    }


def ref(name):
    return {"$ref": "#/$defs/" + name}


name = text(200) | {"minLength": 1, "pattern": r"\S"}
classification = {"enum": ["internal", "confidential", "restricted"]}
base = {
    "id": uuid,
    "organization_id": uuid,
    "type_id": {"enum": entity_types},
    "schema_version": {"const": "0.1.0"},
    "code": text(64) | {"pattern": r"^[A-Z0-9][A-Z0-9._-]{0,63}$"},
    "name": name,
    "description": nullable(text(4000)),
    "classification": classification,
    "status": {"enum": ["active", "archived"]},
    "revision": {"type": "integer", "minimum": 1},
    "created_at": {"type": "string", "format": "date-time"},
    "updated_at": {"type": "string", "format": "date-time"},
    "created_by": uuid,
    "updated_by": uuid,
}
create = {
    k: base[k]
    for k in [
        "type_id",
        "schema_version",
        "code",
        "name",
        "description",
        "classification",
    ]
}
create["code"] = {
    "type": "string",
    "pattern": r"^\s*[A-Za-z0-9][A-Za-z0-9._-]{0,63}\s*$",
}
input_name = {"type": "string", "pattern": r"^\s*\S(?:[\s\S]{0,198}\S)?\s*$"}
create["name"] = input_name
relation = {
    k: v for k, v in base.items() if k not in ["code", "name", "classification"]
}
relation.update(
    type_id={"enum": relation_types}, source_entity_id=uuid, target_entity_id=uuid
)
relation_create = {
    k: relation[k]
    for k in [
        "type_id",
        "schema_version",
        "source_entity_id",
        "target_entity_id",
        "description",
    ]
}
defs = {
    "Entity": obj(base),
    "Relationship": obj(relation),
    "CreateEntity": obj(create, ["type_id", "schema_version", "code", "name"]),
    "PatchEntity": obj(
        {
            k: input_name if k == "name" else base[k]
            for k in ["name", "description", "classification"]
        },
        [],
    )
    | {"minProperties": 1},
    "CreateRelationship": obj(
        relation_create,
        ["type_id", "schema_version", "source_entity_id", "target_entity_id"],
    ),
}
defs["AuditEvent"] = obj(
    {
        "id": uuid,
        "organization_id": uuid,
        "actor_id": uuid,
        "operation": {"enum": ["created", "updated", "archived"]},
        "resource_kind": {"enum": ["entity", "relationship"]},
        "resource_id": uuid,
        "previous_revision": nullable({"type": "integer", "minimum": 1}),
        "new_revision": {"type": "integer", "minimum": 1},
        "occurred_at": {"type": "string", "format": "date-time"},
        "correlation_id": {"type": "string"},
        "changed_fields": {"type": "array", "items": {"type": "string"}},
    }
)
for kind in ["Entity", "Relationship", "AuditEvent"]:
    defs[kind + "Page"] = obj(
        {
            "items": {"type": "array", "items": ref(kind)},
            "total": {"type": "integer", "minimum": 0},
            "limit": {"type": "integer", "minimum": 1, "maximum": 100},
            "offset": {"type": "integer", "minimum": 0, "maximum": 10000},
        }
    )
defs["Types"] = {
    "type": "array",
    "minItems": 5,
    "maxItems": 5,
    "items": obj(
        {
            "type_id": {"enum": entity_types + relation_types},
            "category": {"enum": ["entity", "relationship"]},
            "schema_version": {"const": "0.1.0"},
            "source_type": nullable({"enum": entity_types}),
            "target_type": nullable({"enum": entity_types}),
        }
    ),
}
defs["Problem"] = obj(
    {
        "type": {"type": "string"},
        "title": {"type": "string"},
        "status": {"type": "integer", "minimum": 400, "maximum": 599},
        "code": {"type": "string"},
        "correlation_id": {"type": "string"},
    }
)
defs["Session"] = obj(
    {
        "csrf_token": {"type": "string"},
        "username": nullable({"type": "string"}),
        "organizations": {
            "type": "array",
            "items": obj(
                {
                    "id": uuid,
                    "name": {"type": "string"},
                    "role": {"enum": ["organization_admin", "architect", "reader"]},
                }
            ),
        },
    }
)
SCHEMAS = ROOT / "schemas/catalog/0.1.0"
OPENAPI = ROOT / "openapi"
SCHEMAS.mkdir(parents=True, exist_ok=True)
OPENAPI.mkdir(parents=True, exist_ok=True)
schema = {
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "$id": "https://aip.invalid/contracts/catalog/0.1.0",
    "$defs": defs,
}
(SCHEMAS / "catalog.schema.json").write_text(json.dumps(schema, indent=2) + "\n")


def external(name):
    return {"$ref": "../schemas/catalog/0.1.0/catalog.schema.json#/$defs/" + name}


problem = {
    "description": "Error de contrato, autorización o dominio",
    "content": {"application/problem+json": {"schema": external("Problem")}},
}
prefix = "/api/v1/organizations/{organizationId}/catalog"
paths = {}


def operation(
    method, path, response, *, body=None, created=False, etag=False, query=None
):
    params = []
    if "{organizationId}" in path:
        params.append(
            {"name": "organizationId", "in": "path", "required": True, "schema": uuid}
        )
    if "{id}" in path:
        params.append({"name": "id", "in": "path", "required": True, "schema": uuid})
    if method in ["post", "patch"]:
        params.append(
            {
                "name": "X-CSRF-TOKEN",
                "in": "header",
                "required": True,
                "schema": {"type": "string"},
            }
        )
    if etag and method == "get":
        pass
    if etag and method != "get":
        params.append(
            {
                "name": "If-Match",
                "in": "header",
                "required": True,
                "schema": {"type": "string", "pattern": '^"[1-9][0-9]*"$'},
            }
        )
    for key, s in (query or {}).items():
        params.append({"name": key, "in": "query", "schema": s})
    status = "201" if created else "200" if response else "204"
    success = {"description": "Operación confirmada"}
    if response:
        success["content"] = {"application/json": {"schema": external(response)}}
    if etag or created:
        success["headers"] = {"ETag": {"required": True, "schema": {"type": "string"}}}
    if created:
        success["headers"]["Location"] = {
            "required": True,
            "schema": {"type": "string"},
        }
    op = {
        "operationId": method
        + "_"
        + path.replace(prefix, "catalog")
        .replace("/", "_")
        .replace("{", "")
        .replace("}", ""),
        "parameters": params,
        "responses": {
            status: success,
            **{
                str(code): problem
                for code in [400, 401, 403, 404, 409, 412, 413, 422, 428, 429, 500]
            },
        },
        "security": [{"sessionCookie": []}],
    }
    if body:
        op["requestBody"] = {
            "required": True,
            "content": {"application/json": {"schema": external(body)}},
        }
    paths.setdefault(path, {})[method] = op


paging = {
    "limit": {"type": "integer", "minimum": 1, "maximum": 100, "default": 20},
    "offset": {"type": "integer", "minimum": 0, "maximum": 10000, "default": 0},
}
status = {"enum": ["active", "archived", "all"], "default": "active"}
operation("get", prefix + "/types", "Types")
for plural, kind, types in [
    ("entities", "Entity", entity_types),
    ("relationships", "Relationship", relation_types),
]:
    query = paging | {"type_id": {"enum": types}, "status": status}
    query |= (
        {"code": text(64), "q": text(128)}
        if kind == "Entity"
        else {k: uuid for k in ["source_entity_id", "target_entity_id", "entity_id"]}
    )
    operation("get", prefix + "/" + plural, kind + "Page", query=query)
    operation("post", prefix + "/" + plural, kind, body="Create" + kind, created=True)
    operation("get", prefix + "/" + plural + "/{id}", kind, etag=True)
    operation("post", prefix + "/" + plural + "/{id}/archive", kind, etag=True)
operation("patch", prefix + "/entities/{id}", "Entity", body="PatchEntity", etag=True)
operation(
    "get",
    prefix + "/audit-events",
    "AuditEventPage",
    query=paging
    | {"resource_kind": {"enum": ["entity", "relationship"]}, "resource_id": uuid},
)
operation("get", "/api/session", "Session")
paths["/api/session"]["get"]["security"] = []
operation("post", "/auth/logout", None)
paths["/auth/logout"]["post"]["responses"]["302"] = {
    "description": "Redirección de cierre de sesión OIDC"
}
del paths["/auth/logout"]["post"]["responses"]["204"]
paths["/auth/login"] = {
    "get": {
        "operationId": "oidc_login",
        "security": [],
        "parameters": [
            {"name": "returnUrl", "in": "query", "schema": {"type": "string"}}
        ],
        "responses": {
            "302": {
                "description": "Authorization Code con PKCE; PAR cuando lo anuncia el proveedor"
            },
            "400": problem,
            "500": problem,
        },
    }
}
api = {
    "openapi": "3.1.0",
    "info": {
        "title": "AIP Catalog API",
        "version": "0.1.0",
        "description": "I1 local. Cookies protegidas, CSRF y contexto organizacional autorizado. No habilita agentes ni regulación.",
    },
    "paths": paths,
    "components": {
        "securitySchemes": {
            "sessionCookie": {
                "type": "apiKey",
                "in": "cookie",
                "name": "__Host-aip.session",
            }
        }
    },
}
(OPENAPI / "catalog.v1.json").write_text(json.dumps(api, indent=2) + "\n")
