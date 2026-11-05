import { test, expect } from "@playwright/test";
import path from "path";

test.describe("Task 8: ImageManager E2E Component Verification", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/components-preview");
    // Click vào tab ImageManager
    const imageTab = page.getByRole("button", { name: /ImageManager & Preview/i });
    await expect(imageTab).toBeVisible();
    await imageTab.click();
  });

  test("1. Renders ImageManager interface and displays initial images", async ({ page }) => {
    // Header thêm ảnh mới
    await expect(page.getByText("Thêm hình ảnh mới")).toBeVisible();
    await expect(page.getByTestId("tab-upload-file")).toBeVisible();
    await expect(page.getByTestId("tab-upload-url")).toBeVisible();

    // Thẻ ảnh ban đầu
    const imageCards = page.locator('[data-testid^="image-card-"]');
    const count = await imageCards.count();
    expect(count).toBeGreaterThanOrEqual(1);

    // Kiểm tra có ảnh mang nhãn Ảnh đại diện (badge-primary-0)
    const primaryBadge = page.locator('[data-testid^="badge-primary-"]').first();
    await expect(primaryBadge).toBeVisible();
    await expect(primaryBadge).toContainText(/Ảnh đại diện/i);
  });

  test("2. Reorder buttons exist on image cards", async ({ page }) => {
    const moveDownBtns = page.locator('[data-testid^="btn-move-down-"]');
    const moveUpBtns = page.locator('[data-testid^="btn-move-up-"]');

    await expect(moveDownBtns.first()).toBeVisible();
    await expect(moveUpBtns.first()).toBeVisible();
  });

  test("3. Upload button accepts image file and adds preview card", async ({ page }) => {
    const fileInput = page.locator('input[type="file"][data-testid="file-input"]');
    await expect(fileInput).toHaveCount(1);

    // Chuẩn bị file ảnh test
    const testImagePath = path.join(
      "C:",
      "Users",
      "Maxsys",
      ".gemini",
      "antigravity",
      "brain",
      "cddcf97b-e3c6-43d5-aadb-b918dab5c8d8",
      "scratch",
      "real_sample.jpg"
    );

    const initialCardsCount = await page.locator('[data-testid^="image-card-"]').count();

    // Set file vào hidden file input
    await fileInput.setInputFiles(testImagePath);

    // Kiểm tra preview hiển thị
    await expect(page.locator('[data-testid="image-preview"]')).toBeVisible();

    // Điền alt text và submit
    const altInput = page.locator('input[data-testid="input-alt-text"]');
    if (await altInput.isVisible()) {
      await altInput.fill("Ảnh bò cuộn lá lốt thơm lừng");
    }

    const submitBtn = page.getByTestId("btn-submit-image");
    await expect(submitBtn).toBeEnabled();
    await submitBtn.click();

    // Kiểm tra xuất hiện ảnh mới trong danh sách
    await page.waitForTimeout(600);
    const newCardsCount = await page.locator('[data-testid^="image-card-"]').count();
    expect(newCardsCount).toBe(initialCardsCount + 1);
  });

  test("4. Delete image triggers confirmation dialog and removes image", async ({ page }) => {
    // Thêm 1 ảnh trước để danh sách có ít nhất 2 ảnh
    const fileInput = page.locator('input[type="file"][data-testid="file-input"]');
    const testImagePath = path.join(
      "C:",
      "Users",
      "Maxsys",
      ".gemini",
      "antigravity",
      "brain",
      "cddcf97b-e3c6-43d5-aadb-b918dab5c8d8",
      "scratch",
      "real_sample.png"
    );
    await fileInput.setInputFiles(testImagePath);
    await page.getByTestId("btn-submit-image").click();
    await page.waitForTimeout(600);

    const deleteBtns = page.locator('[data-testid^="btn-delete-image-"]');
    const countBefore = await deleteBtns.count();

    // Bấm nút xóa ảnh cuối cùng
    await deleteBtns.last().click();

    // Kiểm tra Dialog xác nhận
    const confirmBtn = page.getByTestId("btn-confirm-delete-image");
    await expect(confirmBtn).toBeVisible();
    await confirmBtn.click();

    // Sau khi xóa, số lượng thẻ ảnh giảm đi 1
    await page.waitForTimeout(600);
    const countAfter = await page.locator('[data-testid^="image-card-"]').count();
    expect(countAfter).toBe(countBefore - 1);
  });
});
