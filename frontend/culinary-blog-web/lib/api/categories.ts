import { apiClient, unwrapData } from "./client";
import type {
  Category,
  CategoryDetailResponse,
  CategoryStatistics,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from "@/types/category";
import type { ApiResponse } from "@/types/api";

export async function getCategories(): Promise<Category[]> {
  try {
    const response = await apiClient.get<ApiResponse<Category[]>>("/categories");
    return unwrapData(response);
  } catch (err) {
    console.error("Failed to fetch categories:", err);
    return [];
  }
}

export async function getCategoryBySlug(
  slug: string,
  page = 1,
  pageSize = 12
): Promise<CategoryDetailResponse | null> {
  try {
    const response = await apiClient.get<ApiResponse<CategoryDetailResponse>>(
      `/categories/${encodeURIComponent(slug)}`,
      {
        params: { page, pageSize },
      }
    );
    return unwrapData(response);
  } catch (err) {
    console.error(`Failed to fetch category with slug ${slug}:`, err);
    return null;
  }
}

export async function createCategory(
  data: CreateCategoryRequest
): Promise<Category> {
  const response = await apiClient.post<ApiResponse<Category>>("/categories", data);
  return unwrapData(response);
}

export async function updateCategory(
  id: string,
  data: UpdateCategoryRequest
): Promise<void> {
  await apiClient.put(`/categories/${id}`, data);
}

export async function deleteCategory(id: string): Promise<void> {
  await apiClient.delete(`/categories/${id}`);
}

export async function getCategoryStatistics(): Promise<CategoryStatistics> {
  try {
    const response = await apiClient.get<ApiResponse<CategoryStatistics>>(
      "/categories/statistics"
    );
    return unwrapData(response);
  } catch (err) {
    console.error("Failed to fetch category statistics:", err);
    return {
      totalCategories: 0,
      totalRecipes: 0,
      publishedRecipes: 0,
      draftRecipes: 0,
      archivedRecipes: 0,
      categories: [],
      topCategories: [],
      recipesByMonth: [],
    };
  }
}
