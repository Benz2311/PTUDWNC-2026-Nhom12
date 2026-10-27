"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  Search,
  ChefHat,
  Sparkles,
  ArrowRight,
  Layers,
  RotateCcw,
} from "lucide-react";
import { getCategories } from "@/lib/api/categories";
import { getRecipes } from "@/lib/api/recipes";
import type { Category } from "@/types/category";
import type { PagedResult, RecipeFilterParams, RecipeListItem } from "@/types/recipe";
import RecipeCard from "@/components/recipe/RecipeCard";
import { PublicLayout } from "@/components/layout/PublicLayout";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { ErrorState } from "@/components/common/ErrorState";
import { EmptyState } from "@/components/common/EmptyState";
import { Pagination } from "@/components/common/Pagination";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Card } from "@/components/ui/Card";

export default function HomePage() {
  const router = useRouter();

  // Category state (from real API)
  const [categories, setCategories] = useState<Category[]>([]);
  const [loadingCategories, setLoadingCategories] = useState<boolean>(true);
  const [categoriesError, setCategoriesError] = useState<string | null>(null);

  // Recipes state (from real API)
  const [recipesData, setRecipesData] = useState<PagedResult<RecipeListItem>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 9,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
  });

  // Filter & sort state
  const [filters, setFilters] = useState<RecipeFilterParams>({
    page: 1,
    pageSize: 9,
    sortBy: "publishedAt",
    sortDirection: "desc",
  });

  const [searchInput, setSearchInput] = useState<string>("");
  const [loadingRecipes, setLoadingRecipes] = useState<boolean>(true);
  const [recipesError, setRecipesError] = useState<string | null>(null);

  // 1. Fetch categories from API
  const fetchCategoriesList = useCallback(async () => {
    setLoadingCategories(true);
    setCategoriesError(null);
    try {
      const data = await getCategories();
      setCategories(data);
    } catch (err) {
      console.error("Error loading categories for homepage:", err);
      setCategoriesError("Không thể tải danh sách danh mục");
      setCategories([]);
    } finally {
      setLoadingCategories(false);
    }
  }, []);

  useEffect(() => {
    fetchCategoriesList();
  }, [fetchCategoriesList]);

  // 2. Fetch recipes from API whenever filters change
  const fetchRecipesList = useCallback(async () => {
    setLoadingRecipes(true);
    setRecipesError(null);
    try {
      const res = await getRecipes(filters);
      setRecipesData(res);
    } catch (err) {
      console.error("Error loading recipes for homepage:", err);
      setRecipesError("Không thể tải danh sách công thức vào lúc này. Vui lòng thử lại sau.");
    } finally {
      setLoadingRecipes(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchRecipesList();
  }, [fetchRecipesList]);

  // 3. Search submission: navigates to /search?q=... (SRS Search route)
  const handleHeroSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchInput.trim()) {
      router.push(`/search?q=${encodeURIComponent(searchInput.trim())}`);
    }
  };

  // 4. In-page filter handlers
  const handleSortChange = (
    sortBy: RecipeFilterParams["sortBy"],
    sortDirection: "asc" | "desc" = "desc"
  ) => {
    setFilters((prev) => ({
      ...prev,
      sortBy,
      sortDirection,
      page: 1,
    }));
  };

  const handlePageSizeChange = (newSize: number) => {
    setFilters((prev) => ({
      ...prev,
      pageSize: newSize,
      page: 1,
    }));
  };

  const handleResetFilters = () => {
    setSearchInput("");
    setFilters({
      page: 1,
      pageSize: 9,
      sortBy: "publishedAt",
      sortDirection: "desc",
      categorySlug: undefined,
    });
  };

  const handlePageChange = (newPage: number) => {
    setFilters((prev) => ({ ...prev, page: newPage }));
    const section = document.getElementById("recipes-main-content");
    if (section) {
      section.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  };

  // Convert categories to format expected by PublicSidebar
  const sidebarCategories = categories.map((c) => ({
    id: c.id,
    name: c.name,
    slug: c.slug,
    recipeCount: typeof c.recipeCount === "number" ? c.recipeCount : 0,
    imageUrl: c.imageUrl,
  }));

  // Top featured categories for the quick highlights row
  const featuredCategories = categories.slice(0, 6);

  return (
    <div className="min-h-screen overflow-x-hidden">
      {/* ── 1. HERO BANNER ─────────────────────────────────────────── */}
      <section className="relative overflow-hidden culinary-hero-gradient text-white py-14 sm:py-20 px-4 sm:px-6 lg:px-8">
        <div className="absolute inset-0 opacity-10 bg-[radial-gradient(#fff_1px,transparent_1px)] [background-size:16px_16px] pointer-events-none" />
        
        <div className="relative mx-auto max-w-4xl text-center space-y-6">
          <div className="inline-flex items-center gap-2 rounded-full bg-white/10 px-4 py-1.5 text-xs font-semibold text-emerald-100 backdrop-blur-md border border-white/15">
            <Sparkles className="h-4 w-4 text-amber-400" />
            <span>Nền tảng chia sẻ và học nấu ăn trực tuyến • Nhóm 12</span>
          </div>

          <h1 className="text-3xl sm:text-5xl lg:text-6xl font-extrabold tracking-tight leading-tight">
            Khám phá thế giới ẩm thực tuyệt vời
          </h1>

          <p className="mx-auto max-w-2xl text-xs sm:text-base text-emerald-100/90 leading-relaxed">
            Hàng ngàn công thức nấu ăn ngon, chuẩn vị và dễ dàng thực hiện tại nhà được chia sẻ bởi cộng đồng đầu bếp và những người yêu ẩm thực.
          </p>

          {/* Search bar inside Hero that connects to /search */}
          <form onSubmit={handleHeroSearchSubmit} className="mx-auto max-w-xl pt-2">
            <div className="flex items-center rounded-2xl bg-white p-1.5 shadow-2xl transition-all focus-within:ring-4 focus-within:ring-emerald-500/30">
              <div className="pl-3 text-gray-400">
                <Search className="h-5 w-5" />
              </div>
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="Tìm món ăn, nguyên liệu (vd: phở bò, bún chả, pasta)..."
                className="w-full bg-transparent px-3 text-xs sm:text-sm text-gray-800 placeholder-gray-400 outline-none"
              />
              <Button
                type="submit"
                variant="default"
                className="rounded-xl px-5 py-2.5 text-xs font-bold"
              >
                Tìm kiếm
              </Button>
            </div>
          </form>

          {/* Quick stats highlight */}
          <div className="pt-2 flex flex-wrap items-center justify-center gap-6 text-xs text-emerald-100">
            <span className="flex items-center gap-1.5">
              <ChefHat className="h-4 w-4 text-amber-400" />
              <span>Hơn {recipesData.totalCount > 0 ? recipesData.totalCount : 100}+ công thức</span>
            </span>
            <span className="flex items-center gap-1.5">
              <Layers className="h-4 w-4 text-amber-400" />
              <span>{categories.length > 0 ? categories.length : 6} danh mục ẩm thực</span>
            </span>
            <span className="flex items-center gap-1.5">
              <Sparkles className="h-4 w-4 text-amber-400" />
              <span>100% hướng dẫn miễn phí</span>
            </span>
          </div>
        </div>
      </section>

      {/* ── 2. FEATURED CATEGORIES SECTION ─────────────────────────── */}
      <section className="bg-white border-b border-gray-100 py-8 px-4 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-7xl">
          <div className="flex items-center justify-between mb-5">
            <div className="flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700">
                <Layers className="h-4 w-4" />
              </div>
              <div>
                <h2 className="text-base sm:text-lg font-bold text-gray-900">
                  Danh mục món ăn nổi bật
                </h2>
                <p className="text-xs text-gray-500 hidden sm:block">
                  Khám phá các nền ẩm thực đặc sắc từ truyền thống đến hiện đại
                </p>
              </div>
            </div>
            <Link
              href="/categories"
              className="inline-flex items-center gap-1 text-xs font-bold text-emerald-700 hover:text-emerald-800 hover:underline"
            >
              <span>Xem tất cả ({categories.length})</span>
              <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </div>

          {/* Featured categories row */}
          {loadingCategories ? (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-3">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="h-20 rounded-xl bg-gray-100 animate-pulse" />
              ))}
            </div>
          ) : featuredCategories.length > 0 ? (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-3">
              {featuredCategories.map((cat) => {
                const isFiltered = filters.categorySlug === cat.slug;
                return (
                  <Link
                    key={cat.id}
                    href={`/categories/${cat.slug}`}
                    className="group block"
                    aria-label={`Khám phá danh mục ${cat.name}`}
                  >
                    <Card
                      className={`p-3.5 h-full flex flex-col justify-between items-center text-center transition-all culinary-card-hover border rounded-2xl ${
                        isFiltered
                          ? "border-emerald-700 bg-emerald-50/50 shadow-xs"
                          : "border-gray-100 bg-white hover:border-emerald-200"
                      }`}
                    >
                      <div className="h-10 w-10 rounded-full bg-emerald-100 text-emerald-800 flex items-center justify-center font-extrabold text-sm mb-2 group-hover:bg-emerald-700 group-hover:text-white transition-colors">
                        {cat.name ? cat.name[0] : "C"}
                      </div>
                      <h3 className="text-xs font-bold text-gray-900 line-clamp-1 group-hover:text-emerald-800 transition-colors">
                        {cat.name}
                      </h3>
                      {typeof cat.recipeCount === "number" && (
                        <span className="mt-1 text-[11px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-100">
                          {cat.recipeCount} món
                        </span>
                      )}
                    </Card>
                  </Link>
                );
              })}
            </div>
          ) : (
            <p className="text-xs text-gray-500 py-3 text-center">
              Chưa có danh mục nào được xuất bản.
            </p>
          )}
        </div>
      </section>

      {/* ── 3. PUBLIC LAYOUT: SIDEBAR + MAIN CONTENT ────────────────── */}
      <PublicLayout
        categories={sidebarCategories}
        activeCategorySlug={filters.categorySlug}
        isLoadingCategories={loadingCategories}
        categoriesError={categoriesError}
        onRetryCategories={fetchCategoriesList}
        sidebarPosition="left"
        className="py-10"
      >
        <div id="recipes-main-content" className="space-y-6">
          {/* Header & Controls bar */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-gray-100">
            <div>
              <div className="flex items-center gap-2">
                <ChefHat className="h-5 w-5 text-emerald-700" />
                <h2 className="text-lg sm:text-xl font-bold text-gray-900">
                  Công thức món ăn mới nhất
                </h2>
                <Badge variant="secondary" className="bg-emerald-100 text-emerald-800 font-bold">
                  {recipesData.totalCount} món
                </Badge>
              </div>
              <p className="text-xs text-gray-500 mt-1">
                {filters.categorySlug ? (
                  <span>
                    Đang lọc theo danh mục: <strong className="text-emerald-800">{filters.categorySlug}</strong>
                  </span>
                ) : (
                  <span>Tuyển chọn những món ăn ngon, dễ làm được cập nhật liên tục</span>
                )}
              </p>
            </div>

            {/* Sorting & page size controls */}
            <div className="flex flex-wrap items-center gap-2">
              <div className="inline-flex items-center gap-1 rounded-xl bg-gray-100 p-1 text-xs">
                <button
                  type="button"
                  onClick={() => handleSortChange("publishedAt", "desc")}
                  className={`px-3 py-1.5 rounded-lg font-semibold transition-all cursor-pointer ${
                    filters.sortBy === "publishedAt"
                      ? "bg-white text-emerald-800 shadow-xs"
                      : "text-gray-600 hover:text-gray-900"
                  }`}
                >
                  Mới nhất
                </button>
                <button
                  type="button"
                  onClick={() => handleSortChange("cookTime", "asc")}
                  className={`px-3 py-1.5 rounded-lg font-semibold transition-all cursor-pointer ${
                    filters.sortBy === "cookTime"
                      ? "bg-white text-emerald-800 shadow-xs"
                      : "text-gray-600 hover:text-gray-900"
                  }`}
                >
                  Nấu nhanh
                </button>
                <button
                  type="button"
                  onClick={() => handleSortChange("prepTime", "asc")}
                  className={`px-3 py-1.5 rounded-lg font-semibold transition-all cursor-pointer ${
                    filters.sortBy === "prepTime"
                      ? "bg-white text-emerald-800 shadow-xs"
                      : "text-gray-600 hover:text-gray-900"
                  }`}
                >
                  Chuẩn bị nhanh
                </button>
              </div>

              {/* Page size selector */}
              <select
                aria-label="Số công thức mỗi trang"
                value={filters.pageSize || 9}
                onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                className="h-8 rounded-xl border border-gray-200 bg-white px-2.5 text-xs font-semibold text-gray-700 outline-none cursor-pointer hover:border-emerald-500 focus:border-emerald-600 focus:ring-1 focus:ring-emerald-600 shadow-xs"
              >
                <option value={6}>6 món / trang</option>
                <option value={9}>9 món / trang</option>
                <option value={12}>12 món / trang</option>
              </select>

              {/* Reset filter button if category filter is active */}
              {filters.categorySlug && (
                <button
                  onClick={handleResetFilters}
                  className="inline-flex items-center gap-1 text-xs text-rose-600 hover:text-rose-800 font-semibold px-2 py-1 rounded-lg hover:bg-rose-50 transition-colors cursor-pointer"
                  title="Xóa bộ lọc"
                >
                  <RotateCcw className="h-3 w-3" />
                  <span>Xóa lọc</span>
                </button>
              )}
            </div>
          </div>

          {/* Loading State Skeleton */}
          {loadingRecipes && (
            <div className="py-2">
              <LoadingSkeleton variant="grid" count={filters.pageSize || 9} />
            </div>
          )}

          {/* Error State with Retry */}
          {!loadingRecipes && recipesError && (
            <ErrorState
              title="Không thể tải danh sách công thức"
              message={recipesError}
              onRetry={fetchRecipesList}
              retryText="Thử tải lại"
            />
          )}

          {/* Empty State */}
          {!loadingRecipes && !recipesError && recipesData.items.length === 0 && (
            <EmptyState
              icon={<ChefHat className="h-8 w-8" />}
              title="Chưa có công thức nào phù hợp"
              description="Hiện tại chưa có công thức nào được tìm thấy. Hãy thử chọn danh mục khác hoặc đặt lại bộ lọc."
              actionLabel="Xem tất cả công thức"
              onAction={handleResetFilters}
            />
          )}

          {/* Main Recipe Cards Grid */}
          {!loadingRecipes && !recipesError && recipesData.items.length > 0 && (
            <div className="grid-recipes">
              {recipesData.items.map((recipe) => (
                <RecipeCard key={recipe.id} recipe={recipe} />
              ))}
            </div>
          )}

          {/* Shared Pagination Component */}
          {!loadingRecipes && !recipesError && recipesData.totalPages > 1 && (
            <div className="mt-10 border-t border-gray-100 pt-6">
              <Pagination
                page={recipesData.page}
                totalPages={recipesData.totalPages}
                onPageChange={handlePageChange}
              />
            </div>
          )}
        </div>
      </PublicLayout>
    </div>
  );
}
