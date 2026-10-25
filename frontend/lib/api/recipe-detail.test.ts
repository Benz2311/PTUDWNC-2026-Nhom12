import { beforeEach, describe, expect, it, vi } from 'vitest';
import { getRecipeBySlug } from './recipes';
import * as apiModule from '@/lib/api';
import type { RecipeDetail } from '@/types/recipe';

const mockBaseRecipe: RecipeDetail = {
  id: '11111111-1111-4111-8111-111111111111',
  title: 'Phở bò Hà Nội truyền thống',
  slug: 'pho-bo-ha-noi',
  description: 'Món phở bò truyền thống với nước dùng thanh ngọt từ xương hầm.',
  content: 'Hướng dẫn chi tiết từng bước nấu phở bò chuẩn vị Bắc với hoa hồi, thảo quả và quế.',
  categoryId: '22222222-2222-4222-8222-222222222222',
  category: 'Món nước',
  author: 'Nguyễn Văn Đầu Bếp',
  prepTimeMinutes: 30,
  cookTimeMinutes: 180,
  servings: 4,
  difficulty: 'Hard',
  status: 'Published',
  createdAt: '2026-10-15T08:00:00Z',
  publishedAt: '2026-10-20T10:00:00Z',
  nutrition: {
    calories: 450,
    protein: 28,
    carbohydrates: 55,
    fat: 12,
    fiber: 3,
    sodium: 1200,
  },
  ingredients: [
    {
      id: '33333333-3333-4333-8333-333333333331',
      name: 'Xương ống bò',
      quantity: 1.5,
      unit: 'kg',
      sortOrder: 1,
      orderIndex: 1,
      notes: 'Rửa sạch và chần qua nước sôi',
    },
    {
      id: '33333333-3333-4333-8333-333333333332',
      name: 'Bánh phở tươi',
      quantity: 500,
      unit: 'g',
      sortOrder: 2,
      orderIndex: 2,
      notes: 'Trụng sơ nước nóng trước khi ăn',
    },
    {
      id: '33333333-3333-4333-8333-333333333333',
      name: 'Thịt bò phi lê',
      quantity: 300,
      unit: 'g',
      sortOrder: 3,
      orderIndex: 3,
      notes: 'Thái lát mỏng',
    },
  ],
  steps: [
    {
      id: '44444444-4444-4444-8444-444444444441',
      stepNumber: 1,
      title: 'Sơ chế xương bò',
      description: 'Chần xương bò trong nước sôi 5 phút để khử bọt và mùi hôi.',
      timerMinutes: 5,
      imageUrl: '/sample-images/step-01.jpg',
    },
    {
      id: '44444444-4444-4444-8444-444444444442',
      stepNumber: 2,
      title: 'Ninh nước dùng',
      description: 'Hầm xương bò cùng hành tây nướng, gừng nướng, hoa hồi và thảo quả trong lửa nhỏ.',
      timerMinutes: 180,
      imageUrl: null,
    },
    {
      id: '44444444-4444-4444-8444-444444444443',
      stepNumber: 3,
      title: 'Hoàn thiện món phở',
      description: 'Cho bánh phở vào tô, xếp thịt bò lát lên trên và chan nước dùng sôi sùng sục.',
      timerMinutes: 2,
      imageUrl: '/sample-images/step-03.jpg',
    },
  ],
  images: [
    {
      id: '55555555-5555-4555-8555-555555555551',
      url: '/sample-images/pho-primary.jpg',
      altText: 'Bát phở bò nóng hổi thơm phức',
      isPrimary: true,
      sortOrder: 1,
    },
    {
      id: '55555555-5555-4555-8555-555555555552',
      url: '/sample-images/pho-broth.jpg',
      altText: 'Nồi nước dùng phở trong vắt',
      isPrimary: false,
      sortOrder: 2,
    },
  ],
};

describe('Recipe Detail API and display contract tests', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('fetches and validates a published recipe with full details and nutrition', async () => {
    const mockHeaders = new Headers({ etag: '"sample-etag-123"' });
    vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
      data: mockBaseRecipe,
      headers: mockHeaders,
    });

    const result = await getRecipeBySlug('pho-bo-ha-noi', true);

    expect(result.data.title).toBe('Phở bò Hà Nội truyền thống');
    expect(result.data.author).toBe('Nguyễn Văn Đầu Bếp');
    expect(result.data.category).toBe('Món nước');
    expect(result.data.status).toBe('Published');
    expect(result.data.prepTimeMinutes).toBe(30);
    expect(result.data.cookTimeMinutes).toBe(180);
    expect(result.data.servings).toBe(4);
    expect(result.data.publishedAt).toBe('2026-10-20T10:00:00Z');
    expect(result.data.nutrition?.calories).toBe(450);
    expect(result.data.nutrition?.sodium).toBe(1200);
    expect(result.headers.get('etag')).toBe('"sample-etag-123"');
  });

  it('accepts recipe without images and handles primary vs secondary ordering', async () => {
    const recipeNoImages: RecipeDetail = {
      ...mockBaseRecipe,
      images: [],
    };
    vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
      data: recipeNoImages,
      headers: new Headers(),
    });

    const result = await getRecipeBySlug('pho-bo-khong-anh', true);
    expect(result.data.images).toHaveLength(0);

    const recipeWithMultipleImages: RecipeDetail = {
      ...mockBaseRecipe,
      images: [
        {
          id: '55555555-5555-4555-8555-555555555552',
          url: '/sample-images/secondary.jpg',
          altText: 'Ảnh phụ',
          isPrimary: false,
          sortOrder: 2,
        },
        {
          id: '55555555-5555-4555-8555-555555555551',
          url: '/sample-images/primary.jpg',
          altText: 'Ảnh chính',
          isPrimary: true,
          sortOrder: 1,
        },
      ],
    };
    vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
      data: recipeWithMultipleImages,
      headers: new Headers(),
    });

    const resultWithImages = await getRecipeBySlug('pho-bo-nhieu-anh', true);
    const sorted = [...resultWithImages.data.images].sort(
      (a, b) => Number(b.isPrimary) - Number(a.isPrimary) || a.sortOrder - b.sortOrder,
    );
    expect(sorted[0].isPrimary).toBe(true);
    expect(sorted[0].altText).toBe('Ảnh chính');
    expect(sorted[1].isPrimary).toBe(false);
  });

  it('verifies recipe with multiple steps (StepNumber, TimerMinutes, Image) and ingredients (OrderIndex, Notes)', async () => {
    vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
      data: mockBaseRecipe,
      headers: new Headers(),
    });

    const result = await getRecipeBySlug('pho-bo-ha-noi', true);

    expect(result.data.steps).toHaveLength(3);
    const sortedSteps = [...result.data.steps].sort((a, b) => a.stepNumber - b.stepNumber);
    expect(sortedSteps[0].stepNumber).toBe(1);
    expect(sortedSteps[0].timerMinutes).toBe(5);
    expect(sortedSteps[0].imageUrl).toBe('/sample-images/step-01.jpg');
    expect(sortedSteps[1].timerMinutes).toBe(180);
    expect(sortedSteps[1].imageUrl).toBeNull();

    expect(result.data.ingredients).toHaveLength(3);
    const sortedIngredients = [...result.data.ingredients].sort(
      (a, b) => (a.orderIndex ?? a.sortOrder ?? 0) - (b.orderIndex ?? b.sortOrder ?? 0),
    );
    expect(sortedIngredients[0].name).toBe('Xương ống bò');
    expect(sortedIngredients[0].quantity).toBe(1.5);
    expect(sortedIngredients[0].unit).toBe('kg');
    expect(sortedIngredients[0].notes).toBe('Rửa sạch và chần qua nước sôi');
  });

  it('throws 404 error when recipe does not exist', async () => {
    const error = new Error('Không tìm thấy công thức.') as apiModule.ApiError;
    error.status = 404;
    vi.spyOn(apiModule, 'apiFetchWithResponse').mockRejectedValueOnce(error);

    await expect(getRecipeBySlug('slug-khong-ton-tai', true)).rejects.toMatchObject({
      status: 404,
      message: 'Không tìm thấy công thức.',
    });
  });

  it('throws 403 Forbidden when accessing draft recipe without author or admin credentials', async () => {
    const error = new Error('Bạn không có quyền xem công thức này.') as apiModule.ApiError;
    error.status = 403;
    vi.spyOn(apiModule, 'apiFetchWithResponse').mockRejectedValueOnce(error);

    await expect(getRecipeBySlug('recipe-draft-unauthorized', true)).rejects.toMatchObject({
      status: 403,
      message: 'Bạn không có quyền xem công thức này.',
    });
  });

  it('fetches draft recipe when authorized as author or admin', async () => {
    const draftRecipe: RecipeDetail = {
      ...mockBaseRecipe,
      status: 'Draft',
      publishedAt: null,
    };
    const spy = vi.spyOn(apiModule, 'apiFetchWithResponse').mockResolvedValueOnce({
      data: draftRecipe,
      headers: new Headers(),
    });

    const result = await getRecipeBySlug('recipe-draft-authorized', true);

    expect(spy).toHaveBeenCalledWith('/api/v1/recipes/recipe-draft-authorized', {}, true);
    expect(result.data.status).toBe('Draft');
    expect(result.data.publishedAt).toBeNull();
  });
});
