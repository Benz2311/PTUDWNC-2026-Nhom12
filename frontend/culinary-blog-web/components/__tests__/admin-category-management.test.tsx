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

});
