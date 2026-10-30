import { test, expect } from "@playwright/test";

test.describe("E2E Recipe Step Management (StepEditor & StepItem)", () => {
  test.beforeEach(async ({ page }) => {
    // Truy cập trang Components Preview
    await page.goto("/components-preview");
    await page.waitForLoadState("networkidle");

    // Chọn tab StepEditor & StepItem (Mạnh)
    const stepTabBtn = page.getByRole("button", { name: /StepEditor & StepItem/i });
    await expect(stepTabBtn).toBeVisible();
    await stepTabBtn.click();

    // Xác nhận container StepEditor đã hiển thị
    const stepEditor = page.locator('[data-testid="step-editor"]');
    await expect(stepEditor).toBeVisible();
  });

  test("1. Hiển thị danh sách bước nấu ăn ban đầu với đầy đủ StepNumber và thông tin", async ({ page }) => {
    // Kiểm tra danh sách hiển thị
    const stepList = page.locator('[data-testid="step-list"]');
    await expect(stepList).toBeVisible();

    // Kiểm tra các bước mẫu hiển thị số thứ tự 01, 02
    await expect(page.locator('[data-testid="step-item-0"]')).toBeVisible();
    await expect(page.locator('[data-testid="step-item-0"]').getByText("01")).toBeVisible();
    await expect(page.locator('[data-testid="step-item-1"]').getByText("02")).toBeVisible();
  });

  test("2. Validation: Báo lỗi khi thiếu mô tả hoặc thời gian hẹn giờ không hợp lệ", async ({ page }) => {
    // Mở form thêm bước
    await page.locator('[data-testid="btn-add-step"]').click();
    await expect(page.locator('[data-testid="step-form"]')).toBeVisible();

    // Để trống mô tả và bấm Lưu -> Phải hiện lỗi
    await page.locator('[data-testid="btn-save-step"]').click();
    const errorDesc = page.locator('[data-testid="error-step-description"]');
    await expect(errorDesc).toBeVisible();
    await expect(errorDesc).toContainText("Nội dung hướng dẫn bước là bắt buộc");

    // Nhập mô tả hợp lệ nhưng hẹn giờ âm
    await page.locator('[data-testid="input-step-description"]').fill("Mô tả hợp lệ cho kiểm thử E2E");
    await page.locator('[data-testid="input-step-timer"]').fill("-10");
    await page.locator('[data-testid="btn-save-step"]').click();

    const errorTimer = page.locator('[data-testid="error-step-timer"]');
    await expect(errorTimer).toBeVisible();
    await expect(errorTimer).toContainText("Thời gian hẹn giờ phải từ 0 đến 1440 phút");

    // Hủy form
    await page.locator('[data-testid="btn-cancel-step-form"]').click();
    await expect(page.locator('[data-testid="step-form"]')).not.toBeVisible();
  });

  test("3. Thêm bước nấu ăn mới thành công và hiển thị đúng thứ tự tăng dần", async ({ page }) => {
    // Đếm số lượng bước ban đầu
    const initialItems = await page.locator('[data-testid^="step-item-"]').count();

    // Mở form thêm bước
    await page.locator('[data-testid="btn-add-step"]').click();

    // Điền thông tin bước mới
    await page.locator('[data-testid="input-step-title"]').fill("Trang trí và rắc tiêu");
    await page.locator('[data-testid="input-step-description"]').fill("Múc thịt ra đĩa sâu lòng, rắc thêm hạt tiêu và rau mùi.");
    await page.locator('[data-testid="input-step-timer"]').fill("3");

    // Lưu bước
    await page.locator('[data-testid="btn-save-step"]').click();

    // Đảm bảo form đã đóng và số lượng bước tăng thêm 1
    await expect(page.locator('[data-testid="step-form"]')).not.toBeVisible();
    const updatedCount = await page.locator('[data-testid^="step-item-"]').count();
    expect(updatedCount).toBe(initialItems + 1);

    // Kiểm tra nội dung bước mới hiển thị
    await expect(page.getByText("Trang trí và rắc tiêu")).toBeVisible();
  });

  test("4. Chỉnh sửa nội dung bước nấu ăn", async ({ page }) => {
    // Bấm nút Sửa ở bước 0
    await page.locator('[data-testid="btn-edit-step-0"]').click();
    await expect(page.locator('[data-testid="step-form"]')).toBeVisible();

    // Chỉnh sửa tiêu đề
    const titleInput = page.locator('[data-testid="input-step-title"]');
    await titleInput.clear();
    await titleInput.fill("Sơ chế kỹ rau củ");

    // Bấm lưu
    await page.locator('[data-testid="btn-save-step"]').click();
    await expect(page.locator('[data-testid="step-form"]')).not.toBeVisible();

    // Kiểm tra tiêu đề mới xuất hiện
    await expect(page.getByText("Sơ chế kỹ rau củ")).toBeVisible();
  });

  test("5. Sắp xếp thứ tự (Reorder) di chuyển bước lên / xuống", async ({ page }) => {
    // Lấy tiêu đề bước 0 và bước 1 trước khi đổi chỗ
    const firstTitleText = await page.locator('[data-testid="step-item-0"] h4').innerText();
    const secondTitleText = await page.locator('[data-testid="step-item-1"] h4').innerText();

    // Bấm Move Down ở bước 0
    const moveDownBtn = page.locator('[data-testid="btn-move-down-0"]');
    await expect(moveDownBtn).toBeEnabled();
    await moveDownBtn.click();

    // Sau khi đổi chỗ, hai bước đã hoán đổi vị trí tiêu đề cho nhau
    const newFirstTitleText = await page.locator('[data-testid="step-item-0"] h4').innerText();
    const newSecondTitleText = await page.locator('[data-testid="step-item-1"] h4').innerText();
    expect(newFirstTitleText).toBe(secondTitleText);
    expect(newSecondTitleText).toBe(firstTitleText);
  });

  test("6. Xóa bước nấu ăn với Modal xác nhận và tự động đánh số lại 1..N", async ({ page }) => {
    const initialCount = await page.locator('[data-testid^="step-item-"]').count();

    // Bấm nút Xóa ở bước đầu tiên
    await page.locator('[data-testid="btn-delete-step-0"]').click();

    // Modal xác nhận xuất hiện
    const confirmDialog = page.locator('[data-testid="delete-confirmation-dialog"]');
    await expect(confirmDialog).toBeVisible();
    await expect(confirmDialog).toContainText("Xác nhận xóa bước thực hiện");

    // Bấm Xác nhận xóa
    await page.locator('[data-testid="btn-confirm-delete"]').click();
    await expect(confirmDialog).not.toBeVisible();

    // Số lượng bước giảm đi 1
    const finalCount = await page.locator('[data-testid^="step-item-"]').count();
    expect(finalCount).toBe(initialCount - 1);

    // Bước đầu tiên vẫn được đánh số lại là 01
    await expect(page.locator('[data-testid="step-item-0"]').getByText("01")).toBeVisible();
  });
});
