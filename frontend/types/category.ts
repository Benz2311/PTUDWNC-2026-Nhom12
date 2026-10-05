export interface Category {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  imageUrl: string | null;
  orderIndex: number;
  recipeCount: number;
}

export interface CategoryStatistics {
  totalCategories: number;
  totalRecipes: number;
  publishedRecipes: number;
  draftRecipes: number;
  categories: CategoryStatisticItem[];
}

export interface CategoryStatisticItem {
  categoryId: string;
  categoryName: string;
  recipeCount: number;
}

export interface CategoryRecipeItem {
  id: string;
  title: string;
  slug?: string;
  description?: string | null;
  cookTime?: number;
  difficulty?: string;
  imageUrl?: string | null;
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