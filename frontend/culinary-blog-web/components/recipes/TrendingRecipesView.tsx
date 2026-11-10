"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { Flame, Clock, Eye, ChefHat, Filter } from "lucide-react";
import type { TrendingRecipeDto } from "@/types/trending";
import type { Category } from "@/types/category";
import { getTrendingRecipes } from "@/lib/api/engagement";
import { getCategories } from "@/lib/api/categories";
import { resolveImageUrl } from "@/lib/image-url";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { EmptyState } from "@/components/common/EmptyState";
import { ErrorState } from "@/components/common/ErrorState";
import { Badge } from "@/components/ui/Badge";
import { Card } from "@/components/ui/Card";
import DifficultyBadge from "@/components/recipe/DifficultyBadge";

const fallbackImages = [
  "/sample-images/food-01.svg",
  "/sample-images/food-02.svg",
  "/sample-images/food-03.svg",
  "/sample-images/food-04.svg",
  "/sample-images/food-05.svg",
];

export function TrendingRecipesView() {
  const [period, setPeriod] = useState<"7d" | "30d">("7d");
  const [selectedCategory, setSelectedCategory] = useState<string>("");
  const [categories, setCategories] = useState<Category[]>([]);
  const [recipes, setRecipes] = useState<TrendingRecipeDto[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Load categories for filter
  useEffect(() => {
    getCategories()
      .then(setCategories)
      .catch((err) => console.error("Could not load categories for trending filter:", err));
  }, []);

  // Fetch trending recipes
  const fetchTrending = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getTrendingRecipes({
        period,
        categorySlug: selectedCategory || undefined,
        limit: 15,
      });
      setRecipes(data);
    } catch (err) {
      console.error("Error loading trending recipes:", err);
      setError("Không thể tải danh sách công thức thịnh hành. Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTrending();
  }, [period, selectedCategory]);

  return (
    <div className="space-y-8" data-testid="trending-recipes-view">
      {/* Top Banner */}
      <div className="rounded-3xl bg-linear-to-r from-amber-500 via-orange-500 to-rose-600 p-8 sm:p-10 text-white shadow-lg relative overflow-hidden">
        <div className="absolute right-0 top-0 -mt-10 -mr-10 w-64 h-64 bg-white/10 rounded-full blur-2xl pointer-events-none" />
        <div className="relative z-10 max-w-2xl">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/20 backdrop-blur-md text-amber-100 text-xs font-bold mb-3">
            <Flame className="h-4 w-4 text-amber-200 fill-amber-200 animate-pulse" />
            <span>Xu hướng ẩm thực</span>
          </div>
          <h1 className="text-2xl sm:text-4xl font-extrabold tracking-tight">
            Công thức thịnh hành
          </h1>
          <p className="mt-2 text-sm sm:text-base text-amber-100">
            Khám phá những món ăn được cộng đồng đầu bếp và người yêu ẩm thực quan tâm và nấu nhiều nhất.
          </p>
        </div>
      </div>

      {/* Controls Bar: Period selector & Category filter */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-2xl bg-white border border-gray-100 shadow-xs">
        {/* Period Selector Tabs */}
        <div className="inline-flex items-center p-1 rounded-xl bg-gray-100 text-xs font-bold">
          <button
            type="button"
            onClick={() => setPeriod("7d")}
            className={`px-4 py-2 rounded-lg transition-all cursor-pointer ${
              period === "7d"
                ? "bg-white text-orange-600 shadow-xs"
                : "text-gray-600 hover:text-gray-900"
            }`}
          >
            🔥 7 ngày qua
          </button>
          <button
            type="button"
            onClick={() => setPeriod("30d")}
            className={`px-4 py-2 rounded-lg transition-all cursor-pointer ${
              period === "30d"
                ? "bg-white text-orange-600 shadow-xs"
                : "text-gray-600 hover:text-gray-900"
            }`}
          >
            ⭐ 30 ngày qua
          </button>
        </div>

        {/* Category Filter */}
        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-gray-500" />
          <select
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
            className="text-xs font-medium bg-gray-50 border border-gray-200 rounded-xl px-3 py-2 text-gray-700 focus:outline-none focus:ring-2 focus:ring-orange-500"
            aria-label="Lọc theo danh mục"
          >
            <option value="">Tất cả danh mục</option>
            {categories.map((c) => (
              <option key={c.id} value={c.slug}>
                {c.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Trending List */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {Array.from({ length: 6 }).map((_, i) => (
            <LoadingSkeleton key={i} variant="card" />
          ))}
        </div>
      ) : error ? (
        <ErrorState
          title="Không thể tải danh sách thịnh hành"
          message={error}
          onRetry={fetchTrending}
        />
      ) : recipes.length === 0 ? (
        <EmptyState
          title="Chưa có dữ liệu thịnh hành"
          description="Chưa có lượt xem nào được ghi nhận cho danh mục này trong khoảng thời gian đã chọn."
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {recipes.map((item) => {
            const resolvedImg =
              resolveImageUrl(item.coverImageUrl) ||
              fallbackImages[Math.abs(item.title.length) % fallbackImages.length];

            const totalTime = (item.prepTimeMinutes || 0) + (item.cookTimeMinutes || 0);

            // Rank Badge Styling
            const isTop1 = item.rank === 1;
            const isTop2 = item.rank === 2;
            const isTop3 = item.rank === 3;

            return (
              <Card
                key={item.recipeId}
                className="group relative flex flex-col bg-white rounded-2xl border border-gray-100 overflow-hidden shadow-xs hover:shadow-xl hover:border-orange-200 hover:-translate-y-1 transition-all duration-300"
              >
                {/* Image Container with Rank Badge */}
                <div className="relative aspect-video w-full overflow-hidden bg-gray-100">
                  <img
                    src={resolvedImg}
                    alt={item.title}
                    className="h-full w-full object-cover group-hover:scale-105 transition-transform duration-500"
                    loading="lazy"
                  />

                  {/* Rank Badge */}
                  <div className="absolute top-3 left-3 flex items-center gap-1.5">
                    <span
                      className={`inline-flex items-center justify-center font-extrabold text-xs px-2.5 py-1 rounded-full shadow-md ${
                        isTop1
                          ? "bg-amber-400 text-amber-950 ring-2 ring-amber-200"
                          : isTop2
                          ? "bg-slate-200 text-slate-800 ring-2 ring-slate-100"
                          : isTop3
                          ? "bg-amber-700 text-white ring-2 ring-amber-500"
                          : "bg-black/60 text-white backdrop-blur-xs"
                      }`}
                    >
                      {isTop1 ? "🥇 #1" : isTop2 ? "🥈 #2" : isTop3 ? "🥉 #3" : `#${item.rank}`}
                    </span>
                  </div>

                  {/* Category Pill */}
                  <div className="absolute top-3 right-3">
                    <Badge variant="secondary" className="bg-white/90 backdrop-blur-xs text-gray-800 text-[11px] font-bold">
                      {item.categoryName}
                    </Badge>
                  </div>
                </div>

                {/* Content */}
                <div className="p-5 flex flex-col flex-1 justify-between">
                  <div>
                    <h3 className="font-bold text-gray-900 text-base line-clamp-2 group-hover:text-orange-600 transition-colors">
                      <Link href={`/recipes/${item.slug}`}>
                        {item.title}
                      </Link>
                    </h3>

                    <div className="flex items-center gap-2 text-xs text-gray-500 mt-2">
                      <ChefHat className="h-3.5 w-3.5 text-gray-400" />
                      <span>{item.authorName}</span>
                    </div>
                  </div>

                  <div className="pt-4 mt-4 border-t border-gray-100 flex items-center justify-between text-xs">
                    {/* Time & Difficulty */}
                    <div className="flex items-center gap-3 text-gray-600 font-medium">
                      {totalTime > 0 && (
                        <span className="flex items-center gap-1">
                          <Clock className="h-3.5 w-3.5 text-gray-400" />
                          {totalTime}p
                        </span>
                      )}
                      <DifficultyBadge difficulty={item.difficulty} />
                    </div>

                    {/* Views Count */}
                    <div className="flex items-center gap-1 text-orange-600 font-bold bg-orange-50 px-2.5 py-1 rounded-full">
                      <Eye className="h-3.5 w-3.5" />
                      <span>{item.viewsInPeriod} lượt</span>
                    </div>
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}

export default TrendingRecipesView;
