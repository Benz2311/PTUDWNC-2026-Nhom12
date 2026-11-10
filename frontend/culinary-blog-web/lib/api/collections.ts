import { apiClient, unwrapData } from "./client";
import type {
  RecipeCollectionDto,
  RecipeCollectionDetailDto,
  CreateCollectionPayload,
  UpdateCollectionPayload,
} from "@/types/collection";
import type { ApiResponse } from "@/types/api";

export async function getUserCollections(): Promise<RecipeCollectionDto[]> {
  const response = await apiClient.get<ApiResponse<RecipeCollectionDto[]>>("/collections");
  return unwrapData(response) || [];
}

export async function getCollectionById(id: string): Promise<RecipeCollectionDetailDto> {
  const response = await apiClient.get<ApiResponse<RecipeCollectionDetailDto>>(`/collections/${id}`);
  return unwrapData(response);
}

export async function createCollection(payload: CreateCollectionPayload): Promise<RecipeCollectionDto> {
  const response = await apiClient.post<ApiResponse<RecipeCollectionDto>>("/collections", payload);
  return unwrapData(response);
}

export async function updateCollection(
  id: string,
  payload: UpdateCollectionPayload
): Promise<RecipeCollectionDto> {
  const response = await apiClient.put<ApiResponse<RecipeCollectionDto>>(`/collections/${id}`, payload);
  return unwrapData(response);
}

export async function deleteCollection(id: string): Promise<boolean> {
  await apiClient.delete(`/collections/${id}`);
  return true;
}

export async function addRecipeToCollection(collectionId: string, recipeId: string): Promise<boolean> {
  await apiClient.post(`/collections/${collectionId}/recipes`, { recipeId });
  return true;
}

export async function removeRecipeFromCollection(collectionId: string, recipeId: string): Promise<boolean> {
  await apiClient.delete(`/collections/${collectionId}/recipes/${recipeId}`);
  return true;
}

export async function getRecipeCollectionsForUser(recipeId: string): Promise<string[]> {
  try {
    const response = await apiClient.get<ApiResponse<string[]>>(`/recipes/${recipeId}/collections`);
    return unwrapData(response) || [];
  } catch (err) {
    console.error("Failed to get collections for recipe:", err);
    return [];
  }
}

