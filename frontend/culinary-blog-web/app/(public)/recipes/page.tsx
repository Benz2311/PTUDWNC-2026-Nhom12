"use client";

import React, { useState, useEffect, useCallback, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { ChefHat, SlidersHorizontal, RotateCcw, Utensils } from "lucide-react";
import { getRecipes } from "@/lib/api/recipes";
import { getCategories } from "@/lib/api/categories";
import type { Category } from "@/types/category";
import type { PagedResult, RecipeFilterParams, RecipeListItem } from "@/types/recipe";
import RecipeCard from "@/components/recipe/RecipeCard";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { ErrorState } from "@/components/common/ErrorState";
import { EmptyState } from "@/components/common/EmptyState";
import { Pagination } from "@/components/common/Pagination";
import { Button } from "@/components/ui/Button";

function RecipesListContent() {
  const searchParams = useSearchParams();
  const initialCategory = searchParams.get("category") || "";
  const initialDifficulty = searchParams.get("difficulty") || "";
  const initialSort = searchParams.get("sort") || "newest";
  const initialPage = parseInt(searchParams.get("page") || "1", 10) || 1;

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

  const [selectedCategory, setSelectedCategory] = useState<string>(initialCategory);
  const [selectedDifficulty, setSelectedDifficulty] = useState<string>(initialDifficulty);
  const [selectedSort, setSelectedSort] = useState<string>(initialSort);
  const [currentPage, setCurrentPage] = useState<number>(initialPage);

  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Load categories
  useEffect(() => {
    getCategories()
      .then((cats) => setCategories(cats))
      .catch(() => {});
  }, []);

  // Fetch recipes
  const loadRecipes = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params: RecipeFilterParams = {
        page: currentPage,
        pageSize: 9,
        categorySlug: selectedCategory || undefined,
        difficulty: selectedDifficulty || undefined,
        sortBy: selectedSort === "popular" ? "publishedAt" : selectedSort === "cookTime" ? "cookTime" : "publishedAt",
        sortDirection: "desc",
      };

      const result = await getRecipes(params);
      setRecipesData(result);
    } catch {
      setError("Không thể tải danh sách công thức. Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  }, [currentPage, selectedCategory, selectedDifficulty, selectedSort]);

  useEffect(() => {
    loadRecipes();
  }, [loadRecipes]);

  const handleResetFilters = () => {
    setSelectedCategory("");
    setSelectedDifficulty("");
    setSelectedSort("newest");
    setCurrentPage(1);
  };

  return (
    <div className="min-h-screen bg-slate-50/60 py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 border-b border-gray-200/80 pb-6">
          <div>
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-100/80 text-emerald-800 text-xs font-semibold mb-2">
              <Utensils className="h-3.5 w-3.5" />
              <span>Khám phá ẩm thực</span>
            </div>
            <h1 className="text-3xl font-extrabold tracking-tight text-gray-900">
              Tất Cả Công Thức Nấu Ăn
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              Duyệt hàng trăm công thức thơm ngon được chia sẻ bởi cộng đồng tác giả Culinary Blog.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <span className="text-xs font-medium text-gray-500">
              Tìm thấy <strong className="text-emerald-700 font-bold">{recipesData.totalCount}</strong> công thức
            </span>
          </div>
        </div>

        {/* Filter bar */}
        <div className="bg-white p-4 rounded-2xl border border-gray-200/70 shadow-xs flex flex-wrap items-center justify-between gap-4">
          <div className="flex flex-wrap items-center gap-3">
            <div className="flex items-center gap-1.5 text-xs font-semibold text-gray-700 mr-1">
              <SlidersHorizontal className="h-4 w-4 text-emerald-600" />
              <span>Bộ lọc:</span>
            </div>

            {/* Category filter */}
            <select
              aria-label="Lọc theo danh mục"
              value={selectedCategory}
              onChange={(e) => {
                setSelectedCategory(e.target.value);
                setCurrentPage(1);
              }}
              className="text-xs rounded-lg border border-gray-300 bg-white px-3 py-2 text-gray-700 focus:outline-none focus:ring-2 focus:ring-emerald-600"
            >
              <option value="">Tất cả danh mục</option>
              {categories.map((cat) => (
                <option key={cat.id} value={cat.slug}>
                  {cat.name}
                </option>
              ))}
            </select>

            {/* Difficulty filter */}
            <select
              aria-label="Lọc theo độ khó"
              value={selectedDifficulty}
              onChange={(e) => {
                setSelectedDifficulty(e.target.value);
                setCurrentPage(1);
              }}
              className="text-xs rounded-lg border border-gray-300 bg-white px-3 py-2 text-gray-700 focus:outline-none focus:ring-2 focus:ring-emerald-600"
            >
              <option value="">Mọi độ khó</option>
              <option value="Easy">Dễ (Easy)</option>
              <option value="Medium">Trung bình (Medium)</option>
              <option value="Hard">Khó (Hard)</option>
              <option value="Expert">Chuyên nghiệp (Expert)</option>
            </select>

            {/* Sort filter */}
            <select
              aria-label="Sắp xếp công thức"
              value={selectedSort}
              onChange={(e) => {
                setSelectedSort(e.target.value);
                setCurrentPage(1);
              }}
              className="text-xs rounded-lg border border-gray-300 bg-white px-3 py-2 text-gray-700 focus:outline-none focus:ring-2 focus:ring-emerald-600"
            >
              <option value="newest">Mới nhất</option>
              <option value="popular">Nổi bật nhất</option>
              <option value="cookTime">Thời gian nấu</option>
            </select>
          </div>

          {(selectedCategory || selectedDifficulty || selectedSort !== "newest") && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleResetFilters}
              className="text-xs flex items-center gap-1.5 text-gray-600 hover:text-emerald-700"
            >
              <RotateCcw className="h-3.5 w-3.5" />
              <span>Đặt lại bộ lọc</span>
            </Button>
          )}
        </div>

        {/* Recipe Grid / States */}
        {loading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {Array.from({ length: 6 }).map((_, idx) => (
              <LoadingSkeleton key={idx} />
            ))}
          </div>
        ) : error ? (
          <ErrorState message={error} onRetry={loadRecipes} />
        ) : recipesData.items.length === 0 ? (
          <EmptyState
            title="Không tìm thấy công thức nào"
            description="Hãy thử thay đổi tiêu chí bộ lọc hoặc khám phá các danh mục khác."
            actionLabel="Xem tất cả công thức"
            onAction={handleResetFilters}
          />
        ) : (
          <div className="space-y-8">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {recipesData.items.map((recipe) => (
                <RecipeCard key={recipe.id} recipe={recipe} />
              ))}
            </div>

            {/* Pagination */}
            {recipesData.totalPages > 1 && (
              <div className="pt-6 border-t border-gray-200">
                <Pagination
                  page={recipesData.page}
                  totalPages={recipesData.totalPages}
                  onPageChange={(p) => setCurrentPage(p)}
                />
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default function RecipesPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen flex items-center justify-center text-sm text-emerald-800">
          <ChefHat className="h-6 w-6 animate-spin text-emerald-600 mr-2" />
          <span>Đang tải danh sách công thức...</span>
        </div>
      }
    >
      <RecipesListContent />
    </Suspense>
  );
}
