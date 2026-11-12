import { apiClient, unwrapData, unwrapPaged } from './client';
import type { CommentItem, CommentPagedResult } from '@/types/comment';

export async function getComments(
  recipeId: string,
  page: number = 1,
  pageSize: number = 10
): Promise<CommentPagedResult> {
  const response = await apiClient.get(`/recipes/${recipeId}/comments`, {
    params: { page, pageSize },
  });
  const paged = unwrapPaged<CommentItem>(response);
  return {
    items: paged.items,
    page: paged.page,
    pageSize: paged.pageSize,
    totalCount: paged.totalCount,
    totalPages: paged.totalPages,
    hasNextPage: paged.hasNextPage,
    hasPreviousPage: paged.hasPreviousPage,
  };
}

export async function createComment(
  recipeId: string,
  content: string,
  parentCommentId?: string
): Promise<CommentItem> {
  const response = await apiClient.post(`/recipes/${recipeId}/comments`, {
    content,
    parentCommentId: parentCommentId || undefined,
  });
  return unwrapData(response);
}

export async function updateComment(
  commentId: string,
  content: string
): Promise<CommentItem> {
  const response = await apiClient.put(`/comments/${commentId}`, { content });
  return unwrapData(response);
}

export async function deleteComment(commentId: string): Promise<void> {
  await apiClient.delete(`/comments/${commentId}`);
}

export async function reportComment(
  commentId: string,
  reason: string,
  details?: string
): Promise<{ reportId: string }> {
  const response = await apiClient.post(`/comments/${commentId}/reports`, {
    reason,
    details: details || undefined,
  });
  return unwrapData(response);
}
