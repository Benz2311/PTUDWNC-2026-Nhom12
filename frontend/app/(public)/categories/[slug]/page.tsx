"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useParams, useSearchParams, useRouter } from "next/navigation";
import { ChefHat, ArrowLeft, Layers } from "lucide-react";
import { getCategoryBySlug } from "@/lib/api/categories";
import type { CategoryDetailResponse } from "@/types/category";
import type { RecipeListItem } from "@/types/recipe";
import RecipeCard from "@/components/recipe/RecipeCard";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { ErrorState } from "@/components/common/ErrorState";
import { EmptyState } from "@/components/common/EmptyState";
import { Pagination } from "@/components/common/Pagination";

interface CategoryDetailPageProps {
  params?: Promise<{
    slug: string;
  }>;
}

export default function CategoryDetailPage() {
  const routeParams = useParams<{ slug: string }>();
  const routeSlug = typeof routeParams?.slug === "string" ? decodeURIComponent(routeParams.slug) : "";

  const router = useRouter();
  const searchParams = useSearchParams();
  const initialPage = parseInt(searchParams.get("page") || "1", 10);

  const [data, setData] = useState<CategoryDetailResponse | null>(null);
  const [currentPage, setCurrentPage] = useState<number>(Number.isNaN(initialPage) ? 1 : initialPage);
  const [pageSize] = useState<number>(9);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const loadCategoryData = useCallback(async (slug: string, page: number) => {
    if (!slug) return;
    setLoading(true);
    setError(null);
    try {
      const result = await getCategoryBySlug(slug, page, pageSize);
      if (!result || !result.category) {
        setError(`Danh mục ẩm thực "${slug}" không tồn tại hoặc đã được chuyển sang địa chỉ khác.`);
        setData(null);
      } else {
        setData(result);
      }
    } catch (err) {
      console.error("Error loading category details:", err);
      setError("Không thể tải thông tin danh mục vào lúc này. Vui lòng thử lại sau.");
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [pageSize]);

  useEffect(() => {
    if (routeSlug) {
      loadCategoryData(routeSlug, currentPage);
    }
  }, [routeSlug, currentPage, loadCategoryData]);

  const handlePageChange = (newPage: number) => {
    setCurrentPage(newPage);
    // Update URL query string without reloading page
    const query = new URLSearchParams(searchParams.toString());
    query.set("page", newPage.toString());
    router.replace(`/categories/${encodeURIComponent(routeSlug)}?${query.toString()}`, { scroll: false });

    // Smooth scroll up to recipes grid
    const targetElement = document.getElementById("category-recipes-section");
    if (targetElement) {
      targetElement.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  };

  return (
    <div className="container-page py-8 sm:py-10 space-y-8">
      {/* ── BREADCRUMBS ────────────────────────────────────────────── */}
      <nav aria-label="Breadcrumb" className="text-xs text-gray-500 flex items-center gap-2">
        <Link href="/" className="hover:text-emerald-700 transition-colors">
          Trang chủ
        </Link>
        <span>/</span>
        <Link href="/categories" className="hover:text-emerald-700 transition-colors">
          Danh mục
        </Link>
        <span>/</span>
        <span className="text-gray-900 font-semibold truncate max-w-xs">
          {data?.category?.name || routeSlug}
        </span>
      </nav>

      {/* ── LOADING SKELETON (INITIAL LOAD) ─────────────────────────── */}
      {loading && !data && (
        <div className="space-y-8">
          <LoadingSkeleton variant="detail" />
          <div className="pt-4">
            <LoadingSkeleton variant="grid" count={6} />
          </div>
        </div>
      )}

      {/* ── ERROR STATE / NOT FOUND ────────────────────────────────── */}
      {!loading && error && (
        <div className="py-12 space-y-6 text-center">
          <ErrorState
            title="Không tìm thấy danh mục"
            message={error}
            onRetry={() => loadCategoryData(routeSlug, 1)}
            retryText="Thử tải lại"
          />
          <div>
            <Link href="/categories">
              <Button variant="outline" className="border-emerald-200 text-emerald-800 hover:bg-emerald-50">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Xem tất cả danh mục ẩm thực
              </Button>
            </Link>
          </div>
        </div>
      )}

      {/* ── CATEGORY DETAILS AND RECIPES ───────────────────────────── */}
      {data && data.category && (
        <>
          {/* Category Header Banner */}
          <div className="rounded-3xl culinary-hero-gradient p-8 sm:p-10 text-white shadow-md relative overflow-hidden">
            <div className="max-w-3xl space-y-4 relative z-10">
              <div className="flex flex-wrap items-center gap-2">
                <span className="inline-flex items-center gap-1.5 rounded-full bg-white/10 px-3 py-1 text-xs font-semibold backdrop-blur-xs border border-white/15">
                  <Layers className="h-3.5 w-3.5 text-amber-400" />
                  <span>Danh mục món ăn</span>
                </span>
                <Badge variant="secondary" className="bg-amber-400 text-gray-900 border-none font-bold px-3 py-1">
                  {data.recipes?.totalCount ?? 0} công thức
                </Badge>
              </div>

              <h1 className="text-3xl sm:text-5xl font-extrabold tracking-tight">
                {data.category.name}
              </h1>

              <p className="text-xs sm:text-sm text-emerald-100/90 leading-relaxed max-w-2xl">
                {data.category.description ||
                  `Khám phá bộ sưu tập các món ăn ngon, dễ làm và chuẩn vị thuộc danh mục ${data.category.name} với công thức chi tiết từ các đầu bếp hàng đầu.`}
              </p>

              <div className="pt-2 flex items-center gap-3">
                <Link
                  href="/categories"
                  className="inline-flex items-center gap-1.5 text-xs font-semibold text-emerald-100 hover:text-white underline underline-offset-4"
                >
                  <ArrowLeft className="h-3.5 w-3.5" />
                  <span>Danh sách tất cả danh mục</span>
                </Link>
              </div>
            </div>

            <div className="absolute -bottom-10 -right-10 h-52 w-52 rounded-full bg-white/5 pointer-events-none" />
          </div>

          {/* Recipes Section */}
          <div id="category-recipes-section" className="space-y-6 pt-2">
            <div className="flex flex-wrap items-center justify-between gap-4 pb-4 border-b border-gray-100">
              <div>
                <h2 className="text-lg sm:text-xl font-bold text-gray-900 flex items-center gap-2">
                  <span>Công thức món ăn</span>
                  <span className="text-xs text-emerald-800 bg-emerald-50 px-2.5 py-0.5 rounded-full font-semibold border border-emerald-100">
                    {data.recipes?.totalCount ?? 0} món
                  </span>
                </h2>
                <p className="text-xs text-gray-500 mt-1">
                  Hiển thị các công thức đã xuất bản thuộc danh mục <strong>{data.category.name}</strong>
                  {data.recipes?.totalPages > 1 && ` (Trang ${currentPage}/${data.recipes.totalPages})`}
                </p>
              </div>

              <Link
                href="/recipes"
                className="text-xs font-semibold text-emerald-700 hover:text-emerald-800 hover:underline flex items-center gap-1"
              >
                <span>Xem tất cả công thức</span>
                <span>→</span>
              </Link>
            </div>

            {/* In-page loading indicator during page change */}
            {loading && (
              <div className="py-8">
                <LoadingSkeleton variant="grid" count={6} />
              </div>
            )}

            {/* Recipes Grid */}
            {!loading && data.recipes?.items && data.recipes.items.length > 0 && (
              <div className="grid-recipes">
                {data.recipes.items.map((recipe) => {
                  const mappedRecipe: RecipeListItem = {
                    id: recipe.id,
                    title: recipe.title,
                    slug: recipe.slug,
                    description: recipe.description || "",
                    cookTime: recipe.cookTime,
                    prepTime: recipe.prepTime,
                    prepTimeMinutes: recipe.prepTime,
                    cookTimeMinutes: recipe.cookTime,
                    servings: recipe.servings ?? 2,
                    difficulty: recipe.difficulty ?? "Medium",
                    status: recipe.status ?? "Published",
                    publishedAt: recipe.publishedAt ?? null,
                    primaryImageUrl: recipe.primaryImageUrl,
                    category: {
                      id: data.category.id,
                      name: data.category.name,
                      slug: data.category.slug,
                      description: data.category.description,
                    },
                  };

                  return (
                    <RecipeCard
                      key={recipe.id}
                      recipe={mappedRecipe}
                    />
                  );
                })}
              </div>
            )}

            {/* Empty State when no recipes exist in category */}
            {!loading && (!data.recipes?.items || data.recipes.items.length === 0) && (
              <EmptyState
                icon={<ChefHat className="h-8 w-8" />}
                title="Chưa có công thức nào trong danh mục này"
                description={`Các đầu bếp đang chuẩn bị những công thức ngon nhất cho danh mục "${data.category.name}". Bạn hãy quay lại sau nhé!`}
                actionLabel="Khám phá các danh mục khác"
                actionHref="/categories"
              />
            )}

            {/* Shared Pagination Component */}
            {!loading && data.recipes && data.recipes.totalPages > 1 && (
              <div className="mt-10 border-t border-gray-100 pt-6">
                <Pagination
                  page={currentPage}
                  totalPages={data.recipes.totalPages}
                  onPageChange={handlePageChange}
                />
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
