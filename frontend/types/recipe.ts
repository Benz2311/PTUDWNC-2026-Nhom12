export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

export type RecipeDifficulty = "Easy" | "Medium" | "Hard" | "Expert";
export type RecipeStatus = "Draft" | "Published" | "Archived";

export interface RecipeListItem {
  id: string;
  title: string;
  slug: string;
  description: string;
  cookTime: number;
  prepTime: number;
  totalTime: number;
  servings: number;
  difficulty: RecipeDifficulty | string;
  status: RecipeStatus | string;
  publishedAt: string | null;
  createdAt: string;
  categoryId: string;
  categoryName: string;
  categorySlug: string;
  authorId: string;
  authorName: string;
  authorAvatarUrl: string | null;
  primaryImageUrl: string | null;
}

export interface RecipeIngredient {
  id: string;
  name: string;
  quantity: number | null;
  unit: string | null;
  notes: string | null;
  orderIndex: number;
}

export interface RecipeStep {
  id: string;
  stepNumber: number;
  title: string | null;
  description: string;
  timerMinutes: number | null;
  imageUrl: string | null;
}

export interface RecipeImage {
  id: string;
  originalUrl: string;
  mediumUrl: string | null;
  thumbnailUrl: string | null;
  altText: string | null;
  isPrimary: boolean;
  orderIndex: number;
}

export interface RecipeNutrition {
  calories: number | null;
  protein: number | null;
  carbohydrates: number | null;
  fat: number | null;
  fiber: number | null;
  sodium: number | null;
  source: string;
}

export interface RecipeDetail {
  id: string;
  title: string;
  slug: string;
  description: string;
  prepTime: number;
  cookTime: number;
  servings: number;
  difficulty: RecipeDifficulty | string;
  status: RecipeStatus | string;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  category: {
    id: string;
    name: string;
    slug: string;
  };
  author: {
    id: string;
    displayName: string;
    avatarUrl: string | null;
  };
  nutrition: RecipeNutrition | null;
  ingredients: RecipeIngredient[];
  steps: RecipeStep[];
  images: RecipeImage[];
}

export interface RecipeTrashItem {
  id: string;
  title: string;
  slug: string;
  status: string;
  deletedAt: string | null;
}

export interface RecipeFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categorySlug?: string;
  difficulty?: string;
  maxCookTimeMinutes?: number;
  maxTotalTimeMinutes?: number;
  sortBy?: "publishedAt" | "title" | "prepTime" | "cookTime" | "totalTime" | "createdAt";
  sortDirection?: "asc" | "desc";
  mine?: boolean;
  status?: string;
  authorId?: string;
}
