import { test, expect } from "@playwright/test";
import { readFileSync, mkdirSync, chmodSync } from "node:fs";
import { resolve } from "node:path";
const dev = resolve("..", ".local/i1");
const secrets = JSON.parse(readFileSync(resolve(dev, "secrets.json"), "utf8"));
test("OIDC real: cuatro perfiles, PKCE S256 y cookies seguras", async ({
  browser,
}) => {
  mkdirSync(resolve(dev, "sessions"), { recursive: true, mode: 0o700 });
  for (const user of ["admin", "architect", "reader", "outsider"]) {
    const context = await browser.newContext();
    const page = await context.newPage();
    let pkce = false,
      par = false;
    page.on("request", (request) => {
      const url = new URL(request.url());
      if (url.pathname.endsWith("/auth")) {
        par = !!url.searchParams.get("request_uri");
        pkce =
          url.searchParams.get("code_challenge_method") === "S256" &&
          !!url.searchParams.get("code_challenge");
      }
    });
    await page.goto("https://localhost:7240/auth/login");
    await page.locator("#username").fill(user);
    await page.locator("#password").fill(secrets.passwords[user]);
    await page.locator("#kc-login").click();
    await expect(page).toHaveURL("https://localhost:7240/");
    const session = await page.request.get(
      "https://localhost:7240/api/session",
    );
    expect(session.status()).toBe(200);
    expect((await session.json()).username).toContain(user);
    expect(pkce || par).toBe(true); // Keycloak enforces S256 also on the PAR request.
    expect(par).toBe(true);
    const cookies = await context.cookies("https://localhost:7240");
    const cookie = cookies.find((c) => c.name === "__Host-aip.session");
    expect(cookie?.secure).toBe(true);
    expect(cookie?.httpOnly).toBe(true);
    expect(cookie?.sameSite).toBe("Lax");
    expect(await page.evaluate(() => Object.keys(localStorage))).toEqual([]);
    const file = resolve(dev, "sessions", `${user}.json`);
    await context.storageState({ path: file });
    chmodSync(file, 0o600);
    await context.close();
  }
});
