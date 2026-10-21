"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { Search, ChefHat, Sparkles } from "lucide-react";
import { getCategories } from "@/lib/api/categories";
import { getRecipes } from "@/lib/api/recipes";
import { getStoredToken } from "@/lib/api/client";
import type { Category } from "@/types/category";
import type { PagedResult, RecipeFilterParams, RecipeListItem } from "@/types/recipe";
import RecipeCard from "@/components/recipe/RecipeCard";
import RecipeFilter from "@/components/recipe/RecipeFilter";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { ErrorState } from "@/components/common/ErrorState";
import { EmptyState } from "@/components/common/EmptyState";
import { Pagination } from "@/components/common/Pagination";
import { Button } from "@/components/ui/Button";

export default function HomePage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [recipesData, setRecipesData] = useState<PagedResult<RecipeListItem>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 9,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
  });
  const [filters, setFilters] = useState<RecipeFilterParams>({
    page: 1,
    pageSize: 9,
    sortBy: "publishedAt",
    sortDirection: "desc",
  });
  const [searchInput, setSearchInput] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);

  // Load categories and detect role
  useEffect(() => {
    getCategories()
      .then((data) => setCategories(data))
      .catch(() => setCategories([]));

    const token = getStoredToken();
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split(".")[1]));
        if (
          payload.role?.includes("Admin") ||
          payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] === "Admin"
        ) {
          setIsAdmin(true);
        }
      } catch {
        setIsAdmin(false);
      }
    }
  }, []);

  // Fetch recipes whenever filters change
  useEffect(() => {
    setLoading(true);
    setError(null);
    getRecipes(filters)
      .then((res) => {
        setRecipesData(res);
        setLoading(false);
      })
      .catch((err) => {
        setError("Không thể tải danh sách công thức. Vui lòng thử lại sau.");
        setLoading(false);
        console.error(err);
      });
  }, [filters]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFilters((prev) => ({
      ...prev,
      search: searchInput.trim() || undefined,
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
    });
  };

  const handlePageChange = (newPage: number) => {
    setFilters((prev) => ({ ...prev, page: newPage }));
    window.scrollTo({ top: 500, behavior: "smooth" });
  };

  return (
    <div className="min-h-screen">
      {/* ── HERO BANNER ─────────────────────────────────────────────── */}
      <section className="relative overflow-hidden culinary-hero-gradient text-white py-16 px-4 sm:px-6 lg:px-8">
        <div className="absolute inset-0 opacity-10 bg-[radial-gradient(#fff_1px,transparent_1px)] [background-size:16px_16px]" />
        <div className="relative mx-auto max-w-4xl text-center space-y-6">
          <div className="inline-flex items-center gap-2 rounded-full bg-white/10 px-4 py-1.5 text-xs font-semibold text-emerald-100 backdrop-blur-md border border-white/15">
            <Sparkles className="h-4 w-4 text-amber-400" />
            <span>Khám phá ẩm thực • Học nấu ăn • Kết nối cộng đồng</span>
          </div>

          <h1 className="text-3xl sm:text-5xl font-extrabold tracking-tight leading-tight">
            Khám phá thế giới ẩm thực tuyệt vời
          </h1>

          <p className="mx-auto max-w-2xl text-sm sm:text-base text-emerald-100/90 leading-relaxed">
            Hàng ngàn công thức nấu ăn ngon, chuẩn vị và dễ dàng thực hiện tại nhà được chia sẻ bởi cộng đồng đầu bếp thực thụ.
          </p>

          {/* Search bar inside Hero */}
          <form onSubmit={handleSearchSubmit} className="mx-auto max-w-xl">
            <div className="flex items-center rounded-2xl bg-white p-1.5 shadow-xl">
              <div className="pl-3 text-gray-400">
                <Search className="h-5 w-5" />
              </div>
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="Tìm công thức, món ăn, nguyên liệu..."
                className="w-full bg-transparent px-3 text-sm text-gray-800 placeholder-gray-400 outline-none"
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
        </div>
      </section>

      {/* ── FEATURED CATEGORIES ROW ─────────────────────────────────── */}
      <section className="bg-white border-b border-gray-100 py-6">
        <div className="container-page">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <ChefHat className="h-5 w-5 text-emerald-700" />
              <h2 className="text-base font-bold text-gray-900">Danh mục nổi bật</h2>
            </div>
            <Link href="/categories" className="text-xs font-semibold text-emerald-700 hover:underline">
              Xem tất cả ({categories.length}) →
            </Link>
          </div>

          <div className="flex gap-3 overflow-x-auto pb-2 no-scrollbar">
            <button
              onClick={() => setFilters((prev) => ({ ...prev, categorySlug: undefined, page: 1 }))}
              className={`shrink-0 flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-semibold border transition-all cursor-pointer ${
                !filters.categorySlug
                  ? "bg-emerald-800 text-white border-emerald-800 shadow-xs"
                  : "bg-gray-50 text-gray-700 border-gray-200 hover:bg-emerald-50 hover:border-emerald-200"
              }`}
            >
              <span>🍽️</span>
              <span>Tất cả món</span>
            </button>

            {categories.slice(0, 10).map((cat) => {
              const isSelected = filters.categorySlug === cat.slug;
              return (
                <button
                  key={cat.id}
                  onClick={() => setFilters((prev) => ({ ...prev, categorySlug: cat.slug, page: 1 }))}
                  className={`shrink-0 flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-semibold border transition-all cursor-pointer ${
                    isSelected
                      ? "bg-emerald-800 text-white border-emerald-800 shadow-xs"
                      : "bg-white text-gray-700 border-gray-200 hover:bg-emerald-50 hover:border-emerald-200"
                  }`}
                >
                  <span className="h-6 w-6 rounded-full bg-emerald-100 text-emerald-800 flex items-center justify-center text-xs font-bold">
                    {cat.name ? cat.name[0] : "C"}
                  </span>
                  <span>{cat.name}</span>
                  {cat.recipeCount !== undefined && (
                    <span
                      className={`text-[10px] px-1.5 py-0.5 rounded-full font-bold ${
                        isSelected ? "bg-white/20 text-white" : "bg-gray-100 text-gray-600"
                      }`}
                    >
                      {cat.recipeCount}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </div>
      </section>

      {/* ── MAIN RECIPES SECTION WITH FILTER SIDEBAR ─────────────────── */}
      <section className="container-page py-10">
        <div className="flex flex-col lg:flex-row gap-8 items-start">
          {/* Left Sidebar Filter */}
          <RecipeFilter
            categories={categories}
            filters={filters}
            onFilterChange={(newFilters: RecipeFilterParams) => setFilters(newFilters)}
            onReset={handleResetFilters}
            isAdmin={isAdmin}
          />

          {/* Right Main Grid Area */}
          <div className="flex-1 w-full min-w-0">
            {/* Header info bar */}
            <div className="flex flex-wrap items-center justify-between gap-4 pb-4 mb-6 border-b border-gray-100">
              <div>
                <h2 className="text-xl font-bold text-gray-900 flex items-center gap-2">
                  <span>Tất cả công thức</span>
                  <span className="rounded-full bg-emerald-100 text-emerald-800 px-2.5 py-0.5 text-xs font-semibold">
                    {recipesData.totalCount} món
                  </span>
                </h2>
                {filters.categorySlug && (
                  <p className="text-xs text-gray-500 mt-1">
                    Đang lọc theo danh mục: <strong className="text-emerald-700">{filters.categorySlug}</strong>
                  </p>
                )}
                {filters.search && (
                  <p className="text-xs text-gray-500 mt-1">
                    Từ khóa tìm kiếm: &ldquo;<strong>{filters.search}</strong>&rdquo;
                  </p>
                )}
              </div>

              {/* Active filters pill list */}
              {(filters.categorySlug || filters.difficulty || filters.maxCookTimeMinutes || filters.mine || filters.search) && (
                <button
                  onClick={handleResetFilters}
                  className="text-xs text-rose-600 hover:text-rose-800 font-medium underline cursor-pointer"
                >
                  Xóa tất cả bộ lọc ✕
                </button>
              )}
            </div>

            {/* Error state */}
            {error && (
              <ErrorState
                title="Không thể tải công thức"
                message={error}
                onRetry={() => setFilters({ ...filters })}
              />
            )}

            {/* Loading state skeleton */}
            {loading && !error && (
              <LoadingSkeleton variant="grid" count={6} />
            )}

            {/* Empty state */}
            {!loading && !error && recipesData.items.length === 0 && (
              <EmptyState
                title="Không tìm thấy công thức phù hợp"
                description="Hãy thử điều chỉnh bộ lọc, xóa từ khóa tìm kiếm hoặc chọn danh mục ẩm thực khác."
                actionLabel="Đặt lại bộ lọc"
                onAction={handleResetFilters}
              />
            )}

            {/* Recipes Grid */}
            {!loading && !error && recipesData.items.length > 0 && (
              <div className="grid-recipes">
                {recipesData.items.map((recipe) => (
                  <RecipeCard key={recipe.id} recipe={recipe} showStatus={!!filters.mine} />
                ))}
              </div>
            )}

            {/* Pagination Component */}
            {!loading && !error && recipesData.totalPages > 1 && (
              <div className="mt-10 border-t border-gray-100 pt-4">
                <Pagination
                  page={recipesData.page}
                  totalPages={recipesData.totalPages}
                  onPageChange={handlePageChange}
                />
              </div>
            )}
          </div>
        </div>
      </section>
    </div>
  );
}
