"use client";

import React, { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { Layers, ArrowRight, Utensils, ChefHat } from "lucide-react";
import { getCategories } from "@/lib/api/categories";
import type { Category } from "@/types/category";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { ErrorState } from "@/components/common/ErrorState";
import { EmptyState } from "@/components/common/EmptyState";

// Default culinary image fallbacks based on category slug keywords
const categoryFallbackImages: Record<string, string> = {
  "mon-viet": "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80",
  "mon-a": "https://images.unsplash.com/photo-1541696432-82c6da8ce7bf?auto=format&fit=crop&w=800&q=80",
  "mon-au": "https://images.unsplash.com/photo-1551183053-bf91a1d81141?auto=format&fit=crop&w=800&q=80",
  "trang-mieng": "https://images.unsplash.com/photo-1587314168485-3236d6710814?auto=format&fit=crop&w=800&q=80",
  "do-uong": "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80",
  "an-chay": "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
  default: "https://images.unsplash.com/photo-1498837167922-ddd27525d352?auto=format&fit=crop&w=800&q=80",
};

function getCategoryImageUrl(category: Category): string {
  if (category.imageUrl && category.imageUrl.trim() !== "") {
    return category.imageUrl;
  }
  const slug = category.slug?.toLowerCase() || "";
  const matchedKey = Object.keys(categoryFallbackImages).find((key) => slug.includes(key));
  return matchedKey ? categoryFallbackImages[matchedKey] : categoryFallbackImages.default;
}

export default function CategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchCategoriesList = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getCategories();
      setCategories(data);
    } catch (err) {
      console.error("Error fetching categories:", err);
      setError("Không thể tải danh sách danh mục vào lúc này. Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchCategoriesList();
  }, [fetchCategoriesList]);

  return (
    <div className="container-page py-8 sm:py-10 space-y-8">
      {/* ── BREADCRUMBS ────────────────────────────────────────────── */}
      <nav aria-label="Breadcrumb" className="text-xs text-gray-500 flex items-center gap-2">
        <Link href="/" className="hover:text-emerald-700 transition-colors">
          Trang chủ
        </Link>
        <span>/</span>
        <span className="text-gray-900 font-semibold">Danh mục món ăn</span>
      </nav>

      {/* ── HEADER HERO BANNER ─────────────────────────────────────── */}
      <div className="rounded-3xl culinary-hero-gradient p-8 sm:p-10 text-white shadow-md relative overflow-hidden">
        <div className="max-w-2xl space-y-3 relative z-10">
          <div className="inline-flex items-center gap-1.5 rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-emerald-100 backdrop-blur-xs border border-white/10">
            <Layers className="h-3.5 w-3.5 text-amber-400" />
            <span>Phân loại ẩm thực</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight">
            Khám phá danh mục món ăn
          </h1>
          <p className="text-xs sm:text-sm text-emerald-100/90 leading-relaxed">
            Tìm kiếm nguồn cảm hứng nấu nướng từ món Việt truyền thống, món Á đậm đà, món Âu tinh tế cho đến các món tráng miệng ngọt ngào và đồ uống thanh mát.
          </p>
          {!loading && !error && categories.length > 0 && (
            <div className="pt-2">
              <Badge variant="secondary" className="bg-amber-400 text-gray-900 font-bold border-none px-3 py-1">
                Hiện có {categories.length} danh mục
              </Badge>
            </div>
          )}
        </div>
        <div className="absolute -bottom-8 -right-8 h-44 w-44 rounded-full bg-white/5 pointer-events-none" />
      </div>

      {/* ── LOADING STATE ──────────────────────────────────────────── */}
      {loading && (
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-xs text-gray-500 mb-2">
            <ChefHat className="h-4 w-4 animate-spin text-emerald-700" />
            <span>Đang tải danh sách danh mục từ máy chủ...</span>
          </div>
          <LoadingSkeleton variant="grid" count={6} />
        </div>
      )}

      {/* ── ERROR STATE ────────────────────────────────────────────── */}
      {!loading && error && (
        <ErrorState
          title="Không thể tải danh mục"
          message={error}
          onRetry={fetchCategoriesList}
          retryText="Tải lại danh mục"
        />
      )}

      {/* ── EMPTY STATE ────────────────────────────────────────────── */}
      {!loading && !error && categories.length === 0 && (
        <EmptyState
          icon={<Utensils className="h-8 w-8" />}
          title="Chưa có danh mục nào"
          description="Hiện tại hệ thống đang cập nhật các danh mục ẩm thực mới. Vui lòng quay lại sau."
          actionLabel="Về trang chủ"
          actionHref="/"
        />
      )}

      {/* ── CATEGORIES GRID ────────────────────────────────────────── */}
      {!loading && !error && categories.length > 0 && (
        <div className="space-y-4">
          <div className="flex items-center justify-between pb-2 border-b border-gray-100">
            <h2 className="text-base sm:text-lg font-bold text-gray-900 flex items-center gap-2">
              <span>Tất cả danh mục</span>
              <span className="text-xs text-emerald-800 bg-emerald-50 px-2.5 py-0.5 rounded-full font-semibold border border-emerald-100">
                {categories.length}
              </span>
            </h2>
            <Link
              href="/"
              className="text-xs font-semibold text-emerald-700 hover:text-emerald-800 hover:underline"
            >
              ← Về Trang chủ
            </Link>
          </div>

          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {categories.map((category) => {
              const imageUrl = getCategoryImageUrl(category);
              const recipeCount = typeof category.recipeCount === "number" ? category.recipeCount : 0;

              return (
                <Link
                  key={category.id}
                  href={`/categories/${category.slug}`}
                  className="group block h-full focus:outline-none focus:ring-2 focus:ring-emerald-600 focus:ring-offset-2 rounded-2xl"
                  aria-label={`Xem chi tiết danh mục ${category.name}`}
                >
                  <Card className="overflow-hidden h-full flex flex-col justify-between culinary-card-hover border-gray-100 bg-white">
                    {/* Category Image Header */}
                    <div className="relative aspect-16/9 w-full overflow-hidden bg-gray-100">
                      <img
                        src={imageUrl}
                        alt={`Ảnh danh mục ${category.name}`}
                        className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                        loading="lazy"
                      />
                      <div className="absolute inset-0 bg-gradient-to-t from-gray-950/60 via-transparent to-transparent" />

                      {/* Top Badges */}
                      <div className="absolute top-3 right-3">
                        <span className="rounded-full bg-white/90 text-emerald-900 px-3 py-1 text-xs font-bold shadow-xs backdrop-blur-xs">
                          {recipeCount} công thức
                        </span>
                      </div>

                      {/* Bottom Title overlay on image */}
                      <div className="absolute bottom-3 left-4 right-4">
                        <h3 className="text-lg font-extrabold text-white drop-shadow-sm group-hover:text-amber-300 transition-colors">
                          {category.name}
                        </h3>
                      </div>
                    </div>

                    {/* Category Content Details */}
                    <div className="flex flex-1 flex-col justify-between p-5 space-y-4">
                      <div>
                        <p className="text-xs text-gray-600 line-clamp-3 leading-relaxed">
                          {category.description ||
                            "Khám phá các món ăn thơm ngon, chuẩn vị cùng hướng dẫn chi tiết từng bước thuộc danh mục này."}
                        </p>
                      </div>

                      {/* Action footer */}
                      <div className="pt-3 border-t border-gray-100 flex items-center justify-between text-xs font-bold text-emerald-700 group-hover:text-emerald-800">
                        <span className="flex items-center gap-1">
                          <ChefHat className="h-4 w-4" />
                          <span>Xem các công thức</span>
                        </span>
                        <span className="flex items-center gap-1 group-hover:translate-x-1 transition-transform">
                          <span>Chi tiết</span>
                          <ArrowRight className="h-3.5 w-3.5" />
                        </span>
                      </div>
                    </div>
                  </Card>
                </Link>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
