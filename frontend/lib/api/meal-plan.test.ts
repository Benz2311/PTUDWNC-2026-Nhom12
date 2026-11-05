import { afterEach, describe, expect, it, vi } from 'vitest';
import { createShoppingList, getMealPlan, getMealPlans, getShoppingLists } from './meal-plan';

describe('meal planning API responses', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('returns a created shopping list from the API response body', async () => {
    const shoppingList = {
      id: 'list-1',
      name: 'Tuần này',
      createdAt: '2026-11-05T00:00:00Z',
      updatedAt: null,
      items: [],
    };
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(
      JSON.stringify(shoppingList),
      { status: 201, headers: { 'Content-Type': 'application/json' } },
    )));

    await expect(createShoppingList('Tuần này')).resolves.toEqual(shoppingList);
  });

  it('returns meal plans after the API client unwraps the data envelope', async () => {
    const mealPlans = [{
      id: 'plan-1',
      title: 'Kế hoạch tuần',
      startDate: '2026-11-02',
      endDate: '2026-11-08',
      itemCount: 0,
    }];
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(
      JSON.stringify({ data: mealPlans }),
      { status: 200, headers: { 'Content-Type': 'application/json' } },
    )));

    await expect(getMealPlans()).resolves.toEqual(mealPlans);
  });

  it('returns shopping list summaries from the paginated envelope', async () => {
    const shoppingLists = [{
      id: 'list-1',
      name: 'Tuần này',
      createdAt: '2026-11-05T00:00:00Z',
      itemCount: 4,
      checkedCount: 2,
    }];
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(
      JSON.stringify({
        data: shoppingLists,
        meta: { page: 1, pageSize: 20, total: 1, totalPages: 1 },
      }),
      { status: 200, headers: { 'Content-Type': 'application/json' } },
    )));

    await expect(getShoppingLists()).resolves.toEqual(shoppingLists);
  });

  it('keeps the existing meal planner on the legacy endpoint', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(
      JSON.stringify([]),
      { status: 200, headers: { 'Content-Type': 'application/json' } },
    )));

    await getMealPlan('2026-11-02', '2026-11-08');

    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/v1/me/meal-plan?from=2026-11-02&to=2026-11-08'),
      expect.any(Object),
    );
  });
});
