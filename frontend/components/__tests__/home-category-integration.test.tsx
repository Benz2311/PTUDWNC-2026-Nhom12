import React from "react";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import CategoriesPage from "@/app/(public)/categories/page";
import CategoryDetailPage from "@/app/(public)/categories/[slug]/page";
import { PublicSidebar } from "@/components/layout/PublicSidebar";
import { PublicLayout } from "@/components/layout/PublicLayout";
import { getCategories, getCategoryBySlug } from "@/lib/api/categories";

// Mock API modules
jest.mock("@/lib/api/categories", () => ({
  getCategories: jest.fn(),
  getCategoryBySlug: jest.fn(),
  getCategoryStatistics: jest.fn(),
}));

jest.mock("@/lib/api/recipes", () => ({
  getRecipes: jest.fn(),
  getRecipeBySlug: jest.fn(),
}));

// Mock next/navigation
const mockPush = jest.fn();
const mockReplace = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
    replace: mockReplace,
    prefetch: jest.fn(),
  }),
  usePathname: () => "/categories",
  useParams: () => ({ slug: "mon-viet" }),
  useSearchParams: () => ({
    get: (key: string) => (key === "page" ? "1" : null),
    toString: () => "page=1",
  }),
}));

const mockGetCategories = getCategories as jest.MockedFunction<typeof getCategories>;
const mockGetCategoryBySlug = getCategoryBySlug as jest.MockedFunction<typeof getCategoryBySlug>;

describe("Bữa 7: Lê Thị Ánh Nhung - Home / Category / Public Layout Integration", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("PublicSidebar Component", () => {
    const mockCategories = [
      { id: "1", name: "Món Việt", slug: "mon-viet", recipeCount: 15 },
      { id: "2", name: "Món Á", slug: "mon-a", recipeCount: 8 },
    ];

    it("renders real category list with recipe counts and links to category detail", () => {
      render(
        <PublicSidebar
          categories={mockCategories}
          activeCategorySlug="mon-viet"
        />
      );

      expect(screen.getByText("Danh mục món ăn")).toBeInTheDocument();
      expect(screen.getByText("Món Việt")).toBeInTheDocument();
      expect(screen.getByText("15")).toBeInTheDocument();
      expect(screen.getByText("Món Á")).toBeInTheDocument();
      expect(screen.getByText("8")).toBeInTheDocument();

      // Check navigation link
      const links = screen.getAllByRole("link");
      const monVietLink = links.find((l) => l.getAttribute("href") === "/categories/mon-viet");
      expect(monVietLink).toBeDefined();
    });

    it("renders loading skeleton when isLoading is true", () => {
      const { container } = render(<PublicSidebar isLoading />);
      expect(screen.getByText("Danh mục món ăn")).toBeInTheDocument();
      // Should show skeleton elements
      expect(container.querySelectorAll(".animate-pulse").length).toBeGreaterThan(0);
    });

    it("renders error state with retry button when error occurs", () => {
      const onRetryMock = jest.fn();
      render(
        <PublicSidebar
          error="Lỗi kết nối máy chủ"
          onRetry={onRetryMock}
        />
      );

      expect(screen.getByText("Lỗi kết nối máy chủ")).toBeInTheDocument();
      const retryBtn = screen.getByText("Thử lại");
      fireEvent.click(retryBtn);
      expect(onRetryMock).toHaveBeenCalledTimes(1);
    });
  });

  describe("CategoriesPage (/categories)", () => {
    const mockCategoriesList = [
      {
        id: "cat-1",
        name: "Món Việt Truyền Thống",
        slug: "mon-viet",
        description: "Các món ăn đậm đà hương vị quê hương",
        imageUrl: "https://example.com/mon-viet.jpg",
        recipeCount: 24,
      },
      {
        id: "cat-2",
        name: "Tráng Miệng & Đồ Ngọt",
        slug: "trang-mieng",
        description: "Bánh ngọt và chè thơm ngon",
        imageUrl: null,
        recipeCount: 12,
      },
    ];

    it("renders category list with name, description, recipeCount and image", async () => {
      mockGetCategories.mockResolvedValueOnce(mockCategoriesList);

      render(<CategoriesPage />);

      // Should show header banner
      expect(screen.getByText("Khám phá danh mục món ăn")).toBeInTheDocument();

      // Wait for data to load
      await waitFor(() => {
        expect(screen.getByText("Món Việt Truyền Thống")).toBeInTheDocument();
      });

      expect(screen.getByText("Các món ăn đậm đà hương vị quê hương")).toBeInTheDocument();
      expect(screen.getByText("24 công thức")).toBeInTheDocument();
      expect(screen.getByText("Tráng Miệng & Đồ Ngọt")).toBeInTheDocument();
      expect(screen.getByText("12 công thức")).toBeInTheDocument();

      // Check navigation link
      const detailLink = screen.getByLabelText("Xem chi tiết danh mục Món Việt Truyền Thống");
      expect(detailLink).toHaveAttribute("href", "/categories/mon-viet");
    });

    it("renders EmptyState when category list is empty", async () => {
      mockGetCategories.mockResolvedValueOnce([]);

      render(<CategoriesPage />);

      await waitFor(() => {
        expect(screen.getByText("Chưa có danh mục nào")).toBeInTheDocument();
      });
      expect(screen.getByText("Về trang chủ")).toBeInTheDocument();
    });

    it("renders ErrorState when fetching categories fails", async () => {
      mockGetCategories.mockRejectedValueOnce(new Error("Network Error"));

      render(<CategoriesPage />);

      await waitFor(() => {
        expect(screen.getByText("Không thể tải danh mục")).toBeInTheDocument();
      });
      expect(screen.getByText("Tải lại danh mục")).toBeInTheDocument();
    });
  });

  describe("CategoryDetailPage (/categories/[slug])", () => {
    const mockCategoryDetail = {
      category: {
        id: "cat-1",
        name: "Món Việt",
        slug: "mon-viet",
        description: "Món ăn truyền thống Việt Nam",
        imageUrl: "https://example.com/viet.jpg",
      },
      recipes: {
        items: [
          {
            id: "rec-1",
            title: "Phở Bò Hà Nội",
            slug: "pho-bo-ha-noi",
            description: "Nước dùng trong vắt, đậm đà vị quế hồi",
            cookTime: 120,
            prepTime: 30,
            difficulty: "Medium",
            status: "Published",
            publishedAt: "2026-10-20T10:00:00Z",
            primaryImageUrl: "https://example.com/pho.jpg",
          },
          {
            id: "rec-2",
            title: "Bún Chả Nem Cua Bể",
            slug: "bun-cha-nem-cua-be",
            description: "Chả nướng than hoa thơm lừng kèm nem giòn rụm",
            cookTime: 45,
            prepTime: 20,
            difficulty: "Easy",
            status: "Published",
            publishedAt: "2026-10-21T10:00:00Z",
            primaryImageUrl: null,
          },
        ],
        totalCount: 20,
        page: 1,
        pageSize: 9,
        totalPages: 3,
        hasNextPage: true,
        hasPreviousPage: false,
      },
    };

    it("renders category details, recipe cards, and pagination correctly", async () => {
      mockGetCategoryBySlug.mockResolvedValueOnce(mockCategoryDetail);

      render(<CategoryDetailPage />);

      await waitFor(() => {
        expect(screen.getByRole("heading", { level: 1, name: "Món Việt" })).toBeInTheDocument();
      });

      expect(screen.getByText("Món ăn truyền thống Việt Nam")).toBeInTheDocument();
      expect(screen.getByText("Phở Bò Hà Nội")).toBeInTheDocument();
      expect(screen.getByText("Bún Chả Nem Cua Bể")).toBeInTheDocument();

      // Pagination should be displayed because totalPages = 3 > 1
      expect(screen.getByText("1")).toBeInTheDocument();
      expect(screen.getByText("2")).toBeInTheDocument();
      expect(screen.getByText("3")).toBeInTheDocument();
    });

    it("renders EmptyState when category has no recipes", async () => {
      mockGetCategoryBySlug.mockResolvedValueOnce({
        category: {
          id: "cat-empty",
          name: "Món Chay Mới",
          slug: "mon-chay-moi",
          description: "Đang cập nhật",
        },
        recipes: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 9,
          totalPages: 0,
        },
      });

      render(<CategoryDetailPage />);

      await waitFor(() => {
        expect(
          screen.getByText("Chưa có công thức nào trong danh mục này")
        ).toBeInTheDocument();
      });
      expect(screen.getByText("Khám phá các danh mục khác")).toBeInTheDocument();
    });

    it("renders ErrorState when category is not found (404)", async () => {
      mockGetCategoryBySlug.mockResolvedValueOnce(null);

      render(<CategoryDetailPage />);

      await waitFor(() => {
        expect(screen.getByText("Không tìm thấy danh mục")).toBeInTheDocument();
      });
      expect(screen.getByText("Xem tất cả danh mục ẩm thực")).toBeInTheDocument();
    });
  });

  describe("PublicLayout and Navigation Integration", () => {
    it("renders PublicLayout with children and sidebar in responsive container", () => {
      render(
        <PublicLayout
          categories={[{ id: "1", name: "Tráng miệng", slug: "trang-mieng", recipeCount: 5 }]}
          sidebarPosition="left"
        >
          <div data-testid="main-content">Nội dung công thức</div>
        </PublicLayout>
      );

      expect(screen.getByTestId("main-content")).toBeInTheDocument();
      expect(screen.getByText("Tráng miệng")).toBeInTheDocument();
    });
  });
});
