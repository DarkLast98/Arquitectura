import { test, expect, Page } from "@playwright/test";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
const A = "00000000-0000-4000-8000-00000000000a";
const B = "00000000-0000-4000-8000-00000000000b";
const base = "/api/v1/organizations/" + A + "/catalog";
async function login(page: Page, user = "architect") {
  const state = JSON.parse(
    readFileSync(resolve("../.local/i1/sessions", `${user}.json`), "utf8"),
  );
  await page.context().addCookies(state.cookies);
  await page.goto("/");
  await expect(
    page.getByRole("heading", { name: "Catálogo arquitectónico", exact: true }),
  ).toBeVisible();
}
async function entity(page: Page, type: string, code: string, name: string) {
  await page
    .getByRole("button", { name: "Nueva entidad", exact: true })
    .click();
  await page.getByLabel("Tipo de entidad", { exact: true }).selectOption(type);
  await page.getByLabel("Código", { exact: true }).fill(code);
  await page.getByLabel("Nombre", { exact: true }).fill(name);
  await page
    .getByLabel("Descripción", { exact: true })
    .fill('<img src=x onerror="window.aipInjected=true">');
  await page
    .getByRole("button", { name: "Crear entidad", exact: true })
    .click();
  await expect(page.getByRole("status")).toHaveText("Entidad creada.");
  const r = await page.request.get(base + "/entities?code=" + code);
  return (await r.json()).items[0];
}
test("AC-01,03,04,07,12: recorrido completo, archivo, texto seguro y separación", async ({
  page,
}) => {
  page.on("dialog", (d) => d.accept());
  await login(page);
  const key = "UI" + Date.now();
  const cap = await entity(
    page,
    "core.business_capability",
    key + "CAP",
    key + " Capacidad",
  );
  const app = await entity(
    page,
    "core.application",
    key + "APP",
    key + " Aplicación",
  );
  const data = await entity(
    page,
    "core.data_asset",
    key + "DATA",
    key + " Datos",
  );
  await expect(page.locator("img")).toHaveCount(0);
  expect(await page.evaluate(() => Object.hasOwn(window, "aipInjected"))).toBe(
    false,
  );
  await page.getByRole("button", { name: "Relaciones", exact: true }).click();
  await page.getByLabel("Buscar aplicación", { exact: true }).fill(app.name);
  await page.getByLabel("Buscar destino", { exact: true }).fill(cap.name);
  await page
    .getByRole("button", { name: "Buscar extremos", exact: true })
    .click();
  await page
    .getByLabel("Aplicación de origen", { exact: true })
    .selectOption(app.id);
  await page
    .getByLabel("Entidad de destino", { exact: true })
    .selectOption(cap.id);
  await page
    .getByRole("button", { name: "Crear relación", exact: true })
    .click();
  await expect(page.getByRole("status")).toHaveText("Relación creada.");
  await page
    .getByLabel("Tipo de relación", { exact: true })
    .selectOption("core.uses");
  await page.getByLabel("Buscar destino", { exact: true }).fill(data.name);
  await page
    .getByRole("button", { name: "Buscar extremos", exact: true })
    .click();
  await expect(
    page
      .getByLabel("Entidad de destino", { exact: true })
      .locator('option[value="' + data.id + '"]'),
  ).toBeAttached();
  await page
    .getByLabel("Entidad de destino", { exact: true })
    .selectOption(data.id);
  await page
    .getByRole("button", { name: "Crear relación", exact: true })
    .click();
  await expect(page.getByRole("status")).toHaveText("Relación creada.");
  await page.getByLabel("Organización", { exact: true }).selectOption(B);
  await page.getByLabel("Buscar por nombre", { exact: true }).fill(key);
  await page.getByRole("button", { name: "Buscar", exact: true }).click();
  await expect(page.locator(".entity-item")).toHaveCount(0);
  const foreign = await page.request.get(
    "/api/v1/organizations/" + B + "/catalog/entities/" + app.id,
  );
  expect(foreign.status()).toBe(404);
  await page.getByLabel("Organización", { exact: true }).selectOption(A);
  await page
    .getByLabel("Buscar por nombre", { exact: true })
    .fill(key + " Aplicación");
  await page.getByRole("button", { name: "Buscar", exact: true }).click();
  await page
    .locator(".entity-item")
    .filter({ hasText: key + " Aplicación" })
    .click();
  await page
    .getByRole("button", { name: "Archivar entidad", exact: true })
    .click();
  await expect(page.getByRole("alert")).toContainText("Archiva primero");
  await expect(
    page.getByRole("button", { name: "Archivar relación", exact: true }),
  ).toHaveCount(2);
  await page
    .getByRole("button", { name: "Archivar relación", exact: true })
    .first()
    .click();
  await expect(
    page.getByRole("button", { name: "Archivar relación", exact: true }),
  ).toHaveCount(1);
  await page
    .getByRole("button", { name: "Archivar relación", exact: true })
    .click();
  await expect(
    page.getByRole("button", { name: "Archivar relación", exact: true }),
  ).toHaveCount(0);
  await page
    .getByRole("button", { name: "Archivar entidad", exact: true })
    .click();
  await expect(page.getByRole("status")).toHaveText("Entidad archivada.");
});
test("AC-05: lector consulta sin escribir y sin auditoría", async ({
  page,
}) => {
  await login(page, "reader");
  await expect(
    page.getByRole("button", { name: "Nueva entidad", exact: true }),
  ).toHaveCount(0);
  await expect(
    page.getByRole("button", { name: "Auditoría", exact: true }),
  ).toHaveCount(0);
  const session = await (await page.request.get("/api/session")).json();
  const r = await page.request.post(base + "/entities", {
    headers: { "X-CSRF-TOKEN": session.csrf_token },
    data: {},
  });
  expect(r.status()).toBe(403);
});
test("AC-06,12: conflicto preserva formulario y requiere reconciliación", async ({
  page,
}) => {
  await login(page);
  const key = "CONFLICT" + Date.now();
  const e = await entity(page, "core.application", key, key);
  await page.getByLabel("Nombre", { exact: true }).fill("Mi edición pendiente");
  const session = await (await page.request.get("/api/session")).json();
  const r = await page.request.patch(base + "/entities/" + e.id, {
    headers: { "X-CSRF-TOKEN": session.csrf_token, "If-Match": '"1"' },
    data: { name: "Cambio concurrente" },
  });
  expect(r.status()).toBe(200);
  await page
    .getByRole("button", { name: "Guardar cambios", exact: true })
    .click();
  await expect(page.getByRole("alert").first()).toContainText("recarga");
  await expect(page.getByLabel("Nombre", { exact: true })).toHaveValue(
    "Mi edición pendiente",
  );
  await page
    .getByRole("button", {
      name: "Descartar formulario y cargar versión actual",
      exact: true,
    })
    .click();
  await expect(page.getByLabel("Nombre", { exact: true })).toHaveValue(
    "Cambio concurrente",
  );
});
