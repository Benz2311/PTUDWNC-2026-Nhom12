import axios from "axios";
import type {
  Category,
  CategoryDetailResponse,
  CategoryStatistics,
} from "@/types/category";

const API_URL =
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5000/api/v1";

const api = axios.create({
  baseURL: API_URL,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 5000,
});

export async function getCategories(): Promise<Category[]> {
  try {
    const response = await api.get<Category[]>(
      "/categories"
    );
    return response.data;
  } catch {
    return [];
  }
}

export async function getCategoryStatistics():
  Promise<CategoryStatistics> {
  try {
    const response =
      await api.get<CategoryStatistics>(
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
    const response = await api.get<CategoryDetailResponse>(
      `/categories/${slug}`
    );
    return response.data;
  } catch {
    return null;
  }
}

