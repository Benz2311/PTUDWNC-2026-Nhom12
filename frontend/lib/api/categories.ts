import axios from "axios";
import type {
  Category,
  CategoryDetailResponse,
  CategoryStatistics,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from "@/types/category";

const API_URL =
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5000/api/v1";

const api = axios.create({
  baseURL: API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

export class CategoryConflictError extends Error {
  status = 409;
  constructor(message: string) {
    super(message);
    this.name = "CategoryConflictError";
  }
}

export async function getCategories(): Promise<Category[]> {
  try {
    const response = await api.get("/categories");
    const payload = response.data;
    if (payload && typeof payload === "object" && "data" in payload) {
      return payload.data;
    }
    return payload;
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
    const response = await api.get(`/categories/${encodeURIComponent(slug)}`, {
      params: { page, pageSize },
    });
    const payload = response.data;
    if (payload && typeof payload === "object" && "data" in payload) {
      return payload.data;
    }
    return payload;
  } catch (err) {
    console.error(`Failed to fetch category with slug ${slug}:`, err);
    return null;
  }
}

export async function createCategory(
  data: CreateCategoryRequest
): Promise<Category> {
  try {
    const response = await api.post("/categories", data);
    const payload = response.data;
    return (payload && typeof payload === "object" && "data" in payload) ? payload.data : payload;
  } catch (err: any) {
    if (err?.response?.status === 409) {
      throw new CategoryConflictError(
        err?.response?.data?.detail || "Tên danh mục đã tồn tại trong hệ thống."
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
    await api.put(`/categories/${id}`, data);
  } catch (err: any) {
    if (err?.response?.status === 409) {
      throw new CategoryConflictError(
        err?.response?.data?.detail || "Tên danh mục đã tồn tại trong hệ thống."
      );
    }
    throw err;
  }
}

export async function deleteCategory(id: string): Promise<void> {
  try {
    await api.delete(`/categories/${id}`);
  } catch (err: any) {
    if (err?.response?.status === 409) {
      throw new CategoryConflictError(
        "Không thể xóa danh mục vì vẫn còn công thức liên kết."
      );
    }
    throw err;
  }
}

export async function getCategoryStatistics(): Promise<CategoryStatistics> {
  try {
    const response = await api.get("/categories/statistics");
    const payload = response.data;
    return (payload && typeof payload === "object" && "data" in payload) ? payload.data : payload;
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
