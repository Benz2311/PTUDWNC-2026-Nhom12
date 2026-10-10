export interface Category {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  imageUrl?: string | null;
  orderIndex?: number;
  recipeCount: number;
}

export interface CategoryStatisticItem {
  categoryId: string;
  categoryName: string;
  recipeCount: number;
  percentage?: number;
}

export interface RecipeMonthlyStatistic {
  year: number;
  month: number;
  count?: number;
  recipeCount?: number;
}

export interface CategoryStatistics {
  totalCategories: number;
  totalRecipes: number;
  publishedRecipes: number;
  draftRecipes: number;
  archivedRecipes?: number;
  categories: CategoryStatisticItem[];
  topCategories?: CategoryStatisticItem[];
  recipesByMonth?: RecipeMonthlyStatistic[];
}

export interface CategoryRecipeItem {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  cookTime?: number;
  prepTime?: number;
  servings?: number;
  difficulty?: string;
  primaryImageUrl?: string | null;
  status?: string;
  publishedAt?: string | null;
}

export interface CategoryDetailResponse {
  category: {
    id: string;
    name: string;
    slug: string;
    description: string | null;
  };
  recipes: {
    items: CategoryRecipeItem[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
  };
}

export interface CreateCategoryRequest {
  name: string;
  description?: string | null;
}

export interface UpdateCategoryRequest {
  name: string;
  description?: string | null;
}
