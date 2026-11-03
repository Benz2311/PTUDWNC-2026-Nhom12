import { z } from 'zod';

export const recipeStatusSchema = z.enum(['Draft', 'Published', 'Archived']);
export type RecipeStatus = z.infer<typeof recipeStatusSchema>;

export const difficultyLevelSchema = z.enum(['Easy', 'Medium', 'Hard', 'Expert']);
export type DifficultyLevel = z.infer<typeof difficultyLevelSchema>;

export const recipeIngredientSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  quantity: z.number().nullish(),
  unit: z.string().nullish(),
  sortOrder: z.number().int().optional(),
  orderIndex: z.number().int().optional(),
  notes: z.string().nullish(),
});
export type RecipeIngredient = z.infer<typeof recipeIngredientSchema>;

export const recipeNutritionSchema = z.object({
  calories: z.number().nullish(),
  protein: z.number().nullish(),
  carbohydrates: z.number().nullish(),
  fat: z.number().nullish(),
  fiber: z.number().nullish(),
  sodium: z.number().nullish(),
});
export type RecipeNutrition = z.infer<typeof recipeNutritionSchema>;

export const recipeStepSchema = z.object({
  id: z.string().uuid(),
  stepNumber: z.number().int(),
  title: z.string(),
  description: z.string(),
  timerMinutes: z.number().int().nullish(),
  imageUrl: z.string().nullish(),
});
export type RecipeStep = z.infer<typeof recipeStepSchema>;

const recipeImageOutputSchema = z.object({
  id: z.string().uuid(),
  url: z.string().min(1),
  altText: z.string().nullish(),
  isPrimary: z.boolean().default(false),
  sortOrder: z.number().int().optional(),
});
export const recipeImageSchema = z.preprocess((value) => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return value;

  const image = value as Record<string, unknown>;
  return {
    ...image,
    url: typeof image.url === 'string'
      ? image.url
      : image.originalUrl ?? image.mediumUrl ?? image.thumbnailUrl,
    isPrimary: image.isPrimary ?? false,
    sortOrder: image.sortOrder ?? image.orderIndex,
  };
}, recipeImageOutputSchema);
export type RecipeImage = z.infer<typeof recipeImageOutputSchema>;

export const recipeSummarySchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  slug: z.string(),
  description: z.string().nullish(),
  category: z.union([
    z.string(),
    z.object({ name: z.string() }).transform(({ name }) => name),
  ]).nullish(),
  categoryName: z.string().nullish(),
  author: z.string().nullish(),
  authorName: z.string().nullish(),
  prepTimeMinutes: z.number().int(),
  cookTimeMinutes: z.number().int(),
  servings: z.number().int(),
  difficulty: difficultyLevelSchema,
  status: recipeStatusSchema.default('Published'),
  createdAt: z.string().nullish(),
  publishedAt: z.string().nullish(),
  primaryImageUrl: z.string().nullish(),
  imageUrl: z.string().nullish(),
}).transform(({ categoryName, authorName, imageUrl, primaryImageUrl, ...recipe }) => ({
  ...recipe,
  category: recipe.category ?? categoryName ?? null,
  author: recipe.author ?? authorName ?? null,
  primaryImageUrl: primaryImageUrl ?? imageUrl ?? null,
}));
export type RecipeSummary = z.infer<typeof recipeSummarySchema>;

const recipeDetailOutputSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  slug: z.string(),
  description: z.string().nullish(),
  content: z.string().default(''),
  categoryId: z.string().uuid(),
  category: z.string().nullish(),
  author: z.string().nullish(),
  prepTimeMinutes: z.number().int(),
  cookTimeMinutes: z.number().int(),
  servings: z.number().int(),
  difficulty: difficultyLevelSchema,
  status: recipeStatusSchema,
  createdAt: z.string().nullish(),
  publishedAt: z.string().nullish(),
  ingredients: z.array(recipeIngredientSchema).default([]),
  steps: z.array(recipeStepSchema).default([]),
  nutrition: recipeNutritionSchema.nullish(),
  images: z.array(recipeImageSchema).default([]),
});

export const recipeDetailSchema = z.preprocess((value) => {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return value;

  const recipe = value as Record<string, unknown>;
  const category = recipe.category;
  const author = recipe.author;
  const images = Array.isArray(recipe.images)
    ? recipe.images.map((value) => {
        if (!value || typeof value !== 'object' || Array.isArray(value)) return value;
        const image = value as Record<string, unknown>;
        return {
          ...image,
          url: typeof image.url === 'string'
            ? image.url
            : image.originalUrl ?? image.mediumUrl ?? image.thumbnailUrl,
          sortOrder: image.sortOrder ?? image.orderIndex,
        };
      })
    : recipe.images;

  return {
    ...recipe,
    categoryId: recipe.categoryId ??
      (category && typeof category === 'object' && 'id' in category ? category.id : undefined),
    category: category && typeof category === 'object' && 'name' in category
      ? category.name
      : category,
    author: author && typeof author === 'object' && 'displayName' in author
      ? author.displayName ?? null
      : author,
    images,
  };
}, recipeDetailOutputSchema);
export type RecipeDetail = z.infer<typeof recipeDetailOutputSchema>;
export type Recipe = RecipeDetail;

export const recipePageSchema = z.object({
  items: z.array(recipeSummarySchema),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalCount: z.number().int(),
});
export type RecipePage = z.infer<typeof recipePageSchema>;

export const recipeTrashItemSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  slug: z.string(),
  status: z.string(),
  deletedAt: z.string().nullish(),
});

export const recipeTrashPageSchema = z.object({
  items: z.array(recipeTrashItemSchema),
  totalCount: z.number().int(),
  page: z.number().int(),
  pageSize: z.number().int(),
  totalPages: z.number().int(),
});
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
  ingredients: z.array(z.object({
    name: z.string().trim().min(1).max(200),
    quantity: z.number().positive().nullable(),
    unit: z.string().max(50).nullable(),
    notes: z.string().max(500).nullable(),
    orderIndex: z.number().int().nonnegative(),
  })),
  images: z.array(z.object({
    url: z.string().min(1),
    altText: z.string().nullable(),
  })),
  steps: z.array(z.object({
    title: z.string().max(200),
    description: z.string().trim().min(1).max(2000),
  })),
});
export type RecipeDraftRequest = z.infer<typeof recipeDraftRequestSchema>;
