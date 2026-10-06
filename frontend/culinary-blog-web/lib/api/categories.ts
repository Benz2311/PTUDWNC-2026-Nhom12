import { apiClient } from "./client";
import type {
  Category,
  CategoryDetailResponse,
  CategoryStatistics,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from "@/types/category";

export async function getCategories(): Promise<Category[]> {
  try {
    const response = await apiClient.get<Category[]>("/categories");
    return response.data;
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
    const response = await apiClient.get<CategoryDetailResponse>(
      `/categories/${encodeURIComponent(slug)}`,
      {
        params: { page, pageSize },
      }
    );
    return response.data;
  } catch (err) {
    console.error(`Failed to fetch category with slug ${slug}:`, err);
    return null;
  }
}

export async function createCategory(
  data: CreateCategoryRequest
): Promise<Category> {
  const response = await apiClient.post<Category>("/categories", data);
  return response.data;
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
    const response = await apiClient.get<CategoryStatistics>(
      "/categories/statistics"
    );
    return response.data;
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
