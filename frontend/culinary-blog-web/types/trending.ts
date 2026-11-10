export interface TrendingRecipeDto {
  rank: number;
  recipeId: string;
  title: string;
  slug: string;
  coverImageUrl?: string | null;
  categoryName: string;
  categorySlug: string;
  authorName: string;
  viewsInPeriod: number;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  difficulty: string;
  publishedAt?: string | null;
}

