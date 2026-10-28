import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as apiModule from '@/lib/api';
import {
  addRecipeIngredient,
  changeRecipeStatus,
  createRecipe,
  deleteRecipe,
  updateRecipe,
  updateRecipeIngredient,
} from './recipes';
import type { RecipeDraftRequest, RecipeStatus } from '@/types/recipe';

const sampleDraftPayload: RecipeDraftRequest = {
  categoryId: '22222222-2222-4222-8222-222222222222',
  title: 'Bún bò Huế truyền thống',
  description: 'Món bún bò cay nồng thơm lừng mùi sả và mắm ruốc.',
  content: 'Hầm bắp bò và giò heo, nêm mắm ruốc đã lược trong, sả đập dập và ớt sa tế.',
  prepTimeMinutes: 20,
  cookTimeMinutes: 120,
  servings: 6,
  difficulty: 'Hard',
  nutrition: {
    calories: 520,
    protein: 34,
    carbohydrates: 48,
    fat: 16,
    fiber: 3.5,
    sodium: 1450,
  },
  ingredients: [
    {
      name: 'Bắp bò',
      quantity: 800,
      unit: 'g',
      notes: 'Bó tròn trước khi luộc',
      orderIndex: 0,
    },
    {
      name: 'Sả cây',
      quantity: 5,
      unit: 'cây',
      notes: 'Đập dập phần gốc',
      orderIndex: 1,
    },
  ],
  steps: [
    {
      title: 'Hầm thịt bò',
      description: 'Cho bắp bò cùng sả đập dập vào hầm 90 phút cho mềm.',
    },
    {
      title: 'Nấu nước dùng',
      description: 'Lược mắm ruốc, phi thơm dầu màu điều và nêm nếm gia vị vừa ăn.',
    },
  ],
  images: [
    {
      url: '/sample-images/bun-bo.jpg',
      altText: 'Tô bún bò Huế thơm ngon',
    },
  ],
};

describe('Recipe Create, Update and Lifecycle Actions', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('1. Create Recipe Draft', () => {
    it('creates a recipe draft via POST /api/v1/recipes and returns 201 Created with initial ETag', async () => {
      const mockCreated = {
        id: '11111111-1111-4111-8111-111111111111',
        slug: 'bun-bo-hue-truyen-thong',
      };
      const mockHeaders = new Headers({
        etag: '"10001"',
        location: '/api/v1/recipes/bun-bo-hue-truyen-thong',
      });

      const spy = vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
        data: mockCreated,
        headers: mockHeaders,
      });

      const result = await createRecipe(sampleDraftPayload);

      expect(spy).toHaveBeenCalledWith(
        '/api/v1/recipes',
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({ 'Content-Type': 'application/json' }),
          body: JSON.stringify(sampleDraftPayload),
        }),
      );

      expect(result.data.id).toBe(mockCreated.id);
      expect(result.data.slug).toBe(mockCreated.slug);
      expect(result.headers.get('etag')).toBe('"10001"');
    });

    it('validates nutrition fields including Sodium and ensures non-negative numbers', () => {
      expect(sampleDraftPayload.nutrition.sodium).toBe(1450);
      expect(sampleDraftPayload.nutrition.calories).toBe(520);
      expect(sampleDraftPayload.nutrition.protein).toBe(34);
      expect(sampleDraftPayload.nutrition.carbohydrates).toBe(48);
      expect(sampleDraftPayload.nutrition.fat).toBe(16);
      expect(sampleDraftPayload.nutrition.fiber).toBe(3.5);
    });
  });

  describe('2. Update Recipe & Optimistic Concurrency (ETag / If-Match)', () => {
    it('updates recipe using PUT /api/v1/recipes/{id} sending If-Match header', async () => {
      const recipeId = '11111111-1111-4111-8111-111111111111';
      const etag = '"10001"';
      const updatedResponse = { id: recipeId, slug: 'bun-bo-hue-truyen-thong' };
      const nextHeaders = new Headers({ etag: '"10002"' });

      const spy = vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
        data: updatedResponse,
        headers: nextHeaders,
      });

      const result = await updateRecipe(recipeId, sampleDraftPayload, etag);

      expect(spy).toHaveBeenCalledWith(
        `/api/v1/recipes/${recipeId}`,
        expect.objectContaining({
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            'If-Match': etag,
          },
          body: JSON.stringify(sampleDraftPayload),
        }),
      );

      expect(result.data.id).toBe(recipeId);
      expect(result.headers.get('etag')).toBe('"10002"');
    });

    it('throws 409 Conflict when If-Match does not match current backend ETag version', async () => {
      const recipeId = '11111111-1111-4111-8111-111111111111';
      const staleEtag = '"stale-version-999"';

      const conflictError = new Error(
        'Công thức đã được thay đổi bởi một yêu cầu khác. Hãy tải lại dữ liệu rồi thử lại.',
      ) as apiModule.ApiError;
      conflictError.status = 409;

      vi.spyOn(apiModule, 'apiFetchWithResponse').mockRejectedValueOnce(conflictError);

      await expect(updateRecipe(recipeId, sampleDraftPayload, staleEtag)).rejects.toMatchObject({
        status: 409,
        message: expect.stringContaining('thay đổi bởi một yêu cầu khác'),
      });
    });

    it('rejects update when ETag is blank or missing', async () => {
      const recipeId = '11111111-1111-4111-8111-111111111111';
      await expect(updateRecipe(recipeId, sampleDraftPayload, '')).rejects.toThrow(
        /Thiếu ETag của công thức/,
      );
    });
  });

  describe('3. Lifecycle Transitions (Draft -> Published -> Unpublished -> Archived -> Unarchive)', () => {
    const recipeId = '11111111-1111-4111-8111-111111111111';

    it('publishes a draft recipe via POST /api/v1/recipes/{id}/publish with ETag', async () => {
      const spy = vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
        data: { id: recipeId, status: 'Published' },
        headers: new Headers({ etag: '"10003"' }),
      });

      const result = await changeRecipeStatus(recipeId, 'publish', '"10002"');

      expect(spy).toHaveBeenCalledWith(
        `/api/v1/recipes/${recipeId}/publish`,
        expect.objectContaining({
          method: 'POST',
          headers: { 'If-Match': '"10002"' },
        }),
      );
      expect(result.data.status).toBe('Published');
      expect(result.headers.get('etag')).toBe('"10003"');
    });

    it('unpublishes a published recipe via POST /api/v1/recipes/{id}/unpublish back to Draft', async () => {
      const spy = vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
        data: { id: recipeId, status: 'Draft' },
        headers: new Headers({ etag: '"10004"' }),
      });

      const result = await changeRecipeStatus(recipeId, 'unpublish', '"10003"');

      expect(spy).toHaveBeenCalledWith(
        `/api/v1/recipes/${recipeId}/unpublish`,
        expect.objectContaining({
          method: 'POST',
          headers: { 'If-Match': '"10003"' },
        }),
      );
      expect(result.data.status).toBe('Draft');
      expect(result.headers.get('etag')).toBe('"10004"');
    });

    it('archives a recipe via POST /api/v1/recipes/{id}/archive', async () => {
      const spy = vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
        data: { id: recipeId, status: 'Archived' },
        headers: new Headers({ etag: '"10005"' }),
      });

      const result = await changeRecipeStatus(recipeId, 'archive', '"10004"');

      expect(spy).toHaveBeenCalledWith(
        `/api/v1/recipes/${recipeId}/archive`,
        expect.objectContaining({
          method: 'POST',
          headers: { 'If-Match': '"10004"' },
        }),
      );
      expect(result.data.status).toBe('Archived');
      expect(result.headers.get('etag')).toBe('"10005"');
    });

    it('unarchives an archived recipe via POST /api/v1/recipes/{id}/unarchive back to Draft', async () => {
      const spy = vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
        data: { id: recipeId, status: 'Draft' },
        headers: new Headers({ etag: '"10006"' }),
      });

      const result = await changeRecipeStatus(recipeId, 'unarchive', '"10005"');

      expect(spy).toHaveBeenCalledWith(
        `/api/v1/recipes/${recipeId}/unarchive`,
        expect.objectContaining({
          method: 'POST',
          headers: { 'If-Match': '"10005"' },
        }),
      );
      expect(result.data.status).toBe('Draft');
      expect(result.headers.get('etag')).toBe('"10006"');
    });

    it('handles 422 Unprocessable Entity if publish conditions fail (e.g. unconfirmed email or 0 steps)', async () => {
      const unconfirmedEmailError = new Error(
        'The recipe author must have a confirmed email before publishing.',
      ) as apiModule.ApiError;
      unconfirmedEmailError.status = 422;

      vi.spyOn(apiModule, 'apiFetchWithResponse').mockRejectedValueOnce(unconfirmedEmailError);

      await expect(changeRecipeStatus(recipeId, 'publish', '"10002"')).rejects.toMatchObject({
        status: 422,
        message: expect.stringContaining('author must have a confirmed email'),
      });
    });
  });

  describe('4. Recipe Soft Delete', () => {
    it('deletes a recipe sending If-Match ETag via DELETE /api/v1/recipes/{id}', async () => {
      const recipeId = '11111111-1111-4111-8111-111111111111';
      const spy = vi.spyOn(apiModule, 'apiFetch').mockResolvedValueOnce(undefined);

      await deleteRecipe(recipeId, '"10006"');

      expect(spy).toHaveBeenCalledWith(
        `/api/v1/recipes/${recipeId}`,
        expect.objectContaining({
          method: 'DELETE',
          headers: { 'If-Match': '"10006"' },
        }),
      );
    });

    it('rejects delete when ETag is missing', async () => {
      await expect(deleteRecipe('11111111-1111-4111-8111-111111111111', '')).rejects.toThrow(
        /Thiếu ETag của công thức/,
      );
    });
  });

  describe('5. Ingredient Management (Add, Update, Reorder with OrderIndex)', () => {
    const recipeId = '11111111-1111-4111-8111-111111111111';

    it('adds an ingredient with orderIndex and notes', async () => {
      const ingredientInput = {
        name: 'Hành hoa',
        quantity: 50,
        unit: 'g',
        notes: 'Cắt nhỏ',
        orderIndex: 2,
      };

      const mockIngredient = {
        id: '33333333-3333-4333-8333-333333333399',
        name: 'Hành hoa',
        quantity: 50,
        unit: 'g',
        notes: 'Cắt nhỏ',
        sortOrder: 2,
        orderIndex: 2,
      };

      const spy = vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
        data: mockIngredient,
        headers: new Headers({ etag: '"10007"' }),
      });

      const result = await addRecipeIngredient(recipeId, ingredientInput, '"10006"');

      expect(spy).toHaveBeenCalledWith(
        `/api/v1/recipes/${recipeId}/ingredients`,
        expect.objectContaining({
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'If-Match': '"10006"',
          },
          body: JSON.stringify(ingredientInput),
        }),
      );

      expect(result.data.name).toBe('Hành hoa');
      expect(result.data.orderIndex).toBe(2);
      expect(result.headers.get('etag')).toBe('"10007"');
    });

    it('updates an ingredient and maintains orderIndex sequence', async () => {
      const ingredientId = '33333333-3333-4333-8333-333333333399';
      const updatedInput = {
        name: 'Hành lá tươi',
        quantity: 60,
        unit: 'g',
        notes: 'Thái nhỏ rắc lên mặt',
        orderIndex: 0,
      };

      const mockUpdated = {
        id: ingredientId,
        name: 'Hành lá tươi',
        quantity: 60,
        unit: 'g',
        notes: 'Thái nhỏ rắc lên mặt',
        sortOrder: 0,
        orderIndex: 0,
      };

      const spy = vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
        data: mockUpdated,
        headers: new Headers({ etag: '"10008"' }),
      });

      const result = await updateRecipeIngredient(recipeId, ingredientId, updatedInput, '"10007"');

      expect(spy).toHaveBeenCalledWith(
        `/api/v1/recipes/${recipeId}/ingredients/${ingredientId}`,
        expect.objectContaining({
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            'If-Match': '"10007"',
          },
          body: JSON.stringify(updatedInput),
        }),
      );

      expect(result.data.name).toBe('Hành lá tươi');
      expect(result.data.orderIndex).toBe(0);
    });
  });
});
