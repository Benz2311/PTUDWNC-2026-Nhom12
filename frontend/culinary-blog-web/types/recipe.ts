import type { PagedResult } from "./api";

import { z } from "zod";

export const recipeStatusSchema = z.enum(["Draft", "Published", "Archived"]);
export type RecipeStatus = z.infer<typeof recipeStatusSchema>;

export const difficultyLevelSchema = z.enum(["Easy", "Medium", "Hard", "Expert"]);
export type DifficultyLevel = z.infer<typeof difficultyLevelSchema>;
export type RecipeDifficulty = DifficultyLevel;

export const DIFFICULTY_MAP: Record<string, number> = {
  Easy: 1,
  Medium: 2,
  Hard: 3,
  Expert: 4,
};

export const REVERSE_DIFFICULTY_MAP: Record<number, RecipeDifficulty> = {
  1: "Easy",
  2: "Medium",
  3: "Hard",
  4: "Expert",
};

export type { PagedResult };

export interface RecipeListItem {
  id: string;
  title: string;
  slug: string;
  description: string;
  cookTime?: number;
  prepTime?: number;
  totalTime?: number;
  prepTimeMinutes?: number;
  cookTimeMinutes?: number;
  servings: number;
  difficulty: RecipeDifficulty | string | number;
  status?: RecipeStatus | string | number;
  publishedAt: string | null;
  createdAt?: string;
  categoryId?: string;
  categoryName?: string;
  categorySlug?: string;
  category?: {
    id: string;
    name: string;
    slug: string;
    description?: string | null;
  };
  authorId?: string;
  authorName?: string;
  authorAvatarUrl?: string | null;
  author?: {
    id: string;
    displayName: string;
    avatarUrl?: string | null;
  };
  imageUrl?: string | null;
  coverImageUrl?: string | null;
  primaryImageUrl?: string | null;
  images?: RecipeImage[];
}

export type RecipeSummary = RecipeListItem;

export interface RecipeIngredient {
  id: string;
  name: string;
  quantity: number | null;
  unit: string | null;
  notes: string | null;
  orderIndex: number;
  sortOrder?: number;
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
  url?: string;
  sortOrder?: number;
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
  content?: string;
  prepTimeMinutes?: number;
  cookTimeMinutes?: number;
  prepTime?: number;
  cookTime?: number;
  totalTime?: number;
  servings: number;
  difficulty: RecipeDifficulty | string | number;
  status: RecipeStatus | string | number;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  version?: string;
  coverImageUrl?: string | null;
  authorId?: string;
  categoryId?: string;
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

export type Recipe = RecipeDetail;

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

export type RecipePage = PagedResult<RecipeListItem>;

export const recipeIngredientSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  quantity: z.number().nullable(),
  unit: z.string().nullable(),
  sortOrder: z.number().int().optional(),
  notes: z.string().nullable(),
});

export const recipeNutritionSchema = z.object({
  calories: z.number().nullable(),
  protein: z.number().nullable(),
  carbohydrates: z.number().nullable(),
  fat: z.number().nullable(),
  fiber: z.number().nullable(),
  sodium: z.number().nullable(),
});

export const recipeStepSchema = z.object({
  id: z.string().uuid(),
  stepNumber: z.number().int(),
  title: z.string(),
  description: z.string(),
});

export const recipeImageSchema = z.object({
  id: z.string().uuid(),
  url: z.string(),
  altText: z.string().nullable(),
  isPrimary: z.boolean(),
  sortOrder: z.number().int().optional(),
});

export const recipeSummarySchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  slug: z.string(),
  description: z.string().nullable(),
  category: z.string().optional(),
  author: z.string().optional(),
  prepTimeMinutes: z.number().int().optional(),
  cookTimeMinutes: z.number().int().optional(),
  servings: z.number().int(),
  difficulty: difficultyLevelSchema,
  status: recipeStatusSchema,
  createdAt: z.string().optional(),
  primaryImageUrl: z.string().nullable().optional(),
});

export const recipeDraftRequestSchema = z.object({
  categoryId: z.string().uuid(),
  title: z.string().trim().min(5).max(200),
  description: z.string().max(2000),
  content: z.string().trim().min(1),
  prepTimeMinutes: z.number().int().positive(),
  cookTimeMinutes: z.number().int().nonnegative(),
  servings: z.number().int().positive(),
  difficulty: difficultyLevelSchema,
  nutrition: z.object({
    calories: z.number().nonnegative(),
    protein: z.number().nonnegative(),
    carbohydrates: z.number().nonnegative(),
    fat: z.number().nonnegative(),
    fiber: z.number().nonnegative(),
    sodium: z.number().nonnegative(),
  }),
  ingredients: z.array(
    z.object({
      name: z.string().trim().min(1).max(200),
      quantity: z.number().positive().nullable(),
      unit: z.string().max(50).nullable(),
      notes: z.string().max(500).nullable(),
      orderIndex: z.number().int().nonnegative(),
    })
  ),
  images: z.array(
    z.object({
      url: z.string().min(1),
      altText: z.string().nullable(),
    })
  ),
  steps: z.array(
    z.object({
      title: z.string().max(200),
      description: z.string().trim().min(1).max(2000),
    })
  ),
});
export type RecipeDraftRequest = z.infer<typeof recipeDraftRequestSchema>;
