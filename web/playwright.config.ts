import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "e2e",
  workers: 1,
  timeout: 60000,
  use: {
    baseURL: "https://localhost:7240",
    headless: true,
    launchOptions: {
      executablePath: process.env["AIP_CHROMIUM"] || "/usr/bin/chromium",
    },
    viewport: { width: 1440, height: 1000 },
    screenshot: "only-on-failure",
  },
  projects: [
    { name: "setup", testMatch: /oidc.setup.ts/ },
    { name: "catalog", testMatch: /.*\.spec\.ts/, dependencies: ["setup"] },
  ],
  reporter: "list",
});
