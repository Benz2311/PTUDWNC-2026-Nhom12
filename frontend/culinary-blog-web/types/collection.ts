export interface RecipeCollectionDto {
  id: string;
  name: string;
  description?: string | null;
  isPrivate: boolean;
  userId: string;
  userName: string;
  recipeCount: number;
  createdAt: string;
  updatedAt?: string | null;
  previewImageUrls: string[];
}

export interface RecipeCollectionItemDto {
  recipeId: string;
  title: string;
  slug: string;
  coverImageUrl?: string | null;
  categoryName: string;
  categorySlug: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  difficulty: string;
  addedAt: string;
}

export interface RecipeCollectionDetailDto {
  id: string;
  name: string;
  description?: string | null;
  isPrivate: boolean;
  userId: string;
  userName: string;
  createdAt: string;
  updatedAt?: string | null;
  recipes: RecipeCollectionItemDto[];
}

export interface CreateCollectionPayload {
  name: string;
  description?: string;
  isPrivate: boolean;
}

export interface UpdateCollectionPayload {
  name: string;
  description?: string;
  isPrivate: boolean;
}

