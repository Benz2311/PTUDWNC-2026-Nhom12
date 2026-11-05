import { apiGet } from "./client";
import type {
  Category,
  CategoryDetailResponse,
  CategoryStatistics,
} from "@/types/category";

export async function getCategories(): Promise<Category[]> {
  try {
    const response = await apiGet<unknown>("/categories");
    const payload = response.data as unknown;
    if (Array.isArray(payload)) return payload as Category[];
    if (payload && typeof payload === "object") {
      const envelope = payload as { data?: unknown; items?: unknown };
      if (Array.isArray(envelope.data)) return envelope.data as Category[];
      if (Array.isArray(envelope.items)) return envelope.items as Category[];
    }
    return [];
  } catch {
    return [];
  }
}

export async function getCategoryStatistics():
  Promise<CategoryStatistics> {
  try {
    const response =
      await apiGet<CategoryStatistics>(
        "/categories/statistics"
      );
    return response.data;
  } catch {
    return {
      totalCategories: 0,
      totalRecipes: 0,
      publishedRecipes: 0,
      draftRecipes: 0,
      categories: [],
    };
  }
}

export async function getCategoryBySlug(
  slug: string
): Promise<CategoryDetailResponse | null> {
  try {
    const response = await apiGet<CategoryDetailResponse>(
      `/categories/${slug}`
    );
    return response.data;
  } catch {
    return null;
  }
}
