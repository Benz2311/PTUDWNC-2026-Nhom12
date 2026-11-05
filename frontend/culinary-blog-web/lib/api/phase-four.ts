import { apiClient, ApiError } from "./client";

export type ActivityType = "CreatedRecipe" | "UpdatedRecipe" | "RatedRecipe" | "CommentedRecipe";
export type NutritionGender = "Female" | "Male";
export type NutritionActivityLevel = "Sedentary" | "Light" | "Moderate" | "Active";
export type NutritionGoalType = "WeightLoss" | "Maintenance" | "MuscleGain";

export interface FeedItem {
  id: string;
  userId: string;
  userName: string;
  userAvatarUrl: string | null;
  activityType: ActivityType;
  targetId: string;
  recipeId: string;
  recipeTitle: string;
  recipeSlug: string;
  imageUrl: string | null;
  createdAt: string;
}

export interface FeedPage {
  items: FeedItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface NutritionGoal {
  age: number;
  gender: NutritionGender;
  heightCm: number;
  weightKg: number;
  activityLevel: NutritionActivityLevel;
  goalType: NutritionGoalType;
  targetCalories: number;
  targetProteinG: number;
  targetCarbG: number;
  targetFatG: number;
  updatedAt: string;
  basalMetabolicRate?: number | null;
  totalDailyEnergyExpenditure?: number | null;
}

export interface NutritionSummary {
  date: string;
  startDate: string;
  endDate: string;
  period: "day" | "week";
  consumedMeals: number;
  calories: number;
  proteinG: number;
  carbohydratesG: number;
  fatG: number;
}

export interface SuggestedRecipe {
  id: string;
  title: string;
  slug: string;
  imageUrl: string | null;
  ingredients: string[];
}

export interface OcrIngredient {
  name: string;
  quantity: string | null;
  unit: string | null;
}

export interface OcrStep {
  title: string;
  description: string;
}

export interface OcrRecipeDraft {
  title: string;
  description: string;
  ingredients: OcrIngredient[];
  steps: OcrStep[];
}

export const phaseFourApi = {
  async getFeed(page = 1, pageSize = 20): Promise<FeedPage> {
    const response = await apiClient.get<FeedPage>("/me/feed", { params: { page, pageSize } });
    return response.data;
  },

  async getFollowStatus(authorId: string): Promise<boolean> {
    const response = await apiClient.get<{ data: { following: boolean } }>(
      `/users/${encodeURIComponent(authorId)}/follow-status`
    );
    return response.data.data.following;
  },

  async setFollowing(authorId: string, following: boolean): Promise<boolean> {
    const response = following
      ? await apiClient.post<{ data: { following: boolean } }>(`/users/${encodeURIComponent(authorId)}/follow`)
      : await apiClient.delete<{ data: { following: boolean } }>(`/users/${encodeURIComponent(authorId)}/follow`);
    return response.data.data.following;
  },

  async getNutritionGoals(): Promise<NutritionGoal | null> {
    try {
      const response = await apiClient.get<NutritionGoal>("/me/nutrition-goals");
      return response.data;
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) return null;
      throw error;
    }
  },

  async updateNutritionGoals(
    request: Omit<NutritionGoal, "targetCalories" | "targetProteinG" | "targetCarbG" | "targetFatG" | "updatedAt" | "basalMetabolicRate" | "totalDailyEnergyExpenditure">
  ): Promise<NutritionGoal> {
    const response = await apiClient.put<NutritionGoal>("/me/nutrition-goals", request);
    return response.data;
  },

  async getNutritionSummary(date?: string, period: "day" | "week" = "day"): Promise<NutritionSummary> {
    const response = await apiClient.get<NutritionSummary>("/me/nutrition-summary", {
      params: { ...(date ? { date } : {}), period },
    });
    return response.data;
  },

  async suggestRecipes(ingredients: string[]): Promise<SuggestedRecipe[]> {
    const response = await apiClient.post<{ recipes: SuggestedRecipe[] }>("/ai/suggest-by-ingredients", {
      ingredients,
    });
    return response.data.recipes;
  },

  async readRecipeImage(file: File): Promise<OcrRecipeDraft> {
    const data = new FormData();
    data.append("image", file);
    const response = await apiClient.post<OcrRecipeDraft>("/ai/ocr-recipe", data, {
      headers: { "Content-Type": "multipart/form-data" },
      timeout: 60_000,
    });
    return response.data;
  },
};
