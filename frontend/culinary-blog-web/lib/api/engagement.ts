import { apiClient, unwrapData } from "./client";
import type { RecipeListItem } from "@/types/recipe";
import type { TrendingRecipeDto } from "@/types/trending";
import type { ApiResponse } from "@/types/api";

export async function getRecommendations(limit: number = 6): Promise<RecipeListItem[]> {
  try {
    const response = await apiClient.get<ApiResponse<RecipeListItem[]>>("/recipes/recommendations", {
      params: { limit },
    });
    return unwrapData(response) || [];
  } catch (err) {
    console.error("Failed to fetch recommendations:", err);
    return [];
  }
}

export interface TrendingFilterParams {
  period?: "7d" | "30d";
  categoryId?: string;
  categorySlug?: string;
  limit?: number;
}

export async function getTrendingRecipes(params?: TrendingFilterParams): Promise<TrendingRecipeDto[]> {
  try {
    const response = await apiClient.get<ApiResponse<TrendingRecipeDto[]>>("/recipes/trending", {
      params: {
        period: params?.period ?? "7d",
        categoryId: params?.categoryId,
        categorySlug: params?.categorySlug,
        limit: params?.limit ?? 10,
      },
    });
    return unwrapData(response) || [];
  } catch (err) {
    console.error("Failed to fetch trending recipes:", err);
    return [];
  }
}

export async function recordRecipeView(slug: string): Promise<boolean> {
  try {
    const response = await apiClient.post<ApiResponse<{ recorded: boolean }>>(
      `/recipes/${encodeURIComponent(slug)}/view`
    );
    const data = unwrapData(response);
    return data?.recorded ?? false;
  } catch (err) {
    // Non-blocking call for analytics
    console.debug(`Could not record view for ${slug}:`, err);
    return false;
  }
}
