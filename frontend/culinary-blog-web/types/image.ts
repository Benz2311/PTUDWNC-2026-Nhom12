export interface RecipeImage {
  id: string;
  recipeId: string;
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  altText?: string | null;
  isPrimary: boolean;
  orderIndex: number;
  createdAt?: string | null;
}

export interface AddRecipeImageInput {
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  altText?: string | null;
  isPrimary?: boolean | null;
  orderIndex?: number | null;
}

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
