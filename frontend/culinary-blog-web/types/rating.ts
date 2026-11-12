export interface RecipeRatingSummary {
  recipeId: string;
  averageRating: number;
  ratingCount: number;
  userRating?: number | null;
}
