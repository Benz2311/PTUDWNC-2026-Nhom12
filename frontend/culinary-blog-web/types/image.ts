export interface RecipeImage {
  id: string;
  recipeId?: string;
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  altText?: string | null;
  isPrimary: boolean;
  orderIndex: number;
  createdAt?: string | null;
}

export interface AddRecipeImageRequest {
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  altText?: string | null;
  isPrimary?: boolean | null;
  orderIndex?: number | null;
}

export interface UpdateRecipeImageRequest {
  altText?: string | null;
  orderIndex?: number | null;
  isPrimary?: boolean | null;
}

/** PATCH /api/v1/recipes/{recipeId}/images/{imageId}/primary has no body payload. */
export type SetPrimaryImageRequest = Record<string, never>;

// Aliases for backwards compatibility across existing UI components
export type AddRecipeImageInput = AddRecipeImageRequest;
export type UpdateRecipeImageInput = UpdateRecipeImageRequest;

export interface ImageFormValues {
  imageUrl: string;
  altText: string;
  orderIndex: string;
  isPrimary: boolean;
}

export interface ImageValidationError {
  imageUrl?: string;
  file?: string;
  altText?: string;
  orderIndex?: string;
}
