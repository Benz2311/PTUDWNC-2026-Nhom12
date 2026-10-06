"use client";

import { useState, useEffect, useTransition } from "react";
import Link from "next/link";
import { getCategories } from "@/lib/api/categories";
import { getRecipes } from "@/lib/api/recipes";
import { getStoredToken } from "@/lib/api/client";
import type { Category } from "@/types/category";
import type { PagedResult, RecipeFilterParams, RecipeListItem } from "@/types/recipe";
import RecipeCard from "@/components/recipe/RecipeCard";
import RecipeFilter from "@/components/recipe/RecipeFilter";

export default function HomePage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [recipesData, setRecipesData] = useState<PagedResult<RecipeListItem>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 9,
    totalPages: 1,
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
  const [isPending, startTransition] = useTransition();

  // Load categories and detect role
  useEffect(() => {
    getCategories().then((data) => setCategories(data));

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

  return (
    <div className="min-h-screen">
      {/* ── HERO BANNER (Screen 0 & 4 in Design) ───────────────────────── */}
      <section className="relative overflow-hidden bg-gradient-to-br from-[#0d5c3a] via-[#0b4d30] to-[#083a24] text-white py-16 px-4 sm:px-6 lg:px-8">
        <div className="absolute inset-0 opacity-10 bg-[radial-gradient(#fff_1px,transparent_1px)] [background-size:16px_16px]" />
        <div className="relative mx-auto max-w-4xl text-center space-y-6">
          <div className="inline-flex items-center gap-2 rounded-full bg-white/10 px-3.5 py-1 text-xs font-semibold text-emerald-100 backdrop-blur-xs border border-white/15">
            <span className="flex h-2 w-2 rounded-full bg-amber-400" />
            Khám phá ẩm thực • Học nấu ăn • Kết nối cộng đồng
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
                <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </div>
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="Tìm công thức, món ăn, nguyên liệu..."
                className="w-full bg-transparent px-3 text-sm text-gray-800 placeholder-gray-400 outline-none"
              />
              <button
                type="submit"
                className="rounded-xl bg-[#0d5c3a] hover:bg-[#094229] px-5 py-2.5 text-xs font-bold text-white transition-colors shadow-xs cursor-pointer"
              >
                Tìm kiếm
              </button>
            </div>
          </form>
        </div>
      </section>

      {/* ── FEATURED CATEGORIES ROW ────────────────────────────────────── */}
      <section className="bg-white border-b border-gray-100 py-6">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <span className="text-xl">🍲</span>
              <h2 className="text-base font-bold text-gray-900">Danh mục nổi bật</h2>
            </div>
            <Link href="/categories" className="text-xs font-semibold text-[#0d5c3a] hover:underline">
              Xem tất cả ({categories.length}) →
            </Link>
          </div>

          <div className="flex gap-3 overflow-x-auto pb-2 no-scrollbar">
            <button
              onClick={() => setFilters((prev) => ({ ...prev, categorySlug: undefined, page: 1 }))}
              className={`shrink-0 flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-semibold border transition-all cursor-pointer ${
                !filters.categorySlug
                  ? "bg-[#0d5c3a] text-white border-[#0d5c3a] shadow-xs"
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
                      ? "bg-[#0d5c3a] text-white border-[#0d5c3a] shadow-xs"
                      : "bg-white text-gray-700 border-gray-200 hover:bg-emerald-50 hover:border-emerald-200"
                  }`}
                >
                  <span className="h-6 w-6 rounded-full bg-emerald-100 text-[#0d5c3a] flex items-center justify-center text-xs font-bold">
                    {cat.name[0]}
                  </span>
                  <span>{cat.name}</span>
                  {cat.recipeCount !== undefined && (
                    <span className={`text-[10px] px-1.5 py-0.2 rounded-full font-bold ${
                      isSelected ? "bg-white/20 text-white" : "bg-gray-100 text-gray-600"
                    }`}>
                      {cat.recipeCount}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </div>
      </section>

      {/* ── MAIN RECIPES SECTION WITH FILTER SIDEBAR (Screen 5) ────────── */}
      <section className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-10">
        <div className="flex flex-col lg:flex-row gap-8 items-start">
          {/* Left Sidebar Filter */}
          <RecipeFilter
            categories={categories}
            filters={filters}
            onFilterChange={(newFilters) => setFilters(newFilters)}
            onReset={handleResetFilters}
            isAdmin={isAdmin}
          />

          {/* Right Main Grid Area */}
          <main className="flex-1 w-full">
            {/* Header info bar */}
            <div className="flex flex-wrap items-center justify-between gap-4 pb-4 mb-6 border-b border-gray-100">
              <div>
                <h2 className="text-xl font-bold text-gray-900 flex items-center gap-2">
                  <span>Tất cả công thức</span>
                  <span className="rounded-full bg-emerald-100 text-[#0d5c3a] px-2.5 py-0.5 text-xs font-semibold">
                    {recipesData.totalCount} món
                  </span>
                </h2>
                {filters.categorySlug && (
                  <p className="text-xs text-gray-500 mt-1">
                    Đang lọc theo danh mục: <strong className="text-[#0d5c3a]">{filters.categorySlug}</strong>
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
              <div className="rounded-2xl bg-rose-50 border border-rose-200 p-6 text-center text-rose-700 space-y-3 mb-6">
                <p className="text-sm font-medium">{error}</p>
                <button
                  onClick={() => setFilters({ ...filters })}
                  className="px-4 py-2 bg-rose-600 text-white rounded-lg text-xs font-semibold hover:bg-rose-700"
                >
                  Tải lại dữ liệu
                </button>
              </div>
            )}

            {/* Loading state skeleton */}
            {loading ? (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
                {[...Array(6)].map((_, i) => (
                  <div key={i} className="animate-pulse rounded-2xl bg-white border border-gray-100 p-4 space-y-4">
                    <div className="aspect-4/3 w-full bg-gray-200 rounded-xl" />
                    <div className="h-4 bg-gray-200 rounded w-3/4" />
                    <div className="h-3 bg-gray-200 rounded w-1/2" />
                    <div className="h-3 bg-gray-100 rounded w-full" />
                  </div>
                ))}
              </div>
            ) : recipesData.items.length === 0 ? (
              /* Empty state */
              <div className="rounded-2xl bg-white border border-dashed border-gray-200 p-12 text-center space-y-4">
                <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50 text-2xl text-[#0d5c3a]">
                  🍳
                </div>
                <h3 className="text-base font-bold text-gray-900">Không tìm thấy công thức phù hợp</h3>
                <p className="mx-auto max-w-sm text-xs text-gray-500">
                  Hãy thử điều chỉnh bộ lọc, xóa từ khóa tìm kiếm hoặc chọn danh mục khác.
                </p>
                <button
                  onClick={handleResetFilters}
                  className="inline-flex items-center rounded-xl bg-[#0d5c3a] px-4 py-2 text-xs font-semibold text-white hover:bg-[#094229] transition-colors"
                >
                  Đặt lại bộ lọc
                </button>
              </div>
            ) : (
              /* Recipes Grid */
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
                {recipesData.items.map((recipe) => (
                  <RecipeCard key={recipe.id} recipe={recipe} showStatus={!!filters.mine} />
                ))}
              </div>
            )}

            {/* Pagination Controls */}
            {recipesData.totalPages > 1 && (
              <div className="mt-10 flex flex-wrap items-center justify-between gap-4 border-t border-gray-100 pt-6">
                <p className="text-xs text-gray-500">
                  Hiển thị trang <strong className="font-semibold text-gray-900">{recipesData.page}</strong> / {recipesData.totalPages} ({recipesData.totalCount} công thức)
                </p>

                <div className="flex items-center gap-1.5">
                  <button
                    disabled={recipesData.page <= 1}
                    onClick={() => setFilters((prev) => ({ ...prev, page: Math.max(1, (prev.page || 1) - 1) }))}
                    className="flex h-9 items-center gap-1 rounded-lg border border-gray-200 px-3 text-xs font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
                  >
                    ← Trước
                  </button>

                  {[...Array(recipesData.totalPages)].map((_, i) => {
                    const pageNum = i + 1;
                    // Only show first, last, and around current
                    if (
                      pageNum === 1 ||
                      pageNum === recipesData.totalPages ||
                      Math.abs(pageNum - recipesData.page) <= 1
                    ) {
                      const isCurrent = pageNum === recipesData.page;
                      return (
                        <button
                          key={pageNum}
                          onClick={() => setFilters((prev) => ({ ...prev, page: pageNum }))}
                          className={`h-9 w-9 rounded-lg text-xs font-bold transition-colors ${
                            isCurrent
                              ? "bg-[#0d5c3a] text-white shadow-xs"
                              : "border border-gray-200 text-gray-700 hover:bg-gray-50"
                          }`}
                        >
                          {pageNum}
                        </button>
                      );
                    } else if (
                      pageNum === recipesData.page - 2 ||
                      pageNum === recipesData.page + 2
                    ) {
                      return <span key={pageNum} className="px-1 text-gray-400 text-xs">...</span>;
                    }
                    return null;
                  })}

                  <button
                    disabled={recipesData.page >= recipesData.totalPages}
                    onClick={() => setFilters((prev) => ({ ...prev, page: Math.min(recipesData.totalPages, (prev.page || 1) + 1) }))}
                    className="flex h-9 items-center gap-1 rounded-lg border border-gray-200 px-3 text-xs font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
                  >
                    Sau →
                  </button>
                </div>
              </div>
            )}
          </main>
        </div>
      </section>
    </div>
  );
}
