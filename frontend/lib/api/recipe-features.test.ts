import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as apiModule from '@/lib/api';
import {
  compareRecipeRevisions,
  createRecipeComment,
  deletePersonalRecipeNote,
  duplicateRecipe,
  getPersonalRecipeNote,
  getRecipeComments,
  getRecipeRevisions,
  setRecipeRating,
  upsertPersonalRecipeNote,
} from './recipes';

// ---------------------------------------------------------------------------
// Mock setup
// ---------------------------------------------------------------------------

vi.mock('@/lib/api', async () => {
  const actual = await vi.importActual<typeof apiModule>('@/lib/api');
  return {
    ...actual,
    apiFetch: vi.fn(),
    apiJson: vi.fn(),
  };
});

const mockApiFetch = vi.mocked(apiModule.apiFetch);

beforeEach(() => {
  vi.clearAllMocks();
});

// ---------------------------------------------------------------------------
// 0. Recipe comments
// ---------------------------------------------------------------------------

describe('recipe comments', () => {
  const recipeId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';

  it('loads comments including status for pending moderation feedback', async () => {
    const pendingComment = {
      id: 'comment-id',
      content: 'Bình luận đang chờ duyệt',
      authorName: 'Người dùng',
      createdAt: '2026-11-11T03:00:00Z',
      updatedAt: '2026-11-11T03:00:00Z',
      canManage: false,
      status: 'Pending',
      replies: [],
    };
    mockApiFetch.mockResolvedValueOnce({
      data: [pendingComment],
      meta: { page: 1, pageSize: 20, total: 1 },
    });

    await expect(getRecipeComments(recipeId)).resolves.toMatchObject({
      items: [pendingComment],
      page: 1,
      totalCount: 1,
    });
    expect(mockApiFetch).toHaveBeenCalledWith(
      `/api/v1/recipes/${recipeId}/comments?page=1&pageSize=20`,
      {},
      true,
    );
  });

  it('submits a comment and preserves the moderation status returned by the API', async () => {
    const submitted = {
      id: 'comment-id',
      content: 'Bình luận mới',
      status: 'Pending',
      createdAt: '2026-11-11T03:00:00Z',
    };
    mockApiFetch.mockResolvedValueOnce(submitted);

    await expect(createRecipeComment(recipeId, submitted.content)).resolves.toEqual(submitted);

    const [url, options] = mockApiFetch.mock.calls[0];
    expect(url).toBe(`/api/v1/recipes/${recipeId}/comments`);
    expect((options as RequestInit).method).toBe('POST');
    expect(JSON.parse((options as RequestInit).body as string)).toEqual({
      content: submitted.content,
      parentCommentId: null,
    });
  });
});

describe('setRecipeRating', () => {
  it('sends the selected score through the authenticated rating endpoint', async () => {
    const recipeId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
    const expected = { averageRating: 4, ratingCount: 1, myScore: 4 };
    mockApiFetch.mockResolvedValueOnce(expected);

    await expect(setRecipeRating(recipeId, 4)).resolves.toEqual(expected);

    const [url, options, withAuth] = mockApiFetch.mock.calls[0];
    expect(url).toBe(`/api/v1/recipes/${recipeId}/ratings/me`);
    expect((options as RequestInit).method).toBe('PUT');
    expect(JSON.parse((options as RequestInit).body as string)).toEqual({ score: 4 });
    expect(withAuth).toBeUndefined();
  });
});

// ---------------------------------------------------------------------------
// 1. Duplicate Recipe
// ---------------------------------------------------------------------------

describe('duplicateRecipe', () => {
  it('calls POST /api/v1/recipes/{id}/duplicate and returns new recipe info', async () => {
    const sourceId = '11111111-1111-4111-8111-111111111111';
    const expected = {
      id: '22222222-2222-4222-8222-222222222222',
      slug: 'pho-bo-ha-noi-copy',
      title: 'Phở bò Hà Nội (Bản sao)',
      status: 'Draft',
      version: 1,
    };
    mockApiFetch.mockResolvedValueOnce(expected);

    const result = await duplicateRecipe(sourceId);

    expect(mockApiFetch).toHaveBeenCalledOnce();
    const [url, options] = mockApiFetch.mock.calls[0];
    expect(url).toBe(`/api/v1/recipes/${sourceId}/duplicate`);
    expect((options as RequestInit).method).toBe('POST');
    expect(result).toEqual(expected);
  });

  it('calls with the correct recipeId encoded in URL', async () => {
    const sourceId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
    mockApiFetch.mockResolvedValueOnce({ id: sourceId, slug: 'x-copy', title: 'x', status: 'Draft', version: 1 });

    await duplicateRecipe(sourceId);

    const [url] = mockApiFetch.mock.calls[0];
    expect(url).toContain(sourceId);
    expect(url).toContain('duplicate');
  });
});

// ---------------------------------------------------------------------------
// 2. Personal Recipe Note – Get
// ---------------------------------------------------------------------------

describe('getPersonalRecipeNote', () => {
  it('calls GET /api/v1/recipes/{recipeId}/notes/me and returns note', async () => {
    const recipeId = '33333333-3333-4333-8333-333333333333';
    const expected = {
      id: 'note-id-1',
      recipeId,
      userId: 'user-id-1',
      content: 'Ghi chú thử nghiệm',
      createdAt: '2026-11-01T10:00:00Z',
      updatedAt: null,
    };
    mockApiFetch.mockResolvedValueOnce(expected);

    const result = await getPersonalRecipeNote(recipeId);

    const [url] = mockApiFetch.mock.calls[0];
    expect(url).toBe(`/api/v1/recipes/${recipeId}/notes/me`);
    expect(result).toEqual(expected);
  });

  it('returns null when no note exists (API returns null)', async () => {
    const recipeId = '44444444-4444-4444-8444-444444444444';
    mockApiFetch.mockResolvedValueOnce(null);

    const result = await getPersonalRecipeNote(recipeId);

    expect(result).toBeNull();
  });
});

// ---------------------------------------------------------------------------
// 3. Personal Recipe Note – Upsert
// ---------------------------------------------------------------------------

describe('upsertPersonalRecipeNote', () => {
  it('calls POST /api/v1/recipes/{recipeId}/notes with content in body', async () => {
    const recipeId = '55555555-5555-4555-8555-555555555555';
    const content = 'Nên dùng nước xương bò tươi, hầm ít nhất 6 tiếng.';
    const expected = {
      id: 'note-id-2',
      recipeId,
      userId: 'user-id-2',
      content,
      createdAt: '2026-11-11T03:00:00Z',
      updatedAt: null,
    };
    mockApiFetch.mockResolvedValueOnce(expected);

    const result = await upsertPersonalRecipeNote(recipeId, content);

    const [url, options] = mockApiFetch.mock.calls[0];
    expect(url).toBe(`/api/v1/recipes/${recipeId}/notes`);
    expect((options as RequestInit).method).toBe('POST');
    expect(JSON.parse((options as RequestInit).body as string)).toEqual({ content });
    expect(result.content).toBe(content);
  });

  it('sends the content exactly as provided (no extra trimming by the function)', async () => {
    const recipeId = '66666666-6666-4666-8666-666666666666';
    const content = 'Thêm gia vị vừa đủ theo khẩu vị';
    mockApiFetch.mockResolvedValueOnce({ id: 'n1', recipeId, userId: 'u1', content, createdAt: '', updatedAt: null });

    await upsertPersonalRecipeNote(recipeId, content);

    const [, options] = mockApiFetch.mock.calls[0];
    const body = JSON.parse((options as RequestInit).body as string) as { content: string };
    expect(body.content).toBe(content);
  });
});

// ---------------------------------------------------------------------------
// 4. Personal Recipe Note – Delete
// ---------------------------------------------------------------------------

describe('deletePersonalRecipeNote', () => {
  it('calls DELETE /api/v1/recipes/{recipeId}/notes/me when no noteId provided', async () => {
    const recipeId = '77777777-7777-4777-8777-777777777777';
    mockApiFetch.mockResolvedValueOnce(undefined);

    await deletePersonalRecipeNote(recipeId);

    const [url, options] = mockApiFetch.mock.calls[0];
    expect(url).toBe(`/api/v1/recipes/${recipeId}/notes/me`);
    expect((options as RequestInit).method).toBe('DELETE');
  });

  it('calls DELETE /api/v1/recipes/{recipeId}/notes/{noteId} when noteId is provided', async () => {
    const recipeId = '88888888-8888-4888-8888-888888888888';
    const noteId = 'note-to-delete-id';
    mockApiFetch.mockResolvedValueOnce(undefined);

    await deletePersonalRecipeNote(recipeId, noteId);

    const [url] = mockApiFetch.mock.calls[0];
    expect(url).toBe(`/api/v1/recipes/${recipeId}/notes/${noteId}`);
  });
});

// ---------------------------------------------------------------------------
// 5. Recipe Revisions
// ---------------------------------------------------------------------------

describe('getRecipeRevisions', () => {
  it('calls GET /api/v1/recipes/{recipeId}/revisions and returns list', async () => {
    const recipeId = '99999999-9999-4999-8999-999999999999';
    const expected = [
      { id: 'rev-1', version: 2, title: 'Phở bò', description: '', changeSummary: 'Cập nhật nguyên liệu', editorId: 'u1', createdAt: '2026-11-02T10:00:00Z' },
      { id: 'rev-2', version: 1, title: 'Phở bò', description: '', changeSummary: 'Tạo mới', editorId: 'u1', createdAt: '2026-11-01T10:00:00Z' },
    ];
    mockApiFetch.mockResolvedValueOnce(expected);

    const result = await getRecipeRevisions(recipeId);

    const [url] = mockApiFetch.mock.calls[0];
    expect(url).toBe(`/api/v1/recipes/${recipeId}/revisions`);
    expect(result).toHaveLength(2);
  });
});

describe('compareRecipeRevisions', () => {
  it('calls GET /api/v1/recipes/{id}/revisions/compare?v1=&v2= with correct params', async () => {
    const recipeId = 'aaaabbbb-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
    const expected = {
      recipeId,
      versionA: 1,
      versionB: 2,
      from: { version: 1, title: 'Tiêu đề cũ', description: 'Mô tả cũ', contentJson: '{}' },
      to: { version: 2, title: 'Tiêu đề mới', description: 'Mô tả mới', contentJson: '{}' },
      differences: [
        { field: 'title', changeType: 'Modified', oldValue: 'Tiêu đề cũ', newValue: 'Tiêu đề mới' },
        { field: 'description', changeType: 'Modified', oldValue: 'Mô tả cũ', newValue: 'Mô tả mới' },
      ],
    };
    mockApiFetch.mockResolvedValueOnce(expected);

    const result = await compareRecipeRevisions(recipeId, 1, 2);

    const [url] = mockApiFetch.mock.calls[0];
    expect(url).toContain(`/api/v1/recipes/${recipeId}/revisions/compare`);
    expect(url).toContain('v1=1');
    expect(url).toContain('v2=2');
    expect(result.differences).toHaveLength(2);
    expect(result.differences[0].changeType).toBe('Modified');
  });

  it('returns empty differences array when versions are identical', async () => {
    const recipeId = 'bbbbcccc-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
    mockApiFetch.mockResolvedValueOnce({
      recipeId,
      versionA: 1,
      versionB: 2,
      from: { version: 1, title: 'Same', description: '', contentJson: '{}' },
      to: { version: 2, title: 'Same', description: '', contentJson: '{}' },
      differences: [],
    });

    const result = await compareRecipeRevisions(recipeId, 1, 2);

    expect(result.differences).toHaveLength(0);
  });
});
