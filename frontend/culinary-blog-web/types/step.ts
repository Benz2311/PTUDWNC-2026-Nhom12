export interface RecipeStep {
  id: string;
  recipeId: string;
  stepNumber: number;
  title?: string | null;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
  createdAt?: string | null;
}

export interface CreateStepInput {
  title?: string | null;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export interface UpdateStepInput {
  title?: string | null;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export interface ReorderStepsInput {
  stepIds: string[];
}

export interface StepFormValues {
  title: string;
  description: string;
  timerMinutes: string;
  imageUrl: string;
}

export interface StepValidationError {
  title?: string;
  description?: string;
  timerMinutes?: string;
  imageUrl?: string;
}
