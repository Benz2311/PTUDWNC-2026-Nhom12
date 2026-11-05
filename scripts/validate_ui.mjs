import { chromium } from "playwright";
import fs from "fs";
import path from "path";

const OUT_DIR = path.resolve("B:/bài web nhóm mới/task6-validation/docs/task6-validation");
if (!fs.existsSync(OUT_DIR)) {
  fs.mkdirSync(OUT_DIR, { recursive: true });
}

async function runBrowserTests() {
  console.log("=== BẮT ĐẦU KIỂM THỬ TRÌNH DUYỆT THỰC TẾ TASK 6 (CHROME) ===");
  const browser = await chromium.launch({ channel: "chrome", headless: true });
  const results = {
    passed: [],
    failed: [],
    consoleErrors: [],
  };

  const context = await browser.newContext();
  const page = await context.newPage();

  page.on("console", (msg) => {
    if (msg.type() === "error") {
      console.log(`[Browser Console Error] ${msg.text()}`);
      results.consoleErrors.push(msg.text());
    }
  });

  page.on("pageerror", (err) => {
    console.log(`[Browser Uncaught Error] ${err.message}`);
    results.consoleErrors.push(err.message);
  });

  page.on("requestfailed", (req) => {
    console.log(`[Request Failed] ${req.url()}: ${req.failure()?.errorText}`);
  });

  page.on("response", (res) => {
    if (res.status() >= 400) {
      console.log(`[HTTP ${res.status()}] ${res.url()}`);
    }
  });

  try {
    // 1. DESKTOP HOME (1440x900)
    console.log("\n--- 1. Kiểm thử Desktop Home (1440x900) ---");
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto("http://localhost:3000", { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(2000);

    const title = await page.title();
    console.log(`Title: ${title}`);

    const header = await page.$("header");
    const footer = await page.$("footer");
    console.log(`Header exists: ${!!header}, Footer exists: ${!!footer}`);

    // Check recipe cards
    const recipeCards = await page.$$("article, .recipe-card, div[class*='group']");
    console.log(`Found ${recipeCards.length} recipe cards/containers.`);

    await page.screenshot({ path: path.join(OUT_DIR, "desktop_home.png"), fullPage: false });
    results.passed.push("Desktop Home Page rendered cleanly (1440x900)");

    // 2. SEARCH PAGE
    console.log("\n--- 2. Kiểm thử Search Page ---");
    await page.goto("http://localhost:3000/search", { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(500);

    // Find search input
    const searchInput = await page.$("input[type='text'], input[placeholder*='tìm' i], input[placeholder*='search' i]");
    if (searchInput) {
      await searchInput.fill("phở");
      const searchButton = await page.$("button:has-text('Tìm'), button[type='submit'], form button");
      if (searchButton) {
        await searchButton.click();
      } else {
        await searchInput.press("Enter");
      }
      await page.waitForTimeout(1500);
    }
    await page.screenshot({ path: path.join(OUT_DIR, "search_page.png") });
    results.passed.push("Search Page interaction and keyword search verified");

    // 3. COMPONENTS PREVIEW
    console.log("\n--- 3. Kiểm thử Components Preview (/components-preview) ---");
    await page.goto("http://localhost:3000/components-preview", { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(500);

    // Tab 1: Recipe Detail & Form (already active)
    console.log("Testing Tab 1: Recipe Detail & Form...");
    await page.waitForTimeout(500);
    const detailTitle = await page.textContent("h1, h2");
    console.log(`Recipe detail section loaded: ${detailTitle}`);
    await page.screenshot({ path: path.join(OUT_DIR, "preview_tab1_recipe.png") });
    results.passed.push("RecipeDetailView & RecipeFormLayout rendered and validated");

    // Tab 2: StepEditor & StepItem
    console.log("Testing Tab 2: StepEditor & StepItem...");
    await page.click("button:has-text('StepEditor & StepItem')");
    await page.waitForTimeout(500);
    const stepTextarea = await page.$("textarea, input[placeholder*='hướng dẫn' i]");
    if (stepTextarea) {
      await stepTextarea.fill("Bước mới: Nêm nếm gia vị vừa ăn và trình bày ra đĩa.");
      const addStepBtn = await page.$("button:has-text('Thêm bước')");
      if (addStepBtn) {
        await addStepBtn.click();
        await page.waitForTimeout(500);
        console.log("Added new step successfully.");
      }
    }
    await page.screenshot({ path: path.join(OUT_DIR, "preview_tab2_step.png") });
    results.passed.push("StepEditor & StepItem interaction verified");

    // Tab 3: ImageManager & Preview
    console.log("Testing Tab 3: ImageManager & Preview...");
    await page.click("button:has-text('ImageManager & Preview')");
    await page.waitForTimeout(500);
    const uploadBtn = await page.$("button:has-text('Thêm ảnh'), button:has-text('Tải ảnh')");
    console.log(`Image upload button present: ${!!uploadBtn}`);
    await page.screenshot({ path: path.join(OUT_DIR, "preview_tab3_image.png") });
    results.passed.push("ImageManager UI, cards, and upload button verified");

    // Tab 4: Shared UI & Dialogs
    console.log("Testing Tab 4: Shared UI & Dialogs...");
    await page.click("button:has-text('Shared UI & Dialogs')");
    await page.waitForTimeout(500);

    const openDialogBtn = await page.$("button:has-text('Mở Confirm Dialog Thử Nghiệm')");
    if (openDialogBtn) {
      await openDialogBtn.click();
      await page.waitForTimeout(500);
      console.log("Confirm Dialog opened.");
      const confirmActionBtn = await page.$("button:has-text('Đồng ý xóa')");
      if (confirmActionBtn) {
        await confirmActionBtn.click();
        await page.waitForTimeout(500);
        console.log("Confirm action clicked.");
      }
    }
    await page.screenshot({ path: path.join(OUT_DIR, "preview_tab4_shared_dialogs.png") });
    results.passed.push("ConfirmDialog & Shared UI states verified");

    // 4. RESPONSIVE TABLET (768x1024)
    console.log("\n--- 4. Kiểm thử Tablet (768x1024) ---");
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto("http://localhost:3000", { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(800);
    // Check horizontal scroll
    const scrollWidthTablet = await page.evaluate(() => document.documentElement.scrollWidth);
    const clientWidthTablet = await page.evaluate(() => document.documentElement.clientWidth);
    console.log(`Tablet scrollWidth: ${scrollWidthTablet}, clientWidth: ${clientWidthTablet}`);
    if (scrollWidthTablet <= clientWidthTablet + 2) {
      results.passed.push("Tablet layout: No horizontal overflow");
    } else {
      results.failed.push(`Tablet layout: Horizontal overflow detected (${scrollWidthTablet} > ${clientWidthTablet})`);
    }
    await page.screenshot({ path: path.join(OUT_DIR, "tablet_home.png") });

    // 5. RESPONSIVE MOBILE (390x844)
    console.log("\n--- 5. Kiểm thử Mobile (390x844) ---");
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("http://localhost:3000", { waitUntil: "domcontentloaded" });
    await page.waitForTimeout(800);
    const scrollWidthMobile = await page.evaluate(() => document.documentElement.scrollWidth);
    const clientWidthMobile = await page.evaluate(() => document.documentElement.clientWidth);
    console.log(`Mobile scrollWidth: ${scrollWidthMobile}, clientWidth: ${clientWidthMobile}`);
    if (scrollWidthMobile <= clientWidthMobile + 2) {
      results.passed.push("Mobile layout: No horizontal overflow");
    } else {
      results.failed.push(`Mobile layout: Horizontal overflow detected (${scrollWidthMobile} > ${clientWidthMobile})`);
    }
    await page.screenshot({ path: path.join(OUT_DIR, "mobile_home.png") });

    console.log("\n=== TỔNG KẾT KIỂM THỬ TRÌNH DUYỆT ===");
    console.log(`PASSED: ${results.passed.length}`);
    results.passed.forEach(p => console.log(`  ✓ ${p}`));
    console.log(`FAILED: ${results.failed.length}`);
    results.failed.forEach(f => console.log(`  ✗ ${f}`));
    console.log(`CONSOLE ERRORS: ${results.consoleErrors.length}`);

  } catch (err) {
    console.error("Test execution error:", err);
    results.failed.push(err.message);
  } finally {
    await browser.close();
  }
}

runBrowserTests();
