import { apiClient, unwrapPaged } from './client';
import type { CommentReportItem, CommentReportPagedResult } from '@/types/report';

export async function getAdminCommentReports(
  status?: string,
  page: number = 1,
  pageSize: number = 10
): Promise<CommentReportPagedResult> {
  const response = await apiClient.get('/admin/comment-reports', {
    params: {
      status: status && status !== 'all' ? status : undefined,
      page,
      pageSize,
    },
  });
  const paged = unwrapPaged<CommentReportItem>(response);
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

export async function resolveCommentReport(
  reportId: string,
  action: 'Dismiss' | 'HideComment',
  resolutionNotes?: string
): Promise<void> {
  await apiClient.put(`/admin/comment-reports/${reportId}/resolve`, {
    action,
    resolutionNotes: resolutionNotes || undefined,
  });
}
