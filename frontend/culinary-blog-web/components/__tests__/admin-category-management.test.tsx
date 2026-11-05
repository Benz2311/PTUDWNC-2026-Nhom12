import React from "react";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import AdminCategoriesPage from "@/components/admin/AdminCategoriesPage";
import {
  getCategories,
  createCategory,
  updateCategory,
  deleteCategory,
  getCategoryStatistics,
  CategoryConflictError,
} from "@/lib/api/categories";

// Mock next/navigation
jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    prefetch: jest.fn(),
  }),
  usePathname: () => "/admin/categories",
}));

// Mock contexts
jest.mock("@/contexts/AuthContext", () => ({
  useAuth: () => ({
    user: { id: "admin-1", name: "Quản trị viên", role: "Admin", email: "admin@culinary.vn" },
    isAuthenticated: true,
    isLoading: false,
  }),
}));

const mockSuccessToast = jest.fn();
const mockErrorToast = jest.fn();

jest.mock("@/components/ui/toast", () => ({
  useToast: () => ({
    success: mockSuccessToast,
    error: mockErrorToast,
  }),
}));

// Mock categories API
jest.mock("@/lib/api/categories", () => ({
  getCategories: jest.fn(),
  createCategory: jest.fn(),
  updateCategory: jest.fn(),
  deleteCategory: jest.fn(),
  getCategoryStatistics: jest.fn(),
  CategoryConflictError: class extends Error {
    status = 409;
    constructor(msg: string) {
      super(msg);
      this.name = "CategoryConflictError";
    }
  },
}));

const mockGetCategories = getCategories as jest.MockedFunction<typeof getCategories>;
const mockCreateCategory = createCategory as jest.MockedFunction<typeof createCategory>;
const mockUpdateCategory = updateCategory as jest.MockedFunction<typeof updateCategory>;
const mockDeleteCategory = deleteCategory as jest.MockedFunction<typeof deleteCategory>;
const mockGetCategoryStatistics = getCategoryStatistics as jest.MockedFunction<typeof getCategoryStatistics>;

describe("Lê Thị Ánh Nhung - Category Completion & Admin Category Management", () => {
  const sampleCategories = [
    {
      id: "cat-1",
      name: "Món Việt",
      slug: "mon-viet",
      description: "Hương vị ẩm thực ba miền truyền thống",
      imageUrl: "https://example.com/mon-viet.jpg",
      orderIndex: 1,
      recipeCount: 15,
    },
    {
      id: "cat-2",
      name: "Món Tráng Miệng",
      slug: "mon-trang-mieng",
      description: "Chè, bánh ngọt và kem thơm ngon",
      imageUrl: "https://example.com/trang-mieng.jpg",
      orderIndex: 2,
      recipeCount: 0,
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    mockGetCategories.mockResolvedValue(sampleCategories);
    mockGetCategoryStatistics.mockResolvedValue({
      totalCategories: 2,
      totalRecipes: 15,
      publishedRecipes: 12,
      draftRecipes: 3,
      archivedRecipes: 0,
      categories: [],
      topCategories: [],
      recipesByMonth: [],
    });
  });

  it("1. Render danh sách danh mục Admin đầy đủ với OrderIndex và RecipeCount", async () => {
    render(<AdminCategoriesPage />);

    expect(screen.getByText("Quản lý danh mục công thức")).toBeInTheDocument();

    await waitFor(() => {
      // Find table/cards containing categories
      expect(screen.getAllByText("Món Việt").length).toBeGreaterThan(0);
      expect(screen.getAllByText("Món Tráng Miệng").length).toBeGreaterThan(0);
    });

    // Check slug and recipe count badges
    expect(screen.getAllByText("/mon-viet").length).toBeGreaterThan(0);
    expect(screen.getAllByText("15 món").length).toBeGreaterThan(0);
    expect(screen.getAllByText("0 món").length).toBeGreaterThan(0);
  });

  it("2. Tìm kiếm danh mục theo từ khóa lọc đúng kết quả", async () => {
    render(<AdminCategoriesPage />);

    await waitFor(() => {
      expect(screen.getAllByText("Món Việt").length).toBeGreaterThan(0);
    });

    const searchInput = screen.getByPlaceholderText("Tìm theo tên, mô tả hoặc slug...");
    fireEvent.change(searchInput, { target: { value: "Tráng Miệng" } });

    expect(screen.getAllByText("Món Tráng Miệng").length).toBeGreaterThan(0);
    expect(screen.queryByText("Món Việt")).not.toBeInTheDocument();
  });

  it("3. Mở modal tạo Category, validate và tạo mới thành công", async () => {
    const newCategory = {
      id: "cat-3",
      name: "Đồ Uống Giải Nhiệt",
      slug: "do-uong-giai-nhiet",
      description: "Trà hoa quả và sinh tố tươi mát",
      imageUrl: "https://example.com/drink.jpg",
      orderIndex: 3,
      recipeCount: 0,
    };
    mockCreateCategory.mockResolvedValue(newCategory);

    render(<AdminCategoriesPage />);

    await waitFor(() => {
      expect(screen.getAllByText("Món Việt").length).toBeGreaterThan(0);
    });

    // Bấm nút thêm danh mục
    fireEvent.click(screen.getByRole("button", { name: /Thêm danh mục/i }));

    expect(screen.getByText("Tạo danh mục mới")).toBeInTheDocument();

    // Nhập thông tin form
    const nameInput = screen.getByPlaceholderText(/Ví dụ: Món Việt/i);
    const descInput = screen.getByPlaceholderText(/Giới thiệu đôi nét về danh mục/i);
    const imageInput = screen.getByPlaceholderText(/https:\/\/images\.unsplash/i);

    fireEvent.change(nameInput, { target: { value: "Đồ Uống Giải Nhiệt" } });
    fireEvent.change(descInput, { target: { value: "Trà hoa quả và sinh tố tươi mát" } });
    fireEvent.change(imageInput, { target: { value: "https://example.com/drink.jpg" } });

    // Submit form
    fireEvent.click(screen.getByRole("button", { name: /^Tạo danh mục$/i }));

    await waitFor(() => {
      expect(mockCreateCategory).toHaveBeenCalledWith({
        name: "Đồ Uống Giải Nhiệt",
        description: "Trà hoa quả và sinh tố tươi mát",
        imageUrl: "https://example.com/drink.jpg",
        orderIndex: expect.any(Number),
      });
      expect(mockSuccessToast).toHaveBeenCalledWith("Tạo danh mục mới thành công!");
    });
  });

  it("4. Xử lý lỗi 409 Conflict khi tạo danh mục trùng tên", async () => {
    mockCreateCategory.mockRejectedValue(
      new CategoryConflictError("Tên danh mục đã tồn tại trong hệ thống.")
    );

    render(<AdminCategoriesPage />);

    await waitFor(() => {
      expect(screen.getAllByText("Món Việt").length).toBeGreaterThan(0);
    });

    fireEvent.click(screen.getByRole("button", { name: /Thêm danh mục/i }));

    const nameInput = screen.getByPlaceholderText(/Ví dụ: Món Việt/i);
    fireEvent.change(nameInput, { target: { value: "Món Việt" } });

    fireEvent.click(screen.getByRole("button", { name: /^Tạo danh mục$/i }));

    await waitFor(() => {
      expect(screen.getByText("Lỗi xung đột dữ liệu (409 Conflict)")).toBeInTheDocument();
      expect(mockErrorToast).toHaveBeenCalledWith("Tên danh mục đã tồn tại trong hệ thống.");
    });
  });

  it("5. Mở modal sửa Category và cập nhật thành công đồng bộ UI", async () => {
    mockUpdateCategory.mockResolvedValue();

    render(<AdminCategoriesPage />);

    await waitFor(() => {
      expect(screen.getAllByText("Món Việt").length).toBeGreaterThan(0);
    });

    // Bấm nút Sửa danh mục đầu tiên
    const editButtons = screen.getAllByTitle("Chỉnh sửa danh mục");
    fireEvent.click(editButtons[0]);

    expect(screen.getByRole("heading", { name: "Cập nhật danh mục" })).toBeInTheDocument();

    const nameInput = screen.getByDisplayValue("Món Việt");
    fireEvent.change(nameInput, { target: { value: "Món Việt Đặc Sắc" } });

    fireEvent.click(screen.getByRole("button", { name: /Cập nhật danh mục/i }));

    await waitFor(() => {
      expect(mockUpdateCategory).toHaveBeenCalledWith("cat-1", {
        name: "Món Việt Đặc Sắc",
        description: "Hương vị ẩm thực ba miền truyền thống",
        imageUrl: "https://example.com/mon-viet.jpg",
        orderIndex: 1,
      });
      expect(mockSuccessToast).toHaveBeenCalledWith("Cập nhật danh mục thành công!");
    });
  });

  it("6. Xóa danh mục thành công khi không có Recipe liên kết", async () => {
    mockDeleteCategory.mockResolvedValue();

    render(<AdminCategoriesPage />);

    await waitFor(() => {
      expect(screen.getAllByText("Món Tráng Miệng").length).toBeGreaterThan(0);
    });

    // Món Tráng Miệng có recipeCount = 0 (an toàn để xóa)
    const deleteButtons = screen.getAllByTitle("Xóa danh mục");
    fireEvent.click(deleteButtons[1]); // Món Tráng Miệng

    expect(screen.getByText("Xác nhận xóa danh mục")).toBeInTheDocument();

    // Bấm nút Xóa trong confirmation modal
    fireEvent.click(screen.getByRole("button", { name: "Xác nhận xóa" }));

    await waitFor(() => {
      expect(mockDeleteCategory).toHaveBeenCalledWith("cat-2");
      expect(mockSuccessToast).toHaveBeenCalledWith('Đã xóa danh mục "Món Tráng Miệng" thành công!');
    });
  });

  it("7. Xử lý lỗi 409 Conflict khi cố xóa danh mục vẫn còn Recipe liên kết", async () => {
    mockDeleteCategory.mockRejectedValue(
      new CategoryConflictError("Không thể xóa danh mục vì vẫn còn công thức liên kết.")
    );

    render(<AdminCategoriesPage />);

    await waitFor(() => {
      expect(screen.getAllByText("Món Việt").length).toBeGreaterThan(0);
    });

    // Món Việt có recipeCount = 15
    const deleteButtons = screen.getAllByTitle("Xóa danh mục");
    fireEvent.click(deleteButtons[0]); // Món Việt

    // Modal hiển thị cảnh báo nghiệp vụ
    expect(screen.getByText(/Cảnh báo: Danh mục đang có 15 công thức!/i)).toBeInTheDocument();

    // Bấm nút Xóa
    fireEvent.click(screen.getByRole("button", { name: "Xác nhận xóa" }));

    await waitFor(() => {
      expect(mockDeleteCategory).toHaveBeenCalledWith("cat-1");
      expect(mockErrorToast).toHaveBeenCalledWith(
        expect.stringContaining("Không thể xóa danh mục \"Món Việt\" vì vẫn còn 15 công thức liên kết")
      );
    });
  });
});
