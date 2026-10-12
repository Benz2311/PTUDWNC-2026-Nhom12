import { describe, expect, it } from 'vitest';
import { recipeDetailSchema, recipeSummarySchema } from './recipe';

const recipeSummary = {
  id: '11111111-1111-4111-8111-111111111111',
  title: 'Vegetable soup',
  slug: 'vegetable-soup',
  description: null,
  category: 'Vegetarian',
  author: 'Demo Author',
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  servings: 2,
  difficulty: 'Easy',
  status: 'Published',
  createdAt: '2026-10-12T00:00:00Z',
  primaryImageUrl: null,
};

describe('Recipe API contracts', () => {
  it('accepts a backend recipe summary and its string enums', () => {
    expect(recipeSummarySchema.parse(recipeSummary)).toEqual(recipeSummary);
  });

  it('rejects backend enum values outside the declared API contract', () => {
    expect(() => recipeSummarySchema.parse({ ...recipeSummary, difficulty: 'Impossible' })).toThrow();
  });

  it('accepts the full recipe detail shape returned by the API', () => {
    const { primaryImageUrl, ...detailSummary } = recipeSummary;
    expect(primaryImageUrl).toBeNull();
    const detail = {
      ...detailSummary,
      content: 'Simmer the vegetables until tender.',
      categoryId: '22222222-2222-4222-8222-222222222222',
      ingredients: [{
        id: '33333333-3333-4333-8333-333333333333',
        name: 'Carrot',
        quantity: 1,
        unit: 'piece',
        sortOrder: 0,
        notes: null,
      }],
      steps: [{
        id: '44444444-4444-4444-8444-444444444444',
        stepNumber: 1,
        title: 'Simmer',
        description: 'Cook the vegetables until tender.',
      }],
      nutrition: {
        calories: 100,
        protein: 4,
        carbohydrates: 18,
        fat: 2,
        fiber: 3,
        sodium: 250,
      },
      images: [{
        id: '55555555-5555-4555-8555-555555555555',
        url: '/sample-images/soup.svg',
        altText: null,
        isPrimary: true,
        sortOrder: 0,
      }],
    };

    expect(recipeDetailSchema.parse(detail)).toEqual(detail);
  });
});
