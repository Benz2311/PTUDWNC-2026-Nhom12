import { apiDelete, apiGet, apiPost, apiPut, unwrapData, unwrapPaged } from "./client";
import {
  type PagedResult,
  type RecipeDetail,
  type RecipeFilterParams,
  type RecipeListItem,
  type RecipeTrashItem,
  type RecipeRatingSummary,
  type RecipeComment,
  DIFFICULTY_MAP,
} from "@/types/recipe";
import type { ApiResponse, PaginationMeta } from "@/types/api";

export async function getRecipes(
  params?: RecipeFilterParams
): Promise<PagedResult<RecipeListItem>> {
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

  const response = await apiGet<ApiResponse<RecipeListItem[], PaginationMeta>>("/recipes", {
    params: queryParams,
  });
  return unwrapPaged<RecipeListItem>(response);
}

export async function getRecipeBySlug(
  slug: string
): Promise<RecipeDetail | null> {
  try {
    const response = await apiGet<ApiResponse<RecipeDetail>>(
      `/recipes/${encodeURIComponent(slug)}`
    );
    return unwrapData(response);
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
    const response = await apiGet<ApiResponse<RecipeTrashItem[], PaginationMeta>>(
      "/admin/recipes/trash",
      {
        params: { page, pageSize },
      }
    );
    return unwrapPaged<RecipeTrashItem>(response);
  } catch (err) {
    console.error("Failed to fetch trash recipes:", err);
    throw err;
  }
}

export async function createRecipe(data: Record<string, unknown>): Promise<{ id: string; slug: string }> {
  const payload = { ...data };
  if (typeof payload.difficulty === "string" && payload.difficulty in DIFFICULTY_MAP) {
    payload.difficulty = DIFFICULTY_MAP[payload.difficulty as keyof typeof DIFFICULTY_MAP];
  }
  const response = await apiPost<ApiResponse<{ id: string; slug: string }>>("/recipes", payload);
  return unwrapData(response);
}

export async function duplicateRecipe(
  recipeId: string
): Promise<{ id: string; slug: string; title: string; status: string }> {
  const response = await apiPost<ApiResponse<{ id: string; slug: string; title: string; status: string }>>(
    `/recipes/${encodeURIComponent(recipeId)}/duplicate`,
    {}
  );
  return unwrapData(response);
}

export async function updateRecipe(
  id: string,
  data: Record<string, unknown>,
  etag?: string
): Promise<{ id: string; slug: string; version?: string }> {
  const payload = { ...data };
  if (typeof payload.difficulty === "string" && payload.difficulty in DIFFICULTY_MAP) {
    payload.difficulty = DIFFICULTY_MAP[payload.difficulty as keyof typeof DIFFICULTY_MAP];
  }
  const cleanEtag = etag?.replace(/^"+|"+$/g, '').trim();
  const formattedEtag = cleanEtag ? `"${cleanEtag}"` : undefined;
  const response = await apiPut<ApiResponse<{ id: string; slug: string; version?: string }>>(
    `/recipes/${id}`,
    payload,
    formattedEtag ? { headers: { "If-Match": formattedEtag } } : undefined
  );
  return unwrapData(response);
}

export async function deleteRecipe(id: string, etag?: string): Promise<void> {
  await apiDelete(`/recipes/${id}`, etag ? { headers: { "If-Match": etag } } : undefined);
}

export async function changeRecipeStatus(
  id: string,
  action: "publish" | "unpublish" | "archive" | "unarchive",
  etag?: string
): Promise<{ id: string; status: string }> {
  const response = await apiPost<ApiResponse<{ id: string; status: string }>>(
    `/recipes/${id}/${action}`,
    {},
    etag ? { headers: { "If-Match": etag } } : undefined
  );
  return unwrapData(response);
}

export async function restoreRecipe(id: string): Promise<{ id: string; status: string }> {
  const response = await apiPost<ApiResponse<{ id: string; status: string }>>(
    `/admin/recipes/${id}/restore`,
    {}
  );
  return unwrapData(response);
}

export async function purgeRecipe(id: string): Promise<void> {
  await apiDelete(`/admin/recipes/${id}/purge`);
}

export async function toggleRecipeFavorite(
  recipeId: string
): Promise<{ recipeId: string; isFavorite: boolean }> {
  const response = await apiPost<ApiResponse<{ recipeId: string; isFavorite: boolean }>>(
    `/recipes/${recipeId}/favorite`,
    {}
  );
  return unwrapData(response);
}

export async function getRecipeFavoriteStatus(
  recipeId: string
): Promise<{ recipeId: string; isFavorite: boolean }> {
  const response = await apiGet<ApiResponse<{ recipeId: string; isFavorite: boolean }>>(
    `/recipes/${recipeId}/favorite`
  );
  return unwrapData(response);
}

export async function getRecipeRatings(recipeId: string): Promise<RecipeRatingSummary> {
  const response = await apiGet<ApiResponse<RecipeRatingSummary>>(
    `/recipes/${recipeId}/ratings`
  );
  return unwrapData(response);
}

export async function upsertRecipeRating(
  recipeId: string,
  score: number
): Promise<RecipeRatingSummary> {
  const response = await apiPut<ApiResponse<RecipeRatingSummary>>(
    `/recipes/${recipeId}/ratings/me`,
    { score }
  );
  return unwrapData(response);
}

export async function deleteRecipeRating(recipeId: string): Promise<RecipeRatingSummary> {
  const response = await apiDelete<ApiResponse<RecipeRatingSummary>>(
    `/recipes/${recipeId}/ratings/me`
  );
  return unwrapData(response);
}

export async function getRecipeComments(
  recipeId: string,
  page = 1,
  pageSize = 20
): Promise<PagedResult<RecipeComment>> {
  const response = await apiGet<ApiResponse<RecipeComment[], PaginationMeta>>(
    `/recipes/${recipeId}/comments`,
    { params: { page, pageSize } }
  );
  return unwrapPaged<RecipeComment>(response);
}

export async function addRecipeComment(
  recipeId: string,
  content: string,
  parentCommentId?: string
): Promise<{ id: string; content: string; createdAt: string }> {
  const response = await apiPost<ApiResponse<{ id: string; content: string; createdAt: string }>>(
    `/recipes/${recipeId}/comments`,
    { content, parentCommentId: parentCommentId ?? null }
  );
  return unwrapData(response);
}

export async function updateRecipeComment(
  recipeId: string,
  commentId: string,
  content: string
): Promise<{ id: string; content: string; updatedAt?: string }> {
  const response = await apiPut<ApiResponse<{ id: string; content: string; updatedAt?: string }>>(
    `/recipes/${recipeId}/comments/${commentId}`,
    { content }
  );
  return unwrapData(response);
}

export async function deleteRecipeComment(
  recipeId: string,
  commentId: string
): Promise<void> {
  await apiDelete(`/recipes/${recipeId}/comments/${commentId}`);
}
