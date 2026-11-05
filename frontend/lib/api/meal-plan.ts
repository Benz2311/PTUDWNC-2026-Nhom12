import { apiFetch } from '@/lib/api';

export interface MealPlanEntry {
  id: string;
  date: string;
  mealType: string;
  recipeId: string;
  recipeTitle: string;
  recipeSlug: string;
  primaryImageUrl: string | null;
}

export interface ShoppingListItem {
  id: string;
  name: string;
  quantity: number | null;
  unit: string | null;
  isChecked: boolean;
}

export interface ShoppingList {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string | null;
  items: ShoppingListDetailItem[];
}

export interface ShoppingListDetailItem {
  id: string;
  ingredientName: string;
  recipeId: string | null;
  quantity: number | null;
  unit: string | null;
  category: string | null;
  isChecked: boolean;
  sortOrder: number;
}

export interface ShoppingListSummary {
  id: string;
  name: string;
  createdAt: string;
  itemCount: number;
  checkedCount: number;
}

export interface MealPlan {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
  items: MealPlanEntry[];
}

export interface MealPlanSummary {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
  itemCount: number;
}

interface ApiEnvelope<T> {
  data: T;
}

interface PaginatedApiEnvelope<T> extends ApiEnvelope<T> {
  meta: {
    page: number;
    pageSize: number;
    total: number;
    totalPages: number;
  };
}

function unwrap<T>(value: T | ApiEnvelope<T>): T {
  return value && typeof value === 'object' && 'data' in value
    ? (value as ApiEnvelope<T>).data
    : value;
}

export async function getMealPlan(from: string, to: string): Promise<MealPlanEntry[]> {
  const response = await apiFetch<MealPlanEntry[] | ApiEnvelope<MealPlanEntry[]>>(
    `/api/v1/me/meal-plan?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
  );
  return unwrap(response);
}

export function upsertMealPlanEntry(date: string, mealType: string, recipeId: string) {
  return apiFetch(
    '/api/v1/me/meal-plan',
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ date, mealType, recipeId }),
    },
  );
}

export function deleteMealPlanEntry(id: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/me/meal-plan/${encodeURIComponent(id)}`,
    { method: 'DELETE' },
  );
}

export async function getShoppingList(): Promise<ShoppingListItem[]> {
  const response = await apiFetch<ShoppingListItem[] | ApiEnvelope<ShoppingListItem[]>>(
    '/api/v1/me/shopping-list',
  );
  return unwrap(response);
}

export async function generateShoppingList(from: string, to: string): Promise<ShoppingListItem[]> {
  const response = await apiFetch<ShoppingListItem[] | ApiEnvelope<ShoppingListItem[]>>(
    '/api/v1/me/shopping-list/generate',
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ from, to }),
    },
  );
  return unwrap(response);
}

export function addShoppingListItem(name: string, quantity: number | null, unit: string | null) {
  return apiFetch(
    '/api/v1/me/shopping-list',
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, quantity, unit }),
    },
  );
}

export async function checkShoppingListItem(id: string, isChecked: boolean): Promise<ShoppingListItem> {
  const response = await apiFetch<ShoppingListItem | ApiEnvelope<ShoppingListItem>>(
    `/api/v1/me/shopping-list/${encodeURIComponent(id)}/check`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ isChecked }),
    },
  );
  return unwrap(response);
}

export function deleteShoppingListItem(id: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/me/shopping-list/${encodeURIComponent(id)}`,
    { method: 'DELETE' },
  );
}

export async function getShoppingLists(page = 1, pageSize = 20): Promise<ShoppingListSummary[]> {
  const response = await apiFetch<PaginatedApiEnvelope<ShoppingListSummary[]>>(
    `/api/v1/me/shopping-lists?page=${page}&pageSize=${pageSize}`,
  );
  return response.data;
}

export async function createShoppingList(
  name: string,
  recipeIds: string[] = [],
  targetServings = 1,
): Promise<ShoppingList> {
  return apiFetch<ShoppingList>(
    '/api/v1/me/shopping-lists',
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, recipeIds, targetServings }),
    },
  );
}

export async function getMealPlans(): Promise<MealPlanSummary[]> {
  return apiFetch<MealPlanSummary[]>('/api/v1/me/meal-plans');
}
