"use client";

import React, { useEffect, useState } from "react";
import { Sparkles, RefreshCw } from "lucide-react";
import type { RecipeListItem } from "@/types/recipe";
import { getRecommendations } from "@/lib/api/engagement";
import RecipeCard from "@/components/recipe/RecipeCard";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";
import { EmptyState } from "@/components/common/EmptyState";
import { ErrorState } from "@/components/common/ErrorState";
import { Button } from "@/components/ui/Button";

interface RecipeRecommendationsProps {
  limit?: number;
  className?: string;
}

export function RecipeRecommendations({ limit = 6, className = "" }: RecipeRecommendationsProps) {
  const [recipes, setRecipes] = useState<RecipeListItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchRecommendations = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getRecommendations(limit);
      setRecipes(data);
    } catch (err) {
      console.error("Failed to load recommendations:", err);
      setError("Không thể tải danh sách gợi ý công thức vào lúc này.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRecommendations();
  }, [limit]);

  return (
    <section
      data-testid="recipe-recommendations"
      className={`rounded-3xl bg-linear-to-br from-emerald-50/60 via-teal-50/30 to-amber-50/40 border border-emerald-100/80 p-6 sm:p-8 shadow-xs ${className}`}
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-100 text-emerald-800 text-xs font-bold mb-2">
            <Sparkles className="h-3.5 w-3.5 text-emerald-600" />
            <span>Gợi ý cá nhân hóa</span>
          </div>
          <h2 className="text-xl sm:text-2xl font-extrabold text-gray-900 tracking-tight">
            Có thể bạn sẽ thích
          </h2>
          <p className="text-xs sm:text-sm text-gray-600 mt-1">
            Các món ăn được lựa chọn dựa trên sở thích danh mục và lịch sử trải nghiệm của bạn
          </p>
        </div>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={fetchRecommendations}
          disabled={loading}
          className="self-start sm:self-auto rounded-xl border-emerald-200 text-emerald-800 hover:bg-emerald-100/60 font-semibold text-xs cursor-pointer"
        >
          <RefreshCw className={`h-3.5 w-3.5 mr-1.5 ${loading ? "animate-spin" : ""}`} />
          Làm mới gợi ý
        </Button>
      </div>

      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {Array.from({ length: Math.min(limit, 3) }).map((_, i) => (
            <LoadingSkeleton key={i} variant="card" />
          ))}
        </div>
      ) : error ? (
        <ErrorState
          title="Không thể tải gợi ý"
          message={error}
          onRetry={fetchRecommendations}
        />
      ) : recipes.length === 0 ? (
        <EmptyState
          title="Chưa có gợi ý phù hợp"
          description="Hãy khám phá thêm các công thức để hệ thống ghi nhận sở thích ẩm thực của bạn nhé!"
        />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {recipes.map((recipe) => (
            <RecipeCard key={recipe.id} recipe={recipe} />
          ))}
        </div>
      )}
    </section>
  );
}

export default RecipeRecommendations;
