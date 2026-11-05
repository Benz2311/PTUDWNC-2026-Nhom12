import { apiFetch } from '@/lib/api';
import { recipePageSchema, type RecipePage } from '@/types/recipe';

export async function getRecipeSearchSuggestions(query: string): Promise<string[]> {
  if (query.trim().length < 2) return [];
  const response = await apiFetch<string[] | { data: string[] }>(
    `/api/v1/search/suggestions?q=${encodeURIComponent(query.trim())}`,
    {},
    false,
  );
  return Array.isArray(response) ? response : response.data;
}

export type SmartSearchFilters = {
  q: string;
  page: number;
  pageSize: number;
  categoryId?: string;
  difficulty?: string;
  maxCookTime?: string;
  minCalories?: string;
  maxCalories?: string;
  minProtein?: string;
  maxProtein?: string;
  minCarb?: string;
  maxCarb?: string;
  minFat?: string;
  maxFat?: string;
  minRating?: string;
  includeIngredients?: string;
  excludeIngredients?: string;
  sortBy?: string;
};

export async function smartSearchRecipes(filters: SmartSearchFilters): Promise<RecipePage> {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== '') params.set(key, String(value));
  }
  const response = await apiFetch<{
    data: unknown[];
    meta: { page: number; pageSize: number; total: number; totalPages: number };
  }>(`/api/v1/search/recipes?${params.toString()}`, {}, false);
  return recipePageSchema.parse({
    items: response.data,
    page: response.meta.page,
    pageSize: response.meta.pageSize,
    totalCount: response.meta.total,
  });
}
