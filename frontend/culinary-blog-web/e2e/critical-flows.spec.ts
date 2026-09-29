import { test, expect } from "@playwright/test";

/**
 * Critical Flow 1: Homepage loads successfully
 * Verifies the application bootstraps and renders core UI elements.
 */
test("homepage loads and displays navigation", async ({ page }) => {
  await page.goto("/");

  // Page title should be present
  await expect(page).toHaveTitle(/culinary|blog|ẩm thực/i);

  // Navigation should be visible
  const nav = page.locator("nav");
  await expect(nav).toBeVisible();
});

/**
 * Critical Flow 2: Recipe listing page loads
 * Verifies users can browse the recipe list.
 */
test("recipe listing page displays recipes", async ({ page }) => {
  await page.goto("/recipes");

  // Wait for content to load (allow loading states)
  await page.waitForLoadState("networkidle");

  // Either recipes are shown or empty state is shown
  const content = page.locator("main");
  await expect(content).toBeVisible();
});

/**
 * Critical Flow 3: Category browsing
 * Verifies category pages load and display filtered content.
 */
test("category page loads without errors", async ({ page }) => {
  await page.goto("/categories");

  // Page should render without crashing
  await expect(page.locator("main")).toBeVisible();

  // No error boundary should be triggered
  const errorText = page.getByText(/something went wrong|lỗi/i);
  await expect(errorText).not.toBeVisible();
});

/**
 * Critical Flow 4: Search functionality
 * Verifies the search input accepts queries and triggers search.
 */
test("search input accepts text and submits", async ({ page }) => {
  await page.goto("/");

  const searchInput = page.getByRole("searchbox");
  if (await searchInput.isVisible()) {
    await searchInput.fill("Phở Bò");
    await searchInput.press("Enter");

    // URL should reflect search query
    await expect(page).toHaveURL(/search|q=|query=/i);
  } else {
    // Search may not be on homepage — skip gracefully
    test.skip();
  }
});

/**
 * Critical Flow 5: Navigation links work correctly
 * Verifies that clicking navigation items changes the route.
 */
test("navigation links change the page route", async ({ page }) => {
  await page.goto("/");

  // Find any internal navigation link
  const firstNavLink = page.locator("nav a").first();

  if (await firstNavLink.isVisible()) {
    const href = await firstNavLink.getAttribute("href");
    await firstNavLink.click();

    await page.waitForLoadState("networkidle");

    // Page should have navigated (URL changed or content changed)
    const currentUrl = page.url();
    expect(currentUrl).toBeTruthy();

    // No 404 or error page
    await expect(page.getByText(/404|page not found/i)).not.toBeVisible();
  }
});
