import type { RecipeIngredient, RecipeNutrition } from '@/types/recipe';

export function scaleRecipeQuantity(
  quantity: number | null | undefined,
  originalServings: number,
  targetServings: number,
): number | null {
  if (quantity == null) return null;
  if (!Number.isFinite(originalServings) || originalServings <= 0) return quantity;
  return quantity * targetServings / originalServings;
}

export function scaleRecipeIngredients(
  ingredients: RecipeIngredient[],
  originalServings: number,
  targetServings: number,
): RecipeIngredient[] {
  return ingredients.map((ingredient) => ({
    ...ingredient,
    quantity: scaleRecipeQuantity(ingredient.quantity, originalServings, targetServings),
  }));
}

export function scaleRecipeNutrition(
  nutrition: RecipeNutrition | null | undefined,
  originalServings: number,
  targetServings: number,
): RecipeNutrition | null | undefined {
  if (!nutrition) return nutrition;
  const factor = originalServings > 0 ? targetServings / originalServings : 1;
  return Object.fromEntries(
    Object.entries(nutrition).map(([key, value]) => [
      key,
      typeof value === 'number' ? value * factor : value,
    ]),
  ) as RecipeNutrition;
}

export function formatScaledQuantity(quantity: number | null | undefined): string {
  if (quantity == null) return '—';
  const whole = Math.floor(quantity);
  const fraction = quantity - whole;
  const commonFractions: Array<[number, string]> = [
    [0.25, '1/4'],
    [0.333, '1/3'],
    [0.5, '1/2'],
    [0.667, '2/3'],
    [0.75, '3/4'],
  ];
  const match = commonFractions.find(([value]) => Math.abs(fraction - value) < 0.02);
  if (match) return whole > 0 ? `${whole} ${match[1]}` : match[1];
  if (Math.abs(quantity - Math.round(quantity)) < 0.02) return String(Math.round(quantity));
  return new Intl.NumberFormat('vi-VN', {
    maximumFractionDigits: 2,
  }).format(quantity);
}

export function formatNutritionValue(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return '—';
  return new Intl.NumberFormat('vi-VN', {
    maximumFractionDigits: 1,
  }).format(value);
}
