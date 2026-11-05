import { describe, expect, it } from 'vitest';
import {
  formatNutritionValue,
  scaleRecipeIngredients,
  scaleRecipeNutrition,
} from './recipe-scaling';

describe('recipe serving scaling', () => {
  it('scales quantities without mutating the source ingredients', () => {
    const ingredients = [{
      id: '11111111-1111-4111-8111-111111111111',
      name: 'Flour',
      quantity: 100,
      unit: 'g',
      notes: null,
    }];

    const scaled = scaleRecipeIngredients(ingredients, 2, 6);

    expect(scaled[0].quantity).toBe(300);
    expect(ingredients[0].quantity).toBe(100);
  });

  it('scales nutrition values and preserves nullable values', () => {
    expect(scaleRecipeNutrition({
      calories: 200,
      protein: null,
      carbohydrates: 30,
      fat: 4,
      fiber: null,
      sodium: 100,
    }, 2, 4)).toEqual({
      calories: 400,
      protein: null,
      carbohydrates: 60,
      fat: 8,
      fiber: null,
      sodium: 200,
    });
  });

  it('formats nutrition values without floating-point tails', () => {
    expect(formatNutritionValue(454.66666666666663)).toBe('454,7');
    expect(formatNutritionValue(30.666666666666664)).toBe('30,7');
    expect(formatNutritionValue(null)).toBe('—');
    expect(formatNutritionValue(Number.NaN)).toBe('—');
  });
});
