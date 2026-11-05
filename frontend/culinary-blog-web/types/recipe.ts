import type { PagedResult } from "./api";

import { z } from "zod";

export const recipeStatusSchema = z.enum(["Draft", "Published", "Archived", "PendingApproval", "Approved", "Rejected"]);
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
  orderIndex?: number;
  sortOrder: number;
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
  source?: "Manual";
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

export interface RecipeRatingSummary {
  recipeId: string;
  averageRating: number;
  ratingCount: number;
  myScore: number | null;
}

export interface RecipeCommentReply {
  id: string;
  content: string;
  createdAt: string;
  updatedAt?: string | null;
  author: string;
  userId: string;
}

export interface RecipeComment {
  id: string;
  content: string;
  createdAt: string;
  updatedAt?: string | null;
  author: string;
  userId: string;
  replies: RecipeCommentReply[];
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

export const recipeIngredientSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  quantity: z.number().nullable(),
  unit: z.string().nullable(),
  notes: z.string().nullable(),
  sortOrder: z.number().int(),
  orderIndex: z.number().int().optional(),
});

export const recipeNutritionSchema = z.object({
  calories: z.number().nullable(),
  protein: z.number().nullable(),
  carbohydrates: z.number().nullable(),
  fat: z.number().nullable(),
  fiber: z.number().nullable(),
  sodium: z.number().nullable(),
  source: z.literal("Manual").optional(),
});

export const recipeStepSchema = z.object({
  id: z.string().uuid(),
  stepNumber: z.number().int(),
  title: z.string().nullable(),
  description: z.string(),
  timerMinutes: z.number().int().nullable().optional(),
  imageUrl: z.string().nullable().optional(),
});

export const recipeImageOutputSchema = z.object({
  id: z.string().uuid(),
  url: z.string(),
  altText: z.string().nullable(),
  isPrimary: z.boolean(),
  sortOrder: z.number().int().optional(),
});

export const recipeImageSchema = z.preprocess((value) => {
  if (!value || typeof value === "undefined") return value;
  if (!value || typeof value !== "object" || Array.isArray(value)) return value;
  const image = value as Record<string, unknown>;
  return {
    ...image,
    url: typeof image.url === "string"
      ? image.url
      : image.originalUrl ?? image.mediumUrl ?? image.thumbnailUrl,
    isPrimary: image.isPrimary ?? false,
    sortOrder: image.sortOrder ?? image.orderIndex,
  };
}, recipeImageOutputSchema);

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

export const recipeDetailOutputSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  slug: z.string(),
  description: z.string().nullable(),
  content: z.string().default(""),
  categoryId: z.string().uuid().optional(),
  category: z.unknown(),
  author: z.unknown().nullable().optional(),
  prepTimeMinutes: z.number().int(),
  cookTimeMinutes: z.number().int(),
  servings: z.number().int(),
  difficulty: difficultyLevelSchema,
  status: recipeStatusSchema,
  createdAt: z.string().optional(),
  publishedAt: z.string().nullable().optional(),
  updatedAt: z.string().nullable().optional(),
  version: z.string().optional(),
  ingredients: z.array(recipeIngredientSchema).default([]),
  steps: z.array(recipeStepSchema).default([]),
  nutrition: recipeNutritionSchema.nullable().optional(),
  images: z.array(recipeImageSchema).default([]),
});

export const recipeDetailSchema = z.preprocess((value) => {
  if (!value || typeof value !== "object" || Array.isArray(value)) return value;
  const recipe = value as Record<string, unknown>;
  const category = recipe.category;
  const author = recipe.author;
  return {
    ...recipe,
    categoryId: recipe.categoryId ??
      (category && typeof category === "object" && "id" in category ? category.id : undefined),
    category: category && typeof category === "object" && "name" in category
      ? category.name
      : category,
    author: author && typeof author === "object" && "displayName" in author
      ? author.displayName ?? null
      : author,
  };
}, recipeDetailOutputSchema);

export const recipePageSchema = z.object({
  items: z.array(recipeSummarySchema),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalCount: z.number().int(),
});

export const recipeTrashItemSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  slug: z.string(),
  status: z.string(),
  deletedAt: z.string().nullable(),
});

export const recipeTrashPageSchema = z.object({
  items: z.array(recipeTrashItemSchema),
  totalCount: z.number().int(),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalPages: z.number().int(),
});
export type RecipePage = z.infer<typeof recipePageSchema>;
export type RecipeTrashItem = z.infer<typeof recipeTrashItemSchema>;
export type RecipeTrashPage = z.infer<typeof recipeTrashPageSchema>;

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
