import { defineConfig, devices } from "@playwright/test";
import path from "path";

const port = 3000;
const baseURL = `http://127.0.0.1:${port}`;

export default defineConfig({
  testDir: "./tests/e2e",
  fullyParallel: true,
  expect: {
    timeout: 15_000,
  },
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL,
    trace: "on-first-retry",
    ignoreHTTPSErrors: true,
  },
  webServer: [
    {
      command: "dotnet run",
      url: "https://localhost:5001",
      reuseExistingServer: !process.env.CI,
      cwd: path.resolve(__dirname, "../VidFlow/VidFlow.Api"),
      timeout: 120_000,
      ignoreHTTPSErrors: true,
      env: {
        ASPNETCORE_ENVIRONMENT: "Development",
      },
    },
    {
      command: `npm run dev -- --host 127.0.0.1 --port ${port} --strictPort`,
      url: baseURL,
      reuseExistingServer: !process.env.CI,
      cwd: __dirname,
      env: {
        PLAYWRIGHT_TEST: "1",
        VITE_API_PROXY_TARGET: "https://localhost:5001",
      },
    },
  ],
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
