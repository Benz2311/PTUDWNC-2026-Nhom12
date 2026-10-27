import {
  apiFetch,
  apiFetchWithResponse,
} from '@/lib/api';
import {
  recipeDetailSchema,
  recipeIngredientSchema,
  recipePageSchema,
  recipeStepSchema,
  recipeSummarySchema,
  recipeStatusSchema,
  type RecipeDetail,
  type RecipeDraftRequest,
  type RecipeIngredient,
  type RecipePage,
  type RecipeStatus,
  type RecipeStep,
  type RecipeSummary,
} from '@/types/recipe';

export type RecipeSort = 'newest' | 'oldest' | 'title' | 'cooktime';

export type RecipeQuery = {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  status?: RecipeStatus;
  sort?: RecipeSort;
};

export type RecipeIngredientInput = {
  name: string;
  quantity: number | null;
  unit: string | null;
  notes: string | null;
  orderIndex?: number;
};

export type RecipeStepInput = {
  title: string;
  description: string;
};

export type RecipeReference = Pick<RecipeSummary, 'id' | 'slug'>;
export type RecipeStatusResult = Pick<RecipeSummary, 'id'> & { status: RecipeStatus };

const recipeBasePath = '/api/v1/recipes';

export function buildRecipeQuery(query: RecipeQuery = {}): string {
  const params = new URLSearchParams();

  if (query.page !== undefined) params.set('page', String(query.page));
  if (query.pageSize !== undefined) params.set('pageSize', String(query.pageSize));
  if (query.search?.trim()) params.set('search', query.search.trim());
  if (query.categoryId) params.set('categoryId', query.categoryId);
  if (query.status) params.set('status', query.status);
  if (query.sort) params.set('sort', query.sort);

  const serialized = params.toString();
  return serialized ? `?${serialized}` : '';
}

function requireEtag(etag: string): string {
  if (!etag.trim()) {
    throw new Error('Thiếu ETag của công thức. Hãy tải lại dữ liệu trước khi tiếp tục.');
  }

  return etag;
}

function jsonHeaders(etag?: string): HeadersInit {
  return {
    'Content-Type': 'application/json',
    ...(etag ? { 'If-Match': requireEtag(etag) } : {}),
  };
}

export async function getRecipes(query: RecipeQuery = {}, withAuth = true): Promise<RecipePage> {
  const result = await apiFetch<unknown>(`${recipeBasePath}${buildRecipeQuery(query)}`, {}, withAuth);
  return recipePageSchema.parse(result);
}

export async function searchRecipes(
  search: string,
  query: Omit<RecipeQuery, 'search'> = {},
  withAuth = false,
): Promise<RecipePage> {
  const params = new URLSearchParams(buildRecipeQuery(query).slice(1));
  params.set('query', search.trim());
  const result = await apiFetch<unknown>(`${recipeBasePath}/search?${params.toString()}`, {}, withAuth);
  return recipePageSchema.parse(result);
}

export async function getRecipeBySlug(
  slug: string,
  withAuth = true,
): Promise<{ data: RecipeDetail; headers: Headers }> {
  const response = await apiFetchWithResponse<unknown>(
    `${recipeBasePath}/${encodeURIComponent(slug)}`,
    {},
    withAuth,
  );
  return { ...response, data: recipeDetailSchema.parse(response.data) };
}

export async function createRecipe(
  request: RecipeDraftRequest,
): Promise<{ data: RecipeReference; headers: Headers }> {
  const response = await apiFetchWithResponse<RecipeReference>(recipeBasePath, {
    method: 'POST',
    headers: jsonHeaders(),
    body: JSON.stringify(request),
  });
  return response;
}

export async function updateRecipe(
  id: string,
  request: RecipeDraftRequest,
  etag: string,
): Promise<{ data: RecipeReference; headers: Headers }> {
  return apiFetchWithResponse<RecipeReference>(
    `${recipeBasePath}/${encodeURIComponent(id)}`,
    {
      method: 'PUT',
      headers: jsonHeaders(etag),
      body: JSON.stringify(request),
    },
  );
}

export async function changeRecipeStatus(
  id: string,
  action: 'publish' | 'unpublish' | 'archive' | 'unarchive',
  etag: string,
): Promise<{ data: RecipeStatusResult; headers: Headers }> {
  const response = await apiFetchWithResponse<RecipeStatusResult>(
    `${recipeBasePath}/${encodeURIComponent(id)}/${action}`,
    { method: 'POST', headers: { 'If-Match': requireEtag(etag) } },
  );
  const status = recipeStatusSchema.parse(response.data.status);
  return { ...response, data: { ...response.data, status } };
}

export async function deleteRecipe(id: string, etag: string): Promise<void> {
  await apiFetch<void>(`${recipeBasePath}/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: { 'If-Match': requireEtag(etag) },
  });
}

export async function addRecipeIngredient(
  id: string,
  request: RecipeIngredientInput,
  etag: string,
): Promise<{ data: RecipeIngredient; headers: Headers }> {
  const response = await apiFetchWithResponse<unknown>(
    `${recipeBasePath}/${encodeURIComponent(id)}/ingredients`,
    {
      method: 'POST',
      headers: jsonHeaders(etag),
      body: JSON.stringify(request),
    },
  );
  return { ...response, data: recipeIngredientSchema.parse(response.data) };
}

export async function updateRecipeIngredient(
  recipeId: string,
  ingredientId: string,
  request: RecipeIngredientInput,
  etag: string,
): Promise<{ data: RecipeIngredient; headers: Headers }> {
  const response = await apiFetchWithResponse<unknown>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/ingredients/${encodeURIComponent(ingredientId)}`,
    {
      method: 'PUT',
      headers: jsonHeaders(etag),
      body: JSON.stringify(request),
    },
  );
  return { ...response, data: recipeIngredientSchema.parse(response.data) };
}

export async function deleteRecipeIngredient(
  recipeId: string,
  ingredientId: string,
  etag: string,
): Promise<void> {
  await apiFetch<void>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/ingredients/${encodeURIComponent(ingredientId)}`,
    { method: 'DELETE', headers: { 'If-Match': requireEtag(etag) } },
  );
}

export async function addRecipeStep(
  id: string,
  request: RecipeStepInput,
  etag: string,
): Promise<{ data: RecipeStep; headers: Headers }> {
  const response = await apiFetchWithResponse<unknown>(
    `${recipeBasePath}/${encodeURIComponent(id)}/steps`,
    {
      method: 'POST',
      headers: jsonHeaders(etag),
      body: JSON.stringify(request),
    },
  );
  return { ...response, data: recipeStepSchema.parse(response.data) };
}

export async function updateRecipeStep(
  recipeId: string,
  stepId: string,
  request: RecipeStepInput,
  etag: string,
): Promise<{ data: RecipeStep; headers: Headers }> {
  const response = await apiFetchWithResponse<unknown>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/steps/${encodeURIComponent(stepId)}`,
    {
      method: 'PUT',
      headers: jsonHeaders(etag),
      body: JSON.stringify(request),
    },
  );
  return { ...response, data: recipeStepSchema.parse(response.data) };
}

export async function deleteRecipeStep(recipeId: string, stepId: string, etag: string): Promise<void> {
  await apiFetch<void>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/steps/${encodeURIComponent(stepId)}`,
    { method: 'DELETE', headers: { 'If-Match': requireEtag(etag) } },
  );
}

export async function setPrimaryRecipeImage(
  recipeId: string,
  imageId: string,
  etag: string,
): Promise<{ id: string; isPrimary: boolean }> {
  return apiFetch<{ id: string; isPrimary: boolean }>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/images/${encodeURIComponent(imageId)}/primary`,
    { method: 'PATCH', headers: { 'If-Match': requireEtag(etag) } },
  );
}

export async function deleteRecipeImage(recipeId: string, imageId: string, etag: string): Promise<void> {
  await apiFetch<void>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/images/${encodeURIComponent(imageId)}`,
    { method: 'DELETE', headers: { 'If-Match': requireEtag(etag) } },
  );
}
