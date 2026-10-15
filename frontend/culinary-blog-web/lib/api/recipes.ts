import { apiClient, unwrapData, unwrapPaged } from "./client";
import {
  type PagedResult,
  type RecipeDetail,
  type RecipeFilterParams,
  type RecipeListItem,
  type RecipeTrashItem,
  DIFFICULTY_MAP,
} from "@/types/recipe";
import type { ApiResponse, PaginationMeta } from "@/types/api";

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

    const response = await apiClient.get<ApiResponse<RecipeListItem[], PaginationMeta>>("/recipes", {
      params: queryParams,
    });
    return unwrapPaged<RecipeListItem>(response);
  } catch (err) {
    console.error("Failed to fetch recipes:", err);
    return {
      items: [],
      totalCount: 0,
      page: params?.page ?? 1,
      pageSize: params?.pageSize ?? 12,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false,
    };
  }
}

export async function getRecipeBySlug(
  slug: string
): Promise<RecipeDetail | null> {
  try {
    const response = await apiClient.get<ApiResponse<RecipeDetail>>(
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
    const response = await apiClient.get<ApiResponse<RecipeTrashItem[], PaginationMeta>>(
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
  const response = await apiClient.post<ApiResponse<{ id: string; slug: string }>>("/recipes", payload);
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
  const response = await apiClient.put<ApiResponse<{ id: string; slug: string; version?: string }>>(
    `/recipes/${id}`,
    payload,
    formattedEtag ? { headers: { "If-Match": formattedEtag } } : undefined
  );
  return unwrapData(response);
}

export async function deleteRecipe(id: string, etag?: string): Promise<void> {
  await apiClient.delete(`/recipes/${id}`, etag ? { headers: { "If-Match": etag } } : undefined);
}

export async function changeRecipeStatus(
  id: string,
  action: "publish" | "unpublish" | "archive" | "unarchive",
  etag?: string
): Promise<{ id: string; status: string }> {
  const response = await apiClient.post<ApiResponse<{ id: string; status: string }>>(
    `/recipes/${id}/${action}`,
    {},
    etag ? { headers: { "If-Match": etag } } : undefined
  );
  return unwrapData(response);
}

export async function restoreRecipe(id: string): Promise<{ id: string; status: string }> {
  const response = await apiClient.post<ApiResponse<{ id: string; status: string }>>(
    `/admin/recipes/${id}/restore`,
    {}
  );
  return unwrapData(response);
}

export async function purgeRecipe(id: string): Promise<void> {
  await apiClient.delete(`/admin/recipes/${id}/purge`);
}
