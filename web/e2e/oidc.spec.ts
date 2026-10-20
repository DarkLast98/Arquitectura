import { test, expect, Page } from "@playwright/test";
import { readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
const secrets = JSON.parse(readFileSync("../.local/i1/secrets.json", "utf8"));
const base =
  "/api/v1/organizations/00000000-0000-4000-8000-00000000000a/catalog";
async function login(
  page: Page,
  origin = "https://localhost:7240",
  user = "admin",
) {
  await page.goto(origin + "/auth/login");
  await page.locator("#username").fill(user);
  await page.locator("#password").fill(secrets.passwords[user]);
  await page.locator("#kc-login").click();
  await expect(page).toHaveURL(origin + "/");
}
test("OIDC-03: segundo Authority y ClientId con el mismo email no hereda membresías", async ({
  page,
}) => {
  await login(page, "https://localhost:7241");
  const result = await (
    await page.request.get("https://localhost:7241/api/session")
  ).json();
  expect(result.username).toContain("admin");
  expect(result.organizations).toEqual([]);
  expect(
    (
      await page.request.get("https://localhost:7241" + base + "/entities")
    ).status(),
  ).toBe(403);
});
test("OIDC-05: revocar membresía afecta inmediatamente una sesión existente", async ({
  page,
}) => {
  const state = JSON.parse(
    readFileSync("../.local/i1/sessions/reader.json", "utf8"),
  );
  await page.context().addCookies(state.cookies);
  expect((await page.request.get(base + "/entities")).status()).toBe(200);
  try {
    execFileSync("python3", ["../tests/oidc_admin.py", "revoke"]);
    expect((await page.request.get(base + "/entities")).status()).toBe(403);
    const session = await (await page.request.get("/api/session")).json();
    expect(session.organizations.some((o: any) => o.id.endsWith("00a"))).toBe(
      false,
    );
  } finally {
    execFileSync("python3", ["../tests/oidc_admin.py", "restore"]);
  }
});
test("OIDC-01,06: login, CSRF, cierre OIDC y API sin redirección HTML", async ({
  page,
}) => {
  await login(page);
  expect(
    (await page.request.post("/auth/logout", { maxRedirects: 0 })).status(),
  ).toBe(400);
  await page.goto("/");
  await page
    .getByRole("button", { name: "Cerrar sesión", exact: true })
    .click();
  const confirm = page.locator("#kc-logout");
  if (await confirm.isVisible()) await confirm.click();
  await expect(page).toHaveURL("https://localhost:7240/");
  const response = await page.request.get(base + "/entities");
  expect(response.status()).toBe(401);
  expect(response.headers()["content-type"]).toContain(
    "application/problem+json",
  );
  expect(response.headers()["location"]).toBeUndefined();
  expect((await page.request.get("/api/login")).status()).toBe(404);
  expect(
    (await page.context().cookies()).some(
      (c) => c.name === "__Host-aip.session",
    ),
  ).toBe(false);
});
for (const mode of [
  "state",
  "signature",
  "issuer",
  "audience",
  "expired",
  "future",
  "nonce",
  "missing-nonce",
]) {
  test(`OIDC-02: rechaza ${mode} sin sesión`, async ({ page }) => {
    await page.request.post("https://localhost:7443/test-mode", {
      data: { mode },
    });
    await page.goto("https://localhost:7242/auth/login");
    await expect(page.locator("body")).toContainText("AUTH_OIDC");
    expect(
      (await page.context().cookies()).some(
        (c) => c.name === "__Host-aip.session",
      ),
    ).toBe(false);
    expect(
      (
        await page.request.get("https://localhost:7242" + base + "/entities")
      ).status(),
    ).toBe(401);
  });
}
test("OIDC-03: mismo sub de otro issuer no se asocia al administrador existente", async ({
  page,
}) => {
  await page.request.post("https://localhost:7443/test-mode", {
    data: { mode: "valid" },
  });
  await page.goto("https://localhost:7242/auth/login");
  await expect(page).toHaveURL("https://localhost:7242/");
  const session = await (
    await page.request.get("https://localhost:7242/api/session")
  ).json();
  expect(session.username).toContain("independent issuer");
  expect(session.organizations).toEqual([]);
  expect(
    (
      await (
        await page.request.get("https://localhost:7443/test-status")
      ).json()
    ).pkce_verified,
  ).toBe(true);
});
test("OIDC-07: rotación real de clave Keycloak y recuperación JWKS", async ({
  page,
}) => {
  execFileSync("python3", ["../tests/oidc_admin.py", "rotate"]);
  await page.goto("/auth/login");
  await page.locator("#username").fill("admin");
  await page.locator("#password").fill(secrets.passwords.admin);
  await page.locator("#kc-login").click();
  // Official handler may request fresh metadata after the first unknown kid.
  if (new URL(page.url()).pathname === "/signin-oidc")
    await page.goto("/auth/login");
  await expect(page).toHaveURL("https://localhost:7240/");
  expect(
    (await (await page.request.get("/api/session")).json()).organizations
      .length,
  ).toBe(2);
});

test("OIDC-02,06: retorno externo y cabeceras de suplantación no conceden acceso", async ({
  page,
}) => {
  expect(
    (
      await page.request.get(
        "/auth/login?returnUrl=https%3A%2F%2Fevil.invalid",
        { maxRedirects: 0 },
      )
    ).status(),
  ).toBe(400);
  expect(
    (
      await page.request.get(base + "/entities", {
        headers: {
          "X-User-Id": "00000000-0000-4000-8000-000000000001",
          "X-Organization-Id": "00000000-0000-4000-8000-00000000000a",
          Authorization: "Bearer unused-test-only",
        },
      })
    ).status(),
  ).toBe(401);
});
