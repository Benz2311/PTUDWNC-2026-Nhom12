import { apiClient } from "./client";
import type {
  PagedResult,
  RecipeDetail,
  RecipeFilterParams,
  RecipeListItem,
  RecipeTrashItem,
} from "@/types/recipe";

export async function getRecipes(
  params?: RecipeFilterParams
): Promise<PagedResult<RecipeListItem>> {
  try {
    const queryParams: Record<string, unknown> = {};

    if (params?.page) queryParams.page = params.page;
    if (params?.pageSize) queryParams.pageSize = params.pageSize;
    if (params?.search?.trim()) queryParams.search = params.search.trim();
    if (params?.categorySlug?.trim()) queryParams.categorySlug = params.categorySlug.trim();
    if (params?.difficulty) queryParams.difficulty = params.difficulty;
    if (params?.maxCookTimeMinutes) queryParams.maxCookTimeMinutes = params.maxCookTimeMinutes;
    if (params?.maxTotalTimeMinutes) queryParams.maxTotalTimeMinutes = params.maxTotalTimeMinutes;
    if (params?.sortBy) queryParams.sortBy = params.sortBy;
    if (params?.sortDirection) queryParams.sortDirection = params.sortDirection;
    if (params?.mine !== undefined) queryParams.mine = params.mine;
    if (params?.status) queryParams.status = params.status;
    if (params?.authorId?.trim()) queryParams.authorId = params.authorId.trim();

    const response = await apiClient.get<PagedResult<RecipeListItem>>("/recipes", {
      params: queryParams,
    });
    return response.data;
  } catch (err) {
    console.error("Failed to fetch recipes:", err);
    return {
      items: [],
      totalCount: 0,
      page: params?.page ?? 1,
      pageSize: params?.pageSize ?? 12,
      totalPages: 0,
    };
  }
}

export async function getRecipeBySlug(
  slug: string
): Promise<RecipeDetail | null> {
  try {
    const response = await apiClient.get<RecipeDetail>(
      `/recipes/${encodeURIComponent(slug)}`
    );
    return response.data;
  } catch (err) {
    console.error(`Failed to fetch recipe with slug ${slug}:`, err);
    throw err;
  }
}

export async function getTrashRecipes(
  page = 1,
  pageSize = 12
): Promise<PagedResult<RecipeTrashItem>> {
  try {
    const response = await apiClient.get<PagedResult<RecipeTrashItem>>(
      "/admin/recipes/trash",
      {
        params: { page, pageSize },
      }
    );
    return response.data;
  } catch (err) {
    console.error("Failed to fetch trash recipes:", err);
    throw err;
  }
}
