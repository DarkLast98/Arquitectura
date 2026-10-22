const { test } = require("node:test");
const assert = require("node:assert/strict");
const {
  CatalogWorkspace,
} = require("../../.local/web-unit/src/app/application/catalog-workspace.js");
const {
  HttpCatalogGateway,
} = require("../../.local/web-unit/src/app/adapters/http-catalog-gateway.js");
const {
  CatalogError,
} = require("../../.local/web-unit/src/app/application/catalog-workspace.js");

test("Editar conserva identidad/revisión y envía solo campos mutables", async () => {
  let call;
  const workspace = new CatalogWorkspace({
    editEntity: async (...args) => {
      call = args;
      return { id: "entity" };
    },
  });
  await workspace.saveEntity(
    "org-A",
    {
      type_id: "core.application",
      code: "IMMUTABLE",
      name: "Changed",
      description: "",
      classification: "internal",
    },
    { id: "entity", revision: 7 },
  );
  assert.deepEqual(call, [
    "org-A",
    "entity",
    7,
    { name: "Changed", description: null, classification: "internal" },
  ]);
});
test("Crear fija el esquema I1 y conserva la organización explícita", async () => {
  let call;
  const workspace = new CatalogWorkspace({
    createEntity: async (...args) => {
      call = args;
      return {};
    },
  });
  await workspace.saveEntity(
    "org-B",
    {
      type_id: "core.application",
      code: "APP",
      name: "App",
      description: "",
      classification: "internal",
    },
    null,
  );
  assert.equal(call[0], "org-B");
  assert.equal(call[1].schema_version, "0.1.0");
  assert.equal(call[1].description, null);
});
test("Selección de extremos respeta dirección y tipos de SPEC-CAT-001", async () => {
  const calls = [];
  const workspace = new CatalogWorkspace({
    entities: async (...args) => {
      calls.push(args);
      return { items: [] };
    },
  });
  await workspace.options("org-A", "core.uses", "app", "dato");
  assert.deepEqual(
    calls.map((c) => [c[0], c[1].type_id, c[1].q]),
    [
      ["org-A", "core.application", "app"],
      ["org-A", "core.data_asset", "dato"],
    ],
  );
});
test("Adaptador mantiene CSRF privado y envía If-Match y cookie del mismo origen", async () => {
  const original = global.fetch;
  const calls = [];
  global.fetch = async (url, options) => {
    calls.push({ url, options });
    return {
      ok: true,
      status: 200,
      json: async () =>
        url === "/api/session"
          ? {
              csrf_token: "synthetic-csrf",
              username: "Test",
              organizations: [],
            }
          : { id: "entity" },
    };
  };
  try {
    const gateway = new HttpCatalogGateway();
    const session = await gateway.session();
    assert.equal(session.csrf_token, undefined);
    await gateway.editEntity("org-A", "entity", 7, { name: "Changed" });
    assert.equal(calls[1].options.headers["X-CSRF-TOKEN"], "synthetic-csrf");
    assert.equal(calls[1].options.headers["If-Match"], '"7"');
    assert.equal(calls[1].options.credentials, "same-origin");
  } finally {
    global.fetch = original;
  }
});
test("Adaptador conserva conflicto 412 para revisión humana de cambios", async () => {
  const original = global.fetch;
  global.fetch = async () => ({
    ok: false,
    status: 412,
    json: async () => ({ code: "CAT_REVISION_CONFLICT", title: "Cambió" }),
  });
  try {
    await assert.rejects(
      new HttpCatalogGateway().editEntity("org-A", "id", 1, {}),
      (e) =>
        e instanceof CatalogError &&
        e.status === 412 &&
        e.code === "CAT_REVISION_CONFLICT",
    );
  } finally {
    global.fetch = original;
  }
});
