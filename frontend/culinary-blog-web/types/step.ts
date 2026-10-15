export interface RecipeStep {
  id: string;
  recipeId?: string;
  stepNumber: number;
  title?: string | null;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
  createdAt?: string | null;
  file?: File;
}

export interface CreateRecipeStepRequest {
  title?: string | null;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export interface UpdateRecipeStepRequest {
  title?: string | null;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export interface ReorderRecipeStepsRequest {
  stepIds: string[];
}

export interface StepImageUploadResponse {
  url: string;
}

// Aliases for backwards compatibility across existing UI components
export type CreateStepInput = CreateRecipeStepRequest;
export type UpdateStepInput = UpdateRecipeStepRequest;
export type ReorderStepsInput = ReorderRecipeStepsRequest;

export interface StepFormValues {
  title: string;
  description: string;
  timerMinutes: string;
  imageUrl: string;
  file?: File;
}

export interface StepValidationError {
  title?: string;
  description?: string;
  timerMinutes?: string;
  imageUrl?: string;
}
