export type CommentReportStatus = 'Pending' | 'Reviewed' | 'Dismissed' | 'ActionTaken';

export interface CommentReportItem {
  id: string;
  commentId: string;
  commentContent: string;
  commentAuthorId: string;
  commentAuthorName: string;
  recipeId: string;
  recipeTitle: string;
  recipeSlug: string;
  reporterId: string;
  reporterName: string;
  reason: string;
  details?: string | null;
  status: CommentReportStatus | string;
  isCommentHidden: boolean;
  createdAt: string;
  resolvedAt?: string | null;
  resolutionNotes?: string | null;
}

export interface CommentReportPagedResult {
  items: CommentReportItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
