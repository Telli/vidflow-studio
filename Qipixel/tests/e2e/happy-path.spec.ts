import { test, expect } from "@playwright/test";

test("happy path: sign in and open a scene", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.clear();
  });

  await page.goto("/");
  await expect(page.getByLabel("Email")).toBeVisible();

  const email = `e2e_${Date.now()}@example.com`;
  const password = "password123";
  const sceneTitle = `E2E Scene ${Date.now()}`;
  const sceneNumber = `${Date.now()}`;

  await page.getByRole("button", { name: "Create one" }).click();
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByLabel("Confirm Password").fill(password);
  await page.getByRole("button", { name: "Create account" }).click();

  await expect(page.getByText("VidFlow Studio")).toBeVisible();
  await expect(page.getByRole("button", { name: "New Scene" })).toBeVisible();

  await page.getByRole("button", { name: "New Scene" }).click();
  await expect(page.getByRole("heading", { name: "Create New Scene" })).toBeVisible();
  await page.getByLabel("Scene #", { exact: true }).fill(sceneNumber);
  await page.getByLabel("Title *").fill(sceneTitle);
  await page.getByRole("button", { name: "Create Scene" }).click();

  await expect(page.getByRole("dialog", { name: "Create New Scene" })).toBeHidden();

  await expect(page.getByRole("heading", { name: sceneTitle })).toBeVisible();
  await page.getByRole("heading", { name: sceneTitle }).click();

  await expect(page.getByRole("heading", { name: "Narrative Goal" })).toBeVisible();
  await expect(page.getByText(sceneTitle, { exact: true })).toBeVisible();
  await expect(page.getByRole("tab", { name: "Script" })).toBeVisible();
});
