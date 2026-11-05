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
  recipeTrashPageSchema,
  type RecipeDetail,
  type RecipeDraftRequest,
  type RecipeIngredient,
  type RecipePage,
  type RecipeStatus,
  type RecipeStep,
  type RecipeSummary,
  type RecipeTrashPage,
  type RecipeRatingSummary,
  type RecipeComment,
} from '@/types/recipe';

export type RecipeSort = 'newest' | 'oldest' | 'title' | 'cooktime';

export type RecipeQuery = {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  status?: RecipeStatus;
  sort?: RecipeSort;
  mine?: boolean;
  allStatuses?: boolean;
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
  if (query.mine !== undefined) params.set('mine', String(query.mine));
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
    ...(etag !== undefined ? { 'If-Match': requireEtag(etag) } : {}),
  };
}

export async function getRecipes(query: RecipeQuery = {}, withAuth = true): Promise<RecipePage> {
  const path = query.allStatuses ? '/api/v1/admin/recipes' : recipeBasePath;
  const result = await apiFetch<unknown>(`${path}${buildRecipeQuery(query)}`, {}, withAuth);
  return recipePageSchema.parse(normalizeRecipePage(result));
}

function normalizeRecipePage(value: unknown): unknown {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return value;

  const envelope = value as {
    data?: unknown;
    items?: unknown;
    meta?: {
      page?: unknown;
      pageSize?: unknown;
      total?: unknown;
    };
  };

  if (Array.isArray(envelope.items)) return value;
  if (!Array.isArray(envelope.data)) return value;

  return {
    items: envelope.data,
    page: typeof envelope.meta?.page === 'number' ? envelope.meta.page : 1,
    pageSize: typeof envelope.meta?.pageSize === 'number' ? envelope.meta.pageSize : envelope.data.length,
    totalCount: typeof envelope.meta?.total === 'number' ? envelope.meta.total : envelope.data.length,
  };
}

export async function searchRecipes(
  search: string,
  query: Omit<RecipeQuery, 'search'> = {},
  withAuth = false,
): Promise<RecipePage> {
  const params = new URLSearchParams(buildRecipeQuery(query).slice(1));
  params.set('q', search.trim());
  const result = await apiFetch<unknown>(`${recipeBasePath}/search?${params.toString()}`, {}, withAuth);
  return recipePageSchema.parse(normalizeRecipePage(result));
}

export async function getDeletedRecipes(page = 1, pageSize = 12): Promise<RecipeTrashPage> {
  const result = await apiFetch<unknown>(
    `/api/v1/admin/recipes/trash?page=${page}&pageSize=${pageSize}`,
  );
  return recipeTrashPageSchema.parse(result);
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

export async function getRecipeFavoriteStatus(recipeId: string): Promise<{ isFavorite: boolean }> {
  return apiFetch<{ isFavorite: boolean }>(`${recipeBasePath}/${encodeURIComponent(recipeId)}/favorite`);
}

export async function toggleRecipeFavorite(recipeId: string): Promise<{ isFavorite: boolean }> {
  return apiFetch<{ isFavorite: boolean }>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/favorite`,
    { method: 'POST' },
  );
}

export async function getRecipeRating(recipeId: string): Promise<RecipeRatingSummary> {
  return apiFetch<RecipeRatingSummary>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/ratings`,
    {},
    false,
  );
}

export async function setRecipeRating(recipeId: string, score: number): Promise<RecipeRatingSummary> {
  return apiFetch<RecipeRatingSummary>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/ratings/me`,
    { method: 'PUT', headers: jsonHeaders(), body: JSON.stringify({ score }) },
  );
}

export async function getRecipeComments(recipeId: string, page = 1, pageSize = 20) {
  const result = await apiFetch<unknown>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/comments?page=${page}&pageSize=${pageSize}`,
    {},
    false,
  );

  if (result && typeof result === 'object' && !Array.isArray(result)) {
    const envelope = result as {
      data?: unknown;
      meta?: { page?: unknown; pageSize?: unknown; total?: unknown };
    };
    if (Array.isArray(envelope.data)) {
      return {
        items: envelope.data as RecipeComment[],
        page: typeof envelope.meta?.page === 'number' ? envelope.meta.page : page,
        pageSize: typeof envelope.meta?.pageSize === 'number' ? envelope.meta.pageSize : pageSize,
        totalCount: typeof envelope.meta?.total === 'number' ? envelope.meta.total : envelope.data.length,
      };
    }
  }

  if (Array.isArray(result)) {
    return { items: result as RecipeComment[], page, pageSize, totalCount: result.length };
  }

  return { items: [], page, pageSize, totalCount: 0 };
}

export async function createRecipeComment(
  recipeId: string,
  content: string,
  parentCommentId?: string,
): Promise<RecipeComment> {
  return apiFetch<RecipeComment>(
    `${recipeBasePath}/${encodeURIComponent(recipeId)}/comments`,
    {
      method: 'POST',
      headers: jsonHeaders(),
      body: JSON.stringify({ content, parentCommentId: parentCommentId ?? null }),
    },
  );
}
