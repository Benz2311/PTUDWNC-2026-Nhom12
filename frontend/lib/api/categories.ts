import { apiFetch } from "@/lib/api";
import type {
  Category,
  CategoryStatistics,
} from "@/types/category";

export async function getCategories(): Promise<Category[]> {
  return apiFetch<Category[]>("/api/v1/categories", {}, false);
}

export async function getCategoryStatistics():
  Promise<CategoryStatistics> {
  return apiFetch<CategoryStatistics>(
    "/api/v1/categories/statistics",
    {},
    false,
  );
}