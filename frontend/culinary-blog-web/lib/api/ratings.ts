import { apiClient, unwrapData } from './client';
import type { RecipeRatingSummary } from '@/types/rating';

export async function getRatingSummary(recipeId: string): Promise<RecipeRatingSummary> {
  const response = await apiClient.get(`/recipes/${recipeId}/ratings/summary`);
  return unwrapData(response);
}

export async function upsertRating(recipeId: string, score: number): Promise<RecipeRatingSummary> {
  const response = await apiClient.post(`/recipes/${recipeId}/ratings`, { score });
  return unwrapData(response);
}

export async function deleteRating(recipeId: string): Promise<RecipeRatingSummary> {
  const response = await apiClient.delete(`/recipes/${recipeId}/ratings`);
  return unwrapData(response);
}
