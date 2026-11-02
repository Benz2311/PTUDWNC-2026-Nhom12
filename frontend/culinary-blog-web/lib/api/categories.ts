import { apiClient, unwrapData } from "./client";
import type {
  Category,
  CategoryDetailResponse,
  CategoryStatistics,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from "@/types/category";
import type { ApiResponse } from "@/types/api";

export class CategoryConflictError extends Error {
  status = 409;
  constructor(message: string) {
    super(message);
    this.name = "CategoryConflictError";
  }
}

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
  try {
    const response = await apiClient.post<ApiResponse<Category>>("/categories", data);
    return unwrapData(response);
  } catch (err: any) {
    if (err?.response?.status === 409 || err?.status === 409) {
      throw new CategoryConflictError(
        err?.response?.data?.detail || err?.title || "Tên danh mục đã tồn tại trong hệ thống."
      );
    }
    throw err;
  }
}

export async function updateCategory(
  id: string,
  data: UpdateCategoryRequest
): Promise<void> {
  try {
    await apiClient.put(`/categories/${id}`, data);
  } catch (err: any) {
    if (err?.response?.status === 409 || err?.status === 409) {
      throw new CategoryConflictError(
        err?.response?.data?.detail || err?.title || "Tên danh mục đã tồn tại trong hệ thống."
      );
    }
    throw err;
  }
}

export async function deleteCategory(id: string): Promise<void> {
  try {
    await apiClient.delete(`/categories/${id}`);
  } catch (err: any) {
    if (err?.response?.status === 409 || err?.status === 409) {
      throw new CategoryConflictError(
        err?.response?.data?.detail || "Không thể xóa danh mục vì vẫn còn công thức liên kết."
      );
    }
    throw err;
  }
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
