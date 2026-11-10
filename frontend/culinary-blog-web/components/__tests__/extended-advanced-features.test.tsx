import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import { RecipeRecommendations } from "@/components/recipes/RecipeRecommendations";
import { TrendingRecipesView } from "@/components/recipes/TrendingRecipesView";
import { CollectionsPage } from "@/components/collections/CollectionsPage";
import { AdminActivityAnalyticsPage } from "@/components/admin/AdminActivityAnalyticsPage";
import { AdminCategoryPerformancePage } from "@/components/admin/AdminCategoryPerformancePage";

// Mock API clients
jest.mock("@/lib/api/engagement", () => ({
  getRecommendations: jest.fn().mockResolvedValue([
    {
      id: "rec-1",
      title: "Phở Bò Gia Truyền",
      slug: "pho-bo-gia-truyen",
      description: "Hương vị phở đậm đà",
      categoryName: "Món Nước",
      prepTimeMinutes: 20,
      cookTimeMinutes: 60,
      servings: 4,
      difficulty: "Medium",
    },
  ]),
  getTrendingRecipes: jest.fn().mockResolvedValue([
    {
      rank: 1,
      recipeId: "trend-1",
      title: "Bún Bò Huế Cay Nồng",
      slug: "bun-bo-hue",
      categoryName: "Món Nước",
      authorName: "Đầu bếp Nhung",
      viewsInPeriod: 1250,
      prepTimeMinutes: 30,
      cookTimeMinutes: 90,
      difficulty: "Hard",
    },
    {
      rank: 2,
      recipeId: "trend-2",
      title: "Gỏi Cuốn Tôm Thịt",
      slug: "goi-cuon-tom-thit",
      categoryName: "Món Khai Vị",
      authorName: "Đầu bếp Phước",
      viewsInPeriod: 890,
      prepTimeMinutes: 20,
      cookTimeMinutes: 10,
      difficulty: "Easy",
    },
  ]),
  recordRecipeView: jest.fn().mockResolvedValue(true),
}));

jest.mock("@/lib/api/categories", () => ({
  getCategories: jest.fn().mockResolvedValue([
    { id: "cat-1", name: "Món Nước", slug: "mon-nuoc", recipeCount: 5 },
    { id: "cat-2", name: "Món Khai Vị", slug: "mon-khai-vi", recipeCount: 3 },
  ]),
}));

jest.mock("@/lib/api/collections", () => ({
  getUserCollections: jest.fn().mockResolvedValue([
    {
      id: "col-1",
      name: "Món ngon cuối tuần",
      description: "Tổng hợp các món nấu cho gia đình",
      isPrivate: false,
      userId: "user-1",
      userName: "Lê Thị Ánh Nhung",
      recipeCount: 3,
      createdAt: "2026-11-01T00:00:00Z",
      previewImageUrls: [],
    },
  ]),
  createCollection: jest.fn().mockImplementation((payload) =>
    Promise.resolve({
      id: "col-2",
      ...payload,
      userId: "user-1",
      userName: "Lê Thị Ánh Nhung",
      recipeCount: 0,
      createdAt: new Date().toISOString(),
      previewImageUrls: [],
    })
  ),
  updateCollection: jest.fn().mockImplementation((id, payload) =>
    Promise.resolve({
      id,
      ...payload,
      userId: "user-1",
      userName: "Lê Thị Ánh Nhung",
      recipeCount: 3,
      createdAt: "2026-11-01T00:00:00Z",
      previewImageUrls: [],
    })
  ),
  deleteCollection: jest.fn().mockResolvedValue(true),
}));

jest.mock("@/lib/api/analytics", () => ({
  getActivityAnalytics: jest.fn().mockResolvedValue({
    days: 7,
    dailyStats: [
      { date: "2026-11-08", createdCount: 4, publishedCount: 3 },
      { date: "2026-11-09", createdCount: 6, publishedCount: 5 },
    ],
    summary: {
      totalCreatedCurrent: 10,
      totalCreatedPrevious: 5,
      createdGrowthRate: 100.0,
      totalPublishedCurrent: 8,
      totalPublishedPrevious: 4,
      publishedGrowthRate: 100.0,
    },
    topAuthors: [
      {
        authorId: "auth-1",
        authorName: "Lê Thị Ánh Nhung",
        authorEmail: "nhung@culinary.vn",
        recipesCreated: 6,
        recipesPublished: 5,
      },
    ],
  }),
  getCategoryPerformanceAnalytics: jest.fn().mockResolvedValue({
    days: 7,
    categories: [
      {
        categoryId: "cat-1",
        categoryName: "Món Nước",
        slug: "mon-nuoc",
        newRecipesCount: 5,
        publishedRecipesCount: 4,
        previousPublishedCount: 2,
        growthRate: 100.0,
        totalViewsInPeriod: 2140,
      },
    ],
    fastestGrowingCategories: [
      {
        categoryId: "cat-1",
        categoryName: "Món Nước",
        slug: "mon-nuoc",
        newRecipesCount: 5,
        publishedRecipesCount: 4,
        previousPublishedCount: 2,
        growthRate: 100.0,
        totalViewsInPeriod: 2140,
      },
    ],
    lowActivityCategories: [],
  }),
}));

describe("Chức năng mở rộng & nâng cao trải nghiệm người dùng (Lê Thị Ánh Nhung)", () => {
  it("1. RecipeRecommendations: Hiển thị tiêu đề và danh sách món ăn gợi ý cá nhân hóa", async () => {
    render(<RecipeRecommendations limit={3} />);

    expect(screen.getByText("Có thể bạn sẽ thích")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Phở Bò Gia Truyền")).toBeInTheDocument();
    });
  });

  it("2. TrendingRecipesView: Hiển thị bảng xếp hạng công thức thịnh hành và chuyển tab thời gian", async () => {
    render(<TrendingRecipesView />);

    expect(screen.getByText("Công thức thịnh hành")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Bún Bò Huế Cay Nồng")).toBeInTheDocument();
      expect(screen.getByText("Gỏi Cuốn Tôm Thịt")).toBeInTheDocument();
    });

    // Rank badges check
    expect(screen.getByText("🥇 #1")).toBeInTheDocument();
    expect(screen.getByText("🥈 #2")).toBeInTheDocument();

    // Tab click
    const tab30d = screen.getByText("⭐ 30 ngày qua");
    fireEvent.click(tab30d);
  });

  it("3. CollectionsPage: Hiển thị danh sách bộ sưu tập cá nhân và mở modal tạo mới", async () => {
    render(<CollectionsPage />);

    expect(screen.getByText("Bộ sưu tập công thức")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Món ngon cuối tuần")).toBeInTheDocument();
    });

    const createBtn = screen.getByText("Tạo bộ sưu tập mới");
    fireEvent.click(createBtn);

    expect(screen.getByPlaceholderText(/Món ăn sáng yêu thích/i)).toBeInTheDocument();
  });

  it("4. AdminActivityAnalyticsPage: Hiển thị số liệu so sánh, biểu đồ theo ngày và bảng xếp hạng tác giả", async () => {
    render(<AdminActivityAnalyticsPage />);

    expect(screen.getByText("Phân tích hoạt động đăng tải (Activity Analytics)")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Lê Thị Ánh Nhung")).toBeInTheDocument();
      expect(screen.getByText("nhung@culinary.vn")).toBeInTheDocument();
    });

    // Check summary growth rates
    expect(screen.getAllByText("+100%").length).toBeGreaterThanOrEqual(1);
  });

  it("5. AdminCategoryPerformancePage: Hiển thị bảng danh mục tăng trưởng và tỷ lệ phần trăm", async () => {
    render(<AdminCategoryPerformancePage />);

    expect(screen.getByText("Hiệu quả danh mục & Tăng trưởng nội dung")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getAllByText("Món Nước").length).toBeGreaterThan(0);
      expect(screen.getByText("2140 lượt")).toBeInTheDocument();
    });
  });
});
